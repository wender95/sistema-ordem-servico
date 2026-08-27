using System;
using System.Collections.Generic;
using SistemaOrdemServico.Domain.Enums;

namespace SistemaOrdemServico.DTOs;

public record CriarFluxoDto(
    string NumeroOS,
    string IdentificadorFluxo,
    string NomeCliente,
    SetorEnum SetorInicial
);

public record DespacharFluxoDto(SetorEnum SetorDestino);

public record CancelarFluxoDto(string Motivo);

public class FluxoEventoDto
{
    public Guid Id { get; set; }
    public SetorEnum Setor { get; set; }
    public TipoEvento TipoEvento { get; set; }
    public Guid UsuarioId { get; set; }
    public DateTime Timestamp { get; set; }
    public string? MotivoJustificativa { get; set; }
}

public class FluxoResponseDto
{
    public Guid Id { get; set; }
    public string NumeroOS { get; set; } = string.Empty;
    public string IdentificadorFluxo { get; set; } = string.Empty;
    public string NomeCliente { get; set; } = string.Empty;
    public SetorEnum SetorAtual { get; set; }
    public SetorEnum? SetorAnterior { get; set; }
    public StatusFluxo Status { get; set; }
    public DateTime DataCriacao { get; set; }
    public DateTime? DataEncerramento { get; set; }
    public IReadOnlyList<SetorEnum> DestinosPermitidos { get; set; } = Array.Empty<SetorEnum>();
    public IReadOnlyList<FluxoEventoDto> Eventos { get; set; } = Array.Empty<FluxoEventoDto>();
}

public class EditarFluxoDto
{
    public string NumeroOS { get; set; } = string.Empty;
    public string IdentificadorFluxo { get; set; } = string.Empty;
    public string NomeCliente { get; set; } = string.Empty;
}
