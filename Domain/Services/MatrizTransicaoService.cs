using System;
using System.Collections.Generic;
using System.Linq;
using SistemaOrdemServico.Domain.Enums;

namespace SistemaOrdemServico.Domain.Services;

public static class MatrizTransicaoService
{
    private static readonly IReadOnlyDictionary<SetorEnum, IReadOnlyList<SetorEnum>> DestinosPermitidos =
        new Dictionary<SetorEnum, IReadOnlyList<SetorEnum>>
        {
            [SetorEnum.Vendas] = new[] { SetorEnum.Criacao, SetorEnum.Recorte, SetorEnum.Impressao, SetorEnum.Preparacao, SetorEnum.Frota, SetorEnum.Acabamento },
            [SetorEnum.Criacao] = new[] { SetorEnum.Recorte, SetorEnum.Impressao, SetorEnum.Preparacao, SetorEnum.Frota, SetorEnum.Acabamento },
            [SetorEnum.Impressao] = new[] { SetorEnum.Recorte, SetorEnum.Preparacao, SetorEnum.Frota, SetorEnum.Acabamento },
            [SetorEnum.Recorte] = new[] { SetorEnum.Preparacao, SetorEnum.Frota, SetorEnum.Acabamento },
            [SetorEnum.Preparacao] = new[] { SetorEnum.Frota },
            [SetorEnum.Acabamento] = new[] { SetorEnum.Prateleira },
            [SetorEnum.Frota] = new[] { SetorEnum.Patio },
            [SetorEnum.Prateleira] = new[] { SetorEnum.Financeiro },
            [SetorEnum.Patio] = new[] { SetorEnum.Financeiro }
        };

    public static IReadOnlyList<SetorEnum> ObterDestinosPermitidos(
        SetorEnum setorOrigem,
        SetorEnum? setorAnterior = null)
    {
        var destinos = new List<SetorEnum>();

        if (DestinosPermitidos.TryGetValue(setorOrigem, out var normais))
        {
            destinos.AddRange(normais);
        }

        if (setorAnterior.HasValue &&
            setorAnterior.Value != setorOrigem &&
            !destinos.Contains(setorAnterior.Value))
        {
            destinos.Add(setorAnterior.Value);
        }

        return destinos;
    }

    public static bool TransicaoEhValida(
        SetorEnum setorOrigem,
        SetorEnum setorDestino,
        SetorEnum? setorAnterior)
        => ObterDestinosPermitidos(setorOrigem, setorAnterior).Contains(setorDestino);

    public static bool SetorInicialEhValido(SetorEnum setor)
        => setor is SetorEnum.Criacao
            or SetorEnum.Recorte
            or SetorEnum.Impressao
            or SetorEnum.Preparacao
            or SetorEnum.Frota
            or SetorEnum.Acabamento;
}
