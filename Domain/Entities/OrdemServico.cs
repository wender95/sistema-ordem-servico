using System;
using System.Collections.Generic;
using SistemaOrdemServico.Domain.Enums;
using SistemaOrdemServico.Domain.Services;

namespace SistemaOrdemServico.Domain.Entities;

public class FluxoOS
{
    public Guid Id { get; private set; }
    public string NumeroOS { get; private set; } = string.Empty;
    public string IdentificadorFluxo { get; private set; } = string.Empty;
    public string NomeCliente { get; private set; } = string.Empty;
    public SetorEnum SetorAtual { get; private set; }
    public SetorEnum? SetorAnterior { get; private set; }
    public StatusFluxo Status { get; private set; }
    public DateTime DataCriacao { get; private set; }
    public DateTime? DataEncerramento { get; private set; }

    private readonly List<EventoMovimentacao> _eventos = new();
    public IReadOnlyCollection<EventoMovimentacao> Eventos => _eventos.AsReadOnly();

    public FluxoOS(
        string numeroOS,
        string identificadorFluxo,
        string nomeCliente,
        SetorEnum setorInicial,
        Guid usuarioVendedorId)
    {
        if (string.IsNullOrWhiteSpace(numeroOS))
            throw new ArgumentException("Número da OS é obrigatório.");

        if (!MatrizTransicaoService.SetorInicialEhValido(setorInicial))
            throw new ArgumentException("Setor inicial inválido para um fluxo.");

        Id = Guid.NewGuid();
        NumeroOS = numeroOS.Trim();
        IdentificadorFluxo = identificadorFluxo?.Trim() ?? string.Empty;
        NomeCliente = nomeCliente?.Trim() ?? string.Empty;
        SetorAtual = setorInicial;
        SetorAnterior = SetorEnum.Vendas;
        Status = StatusFluxo.AguardandoRecebimento;
        DataCriacao = DateTime.UtcNow;

        _eventos.Add(
            new EventoMovimentacao(
                Id,
                setorInicial,
                TipoEvento.Criado,
                usuarioVendedorId));
    }

    private FluxoOS() { }

    public void Receber(Guid usuarioOperadorId)
    {
        if (Status != StatusFluxo.AguardandoRecebimento)
            throw new InvalidOperationException("O fluxo não está aguardando recebimento.");

        Status = StatusFluxo.EmAtendimento;

        _eventos.Add(
            new EventoMovimentacao(
                Id,
                SetorAtual,
                TipoEvento.Recebido,
                usuarioOperadorId));
    }

    public void Despachar(SetorEnum setorDestino, Guid usuarioId)
    {
        if (Status is StatusFluxo.Encerrado or StatusFluxo.Cancelado)
            throw new InvalidOperationException("Não é possível despachar um fluxo encerrado ou cancelado.");

        if (Status != StatusFluxo.EmAtendimento &&
            setorAtualNaoPodeSerDespachadoPorSaida(setorDestino))
        {
            throw new InvalidOperationException("O fluxo precisa ser recebido antes do despacho.");
        }

        if (!MatrizTransicaoService.TransicaoEhValida(SetorAtual, setorDestino, SetorAnterior))
        {
            throw new InvalidOperationException(
                $"Transição de setor não permitida: '{SetorAtual}' para '{setorDestino}'.");
        }

        SetorAnterior = SetorAtual;
        SetorAtual = setorDestino;

        if (setorDestino == SetorEnum.Financeiro)
        {
            Status = StatusFluxo.Encerrado;
            DataEncerramento = DateTime.UtcNow;
        }
        else
        {
            Status = StatusFluxo.AguardandoRecebimento;
            DataEncerramento = null;
        }

        _eventos.Add(
            new EventoMovimentacao(
                Id,
                setorDestino,
                TipoEvento.Despachado,
                usuarioId));
    }

    // Prateleira/Pátio podem receber uma saída administrativa sem ação de "Receber".
    private bool setorAtualNaoPodeSerDespachadoPorSaida(SetorEnum setorDestino)
        => setorDestino == SetorEnum.Financeiro &&
           SetorAtual is SetorEnum.Prateleira or SetorEnum.Patio;

    public void Editar(
        string numeroOS,
        string identificadorFluxo,
        string nomeCliente,
        Guid usuarioId)
    {
        if (Status is StatusFluxo.Encerrado or StatusFluxo.Cancelado)
            throw new InvalidOperationException(
                "Ordens de Serviço concluídas ou canceladas não podem ser alteradas.");

        if (string.IsNullOrWhiteSpace(numeroOS))
            throw new ArgumentException("Número da OS é obrigatório.");

        NumeroOS = numeroOS.Trim();
        IdentificadorFluxo = identificadorFluxo?.Trim() ?? string.Empty;
        NomeCliente = nomeCliente?.Trim() ?? string.Empty;

        _eventos.Add(
            new EventoMovimentacao(
                Id,
                SetorAtual,
                TipoEvento.Editado,
                usuarioId));
    }

    public void Cancelar(string motivo, Guid usuarioId)
    {
        if (string.IsNullOrWhiteSpace(motivo))
            throw new ArgumentException("A justificativa de cancelamento é obrigatória.");

        if (Status is StatusFluxo.Encerrado or StatusFluxo.Cancelado)
            throw new InvalidOperationException("Apenas fluxos ativos podem ser cancelados.");

        Status = StatusFluxo.Cancelado;
        DataEncerramento = DateTime.UtcNow;

        _eventos.Add(
            new EventoMovimentacao(
                Id,
                SetorAtual,
                TipoEvento.Cancelado,
                usuarioId,
                motivo.Trim()));
    }
}
