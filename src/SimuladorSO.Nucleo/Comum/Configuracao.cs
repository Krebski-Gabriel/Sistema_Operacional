namespace SimuladorSO.Nucleo.Comum;

/// <summary>
/// Política de escalonamento de CPU selecionável em tempo de execução.
/// </summary>
public enum PoliticaEscalonamento { Fcfs, RoundRobin, Prioridade }

/// <summary>
/// Política de substituição de páginas.
/// </summary>
public enum PoliticaSubstituicao { Fifo, Lru }

/// <summary>
/// Política de atendimento das requisições de um dispositivo de bloco.
/// </summary>
public enum PoliticaDisco { Fcfs, Sstf, Scan }

/// <summary>
/// Reúne todos os parâmetros de uma execução. Nenhum destes valores está
/// embutido no código-fonte: todos chegam pela linha de comando ou pela carga.
/// A configuração é registrada no início do log para garantir reprodutibilidade.
/// </summary>
public sealed class Configuracao
{
    // Escalonamento
    public PoliticaEscalonamento Escalonamento { get; init; } = PoliticaEscalonamento.Fcfs;
    public long Quantum { get; init; } = 4;
    public long CustoTrocaContexto { get; init; } = 1;
    public bool PrioridadePreemptiva { get; init; } = true;
    public bool EnvelhecimentoAtivo { get; init; } = false; // extensão: aging
    public long IntervaloEnvelhecimento { get; init; } = 5;  // a cada N unidades na fila, melhora prioridade

    // Memória (paginação)
    public long TamanhoPagina { get; init; } = 4;
    public int NumeroMolduras { get; init; } = 8;
    public long TempoAtendimentoFalta { get; init; } = 6;
    public PoliticaSubstituicao Substituicao { get; init; } = PoliticaSubstituicao.Fifo;

    // Entrada e saída
    public long TempoServicoDispositivoPadrao { get; init; } = 5;
    public PoliticaDisco EscalonamentoDisco { get; init; } = PoliticaDisco.Fcfs;

    // Sistema de arquivos
    public int TotalBlocosDisco { get; init; } = 256;
    public long TamanhoBloco { get; init; } = 512;

    // Aleatoriedade (registrada mesmo quando não utilizada, para reprodutibilidade)
    public int Semente { get; init; } = 0;

    public string Descricao()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("# CONFIGURACAO DA EXECUCAO");
        sb.AppendLine($"#   escalonador           = {Escalonamento}");
        sb.AppendLine($"#   quantum               = {Quantum}");
        sb.AppendLine($"#   custo_troca_contexto  = {CustoTrocaContexto}");
        sb.AppendLine($"#   prioridade_preemptiva = {PrioridadePreemptiva}");
        sb.AppendLine($"#   envelhecimento        = {EnvelhecimentoAtivo} (intervalo={IntervaloEnvelhecimento})");
        sb.AppendLine($"#   tamanho_pagina        = {TamanhoPagina}");
        sb.AppendLine($"#   numero_molduras       = {NumeroMolduras}");
        sb.AppendLine($"#   substituicao_paginas  = {Substituicao}");
        sb.AppendLine($"#   tempo_atend_falta     = {TempoAtendimentoFalta}");
        sb.AppendLine($"#   tempo_servico_disp    = {TempoServicoDispositivoPadrao}");
        sb.AppendLine($"#   escalonamento_disco   = {EscalonamentoDisco}");
        sb.AppendLine($"#   total_blocos_disco    = {TotalBlocosDisco}");
        sb.AppendLine($"#   tamanho_bloco         = {TamanhoBloco}");
        sb.Append($"#   semente               = {Semente}");
        return sb.ToString();
    }
}
