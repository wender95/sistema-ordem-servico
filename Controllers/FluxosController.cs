using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaOrdemServico.Domain.Entities;
using SistemaOrdemServico.Domain.Enums;
using SistemaOrdemServico.Domain.Interfaces;
using SistemaOrdemServico.Domain.Services;
using SistemaOrdemServico.DTOs;

namespace SistemaOrdemServico.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FluxosController : ControllerBase
{
    private readonly IFluxoRepository _fluxoRepository;

    public FluxosController(IFluxoRepository fluxoRepository)
    {
        _fluxoRepository = fluxoRepository;
    }

    private Guid ObterUsuarioLogadoId()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return Guid.TryParse(idClaim, out var id)
            ? id
            : throw new InvalidOperationException("Token sem identificação de usuário.");
    }

    private string? ObterSetorLogado()
        => User.FindFirst("Setor")?.Value;

    private bool PossuiPerfil(params TipoPerfil[] perfis)
        => perfis.Any(p => User.IsInRole(p.ToString()));

    private bool UsuarioDoSetorAtual(FluxoOS fluxo)
    {
        // Administrador e Diretoria podem interagir independentemente do setor atual
        if (PossuiPerfil(TipoPerfil.Administrador, TipoPerfil.Diretoria))
            return true;

        if (!Enum.TryParse<SetorEnum>(ObterSetorLogado(), true, out var setor))
            return false;

        return setor == fluxo.SetorAtual;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<FluxoResponseDto>>> ObterTodos()
    {
        var fluxos = await _fluxoRepository.ObterTodosAsync();

        if (User.IsInRole(nameof(TipoPerfil.Operacional)))
        {
            if (!Enum.TryParse<SetorEnum>(ObterSetorLogado(), true, out var setor))
                return BadRequest("Usuário operacional sem setor configurado.");

            fluxos = fluxos.Where(f => f.SetorAtual == setor);
        }
        else if (!PossuiPerfil(TipoPerfil.Vendedor, TipoPerfil.Diretoria, TipoPerfil.Administrador))
        {
            return Forbid();
        }

        return Ok(fluxos
            .Where(f => f.Status is not StatusFluxo.Encerrado and not StatusFluxo.Cancelado)
            .OrderBy(f => f.DataCriacao)
            .Select(MapearFluxo));
    }

    [HttpGet("setor/{setorId}")]
    public async Task<ActionResult<IEnumerable<FluxoResponseDto>>> ObterPorSetor(SetorEnum setorId)
    {
        if (User.IsInRole(nameof(TipoPerfil.Operacional)) &&
            (!Enum.TryParse<SetorEnum>(ObterSetorLogado(), true, out var setorLogado) || setorLogado != setorId))
        {
            return Forbid();
        }

        if (!PossuiPerfil(
                TipoPerfil.Operacional,
                TipoPerfil.Vendedor,
                TipoPerfil.Diretoria,
                TipoPerfil.Administrador))
        {
            return Forbid();
        }

        var fluxos = await _fluxoRepository.ObterFilaDoSetorAsync(setorId);

        return Ok(fluxos
            .OrderBy(f => f.DataCriacao)
            .Select(MapearFluxo));
    }

    [HttpGet("concluidas")]
    [Authorize(Roles = "Vendedor,Diretoria,Administrador")]
    public async Task<ActionResult<IEnumerable<FluxoResponseDto>>> ObterConcluidas()
    {
        var fluxos = await _fluxoRepository.ObterTodosAsync();

        return Ok(fluxos
            .Where(f => f.Status == StatusFluxo.Encerrado)
            .OrderByDescending(f => f.DataEncerramento)
            .Select(MapearFluxo));
    }

    [HttpPost]
    [Authorize(Roles = "Vendedor,Administrador,Diretoria")]
    public async Task<ActionResult<FluxoResponseDto>> Criar([FromBody] CriarFluxoDto dto)
    {
        if (dto is null)
            return BadRequest("Dados inválidos.");

        if (string.IsNullOrWhiteSpace(dto.NumeroOS))
            return BadRequest("Número da OS é obrigatório.");

        if (!MatrizTransicaoService.SetorInicialEhValido(dto.SetorInicial))
            return BadRequest("O setor inicial informado não é válido.");

        var usuarioId = ObterUsuarioLogadoId();

        var fluxo = new FluxoOS(
            dto.NumeroOS,
            dto.IdentificadorFluxo,
            dto.NomeCliente,
            dto.SetorInicial,
            usuarioId);

        await _fluxoRepository.AdicionarAsync(fluxo);

        return CreatedAtAction(
            nameof(ObterPorId),
            new { id = fluxo.Id },
            MapearFluxo(fluxo));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<FluxoResponseDto>> ObterPorId(Guid id)
    {
        var fluxo = await _fluxoRepository.ObterPorIdAsync(id);

        if (fluxo is null)
            return NotFound("OS não encontrada.");

        if (User.IsInRole(nameof(TipoPerfil.Operacional)) && !UsuarioDoSetorAtual(fluxo))
            return Forbid();

        if (!PossuiPerfil(
                TipoPerfil.Operacional,
                TipoPerfil.Vendedor,
                TipoPerfil.Diretoria,
                TipoPerfil.Administrador))
        {
            return Forbid();
        }

        return Ok(MapearFluxo(fluxo));
    }

    [HttpPost("{id:guid}/receber")]
    [Authorize(Roles = "Operacional,Administrador,Diretoria")]
    public async Task<IActionResult> Receber(Guid id)
    {
        var fluxo = await _fluxoRepository.ObterPorIdAsync(id);

        if (fluxo is null)
            return NotFound("OS não encontrada.");

        if (!UsuarioDoSetorAtual(fluxo))
            return Forbid();

        try
        {
            fluxo.Receber(ObterUsuarioLogadoId());
            await _fluxoRepository.AtualizarAsync(fluxo);

            return Ok(MapearFluxo(fluxo));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("{id:guid}/despachar")]
    [Authorize(Roles = "Operacional,Vendedor,Diretoria,Administrador")]
    public async Task<IActionResult> Despachar(
        Guid id,
        [FromBody] DespacharFluxoDto dto)
    {
        if (dto is null)
            return BadRequest("Destino é obrigatório.");

        var fluxo = await _fluxoRepository.ObterPorIdAsync(id);

        if (fluxo is null)
            return NotFound("OS não encontrada.");

        // Administrador e Diretoria podem despachar livremente
        if (!PossuiPerfil(TipoPerfil.Administrador, TipoPerfil.Diretoria))
        {
            var ehOperacional = User.IsInRole(nameof(TipoPerfil.Operacional));
            var ehVendedor = User.IsInRole(nameof(TipoPerfil.Vendedor));

            if (ehOperacional && !UsuarioDoSetorAtual(fluxo))
                return Forbid();

            if (ehVendedor &&
                (fluxo.SetorAtual is not (SetorEnum.Prateleira or SetorEnum.Patio) ||
                 dto.SetorDestino != SetorEnum.Financeiro))
            {
                return Forbid();
            }
        }

        try
        {
            fluxo.Despachar(dto.SetorDestino, ObterUsuarioLogadoId());
            await _fluxoRepository.AtualizarAsync(fluxo);

            return Ok(MapearFluxo(fluxo));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Vendedor,Diretoria,Administrador")]
    public async Task<IActionResult> Editar(
        Guid id,
        [FromBody] EditarFluxoDto dto)
    {
        if (dto is null || string.IsNullOrWhiteSpace(dto.NumeroOS))
            return BadRequest("Dados inválidos para edição.");

        var fluxo = await _fluxoRepository.ObterPorIdAsync(id);

        if (fluxo is null)
            return NotFound("OS não encontrada.");

        try
        {
            fluxo.Editar(
                dto.NumeroOS,
                dto.IdentificadorFluxo,
                dto.NomeCliente,
                ObterUsuarioLogadoId());

            await _fluxoRepository.AtualizarAsync(fluxo);

            return Ok(MapearFluxo(fluxo));
        }
        catch (Exception ex) when (
            ex is InvalidOperationException or ArgumentException)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("{id:guid}/cancelar")]
    [Authorize(Roles = "Vendedor,Diretoria,Administrador")]
    public async Task<IActionResult> Cancelar(
        Guid id,
        [FromBody] CancelarFluxoDto dto)
    {
        if (dto is null || string.IsNullOrWhiteSpace(dto.Motivo))
            return BadRequest("A justificativa de cancelamento é obrigatória.");

        var fluxo = await _fluxoRepository.ObterPorIdAsync(id);

        if (fluxo is null)
            return NotFound("OS não encontrada.");

        try
        {
            fluxo.Cancelar(dto.Motivo, ObterUsuarioLogadoId());
            await _fluxoRepository.AtualizarAsync(fluxo);

            return Ok(MapearFluxo(fluxo));
        }
        catch (Exception ex) when (
            ex is InvalidOperationException or ArgumentException)
        {
            return BadRequest(ex.Message);
        }
    }

    private static FluxoResponseDto MapearFluxo(FluxoOS fluxo)
    {
        return new FluxoResponseDto
        {
            Id = fluxo.Id,
            NumeroOS = fluxo.NumeroOS,
            IdentificadorFluxo = fluxo.IdentificadorFluxo,
            NomeCliente = fluxo.NomeCliente,
            SetorAtual = fluxo.SetorAtual,
            SetorAnterior = fluxo.SetorAnterior,
            Status = fluxo.Status,
            DataCriacao = fluxo.DataCriacao,
            DataEncerramento = fluxo.DataEncerramento,
            DestinosPermitidos = MatrizTransicaoService.ObterDestinosPermitidos(
                fluxo.SetorAtual,
                fluxo.SetorAnterior),
            Eventos = fluxo.Eventos
                .OrderBy(e => e.Timestamp)
                .Select(e => new FluxoEventoDto
                {
                    Id = e.Id,
                    Setor = e.Setor,
                    TipoEvento = e.TipoEvento, // <-- CORRIGIDO AQUI DE TipoEnum PARA TipoEvento
                    UsuarioId = e.UsuarioId,
                    Timestamp = e.Timestamp,
                    MotivoJustificativa = e.MotivoJustificativa
                })
                .ToArray()
        };
    }
}