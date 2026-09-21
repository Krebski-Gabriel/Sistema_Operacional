using System.Globalization;
using System.Text;
using SimuladorSO.Nucleo.EntradaSaida;

namespace SimuladorSO.Nucleo.Metricas;

/// <summary>Gera a apresentação textual das métricas ao final da execução.</summary>
public static class RelatorioMetricas
{
    private static readonly CultureInfo Cult = CultureInfo.InvariantCulture;

    public static string Gerar(ColetorMetricas m, IReadOnlyDictionary<string, Dispositivo> dispositivos)
    {
        var sb = new StringBuilder();
        sb.AppendLine();
        sb.AppendLine("================= METRICAS =================");
        sb.AppendLine($"Tempo total simulado (clock)....... {m.TempoFinal}");
        sb.AppendLine($"Processos concluidos............... {m.ProcessosConcluidos}");
        sb.AppendLine($"Throughput (proc/u.t.)............. {m.Throughput.ToString("F4", Cult)}");
        sb.AppendLine($"Utilizacao da CPU (carga util)..... {(m.UtilizacaoCpu * 100).ToString("F2", Cult)}%");
        sb.AppendLine($"Sobrecarga de troca de contexto.... {(m.FracaoOverhead * 100).ToString("F2", Cult)}% " +
                      $"({m.TempoOverheadTroca} u.t.)");
        double ocioso = m.TempoFinal - m.UnidadesCpuUteis - m.TempoOverheadTroca;
        sb.AppendLine($"CPU ociosa......................... {(m.TempoFinal > 0 ? ocioso / m.TempoFinal * 100 : 0).ToString("F2", Cult)}% ({ocioso} u.t.)");
        sb.AppendLine($"Trocas de contexto................. {m.NumeroTrocasContexto}");
        sb.AppendLine($"Referencias a memoria.............. {m.TotalReferencias} (acertos={m.Acertos}, faltas={m.Faltas})");
        sb.AppendLine($"Taxa de faltas de pagina........... {(m.TaxaFaltas * 100).ToString("F2", Cult)}%");
        sb.AppendLine($"Carregamentos / substituicoes...... {m.Carregamentos} / {m.Substituicoes}");

        sb.AppendLine();
        sb.AppendLine("--- Por thread ---");
        sb.AppendLine($"{"Thread",-10} {"Chegada",8} {"Fim",8} {"Retorno",8} {"Espera",8} {"Resposta",9}");
        foreach (var mt in m.Threads.Values.OrderBy(t => t.Identificacao, StringComparer.Ordinal))
        {
            sb.AppendLine($"{mt.Identificacao,-10} {mt.Chegada,8} {mt.Fim,8} {mt.TempoRetorno,8} " +
                          $"{mt.TempoEspera,8} {mt.TempoResposta,9}");
        }
        var threads = m.Threads.Values.Where(t => t.Fim >= 0).ToList();
        if (threads.Count > 0)
        {
            sb.AppendLine($"{"MEDIA",-10} {"",8} {"",8} " +
                          $"{threads.Average(t => (double)t.TempoRetorno).ToString("F2", Cult),8} " +
                          $"{threads.Average(t => (double)t.TempoEspera).ToString("F2", Cult),8} " +
                          $"{threads.Average(t => (double)t.TempoResposta).ToString("F2", Cult),9}");
        }

        sb.AppendLine();
        sb.AppendLine("--- Por processo ---");
        sb.AppendLine($"{"Processo",-10} {"Chegada",8} {"Fim",8} {"Retorno",8}");
        foreach (var mp in m.Processos.Values.OrderBy(p => p.Id))
            sb.AppendLine($"{("P" + mp.Id),-10} {mp.Chegada,8} {mp.Fim,8} {mp.TempoRetorno,8}");

        sb.AppendLine();
        sb.AppendLine("--- Dispositivos ---");
        sb.AppendLine($"{"Dispositivo",-14} {"Tipo",-10} {"Politica",-8} {"Atendidas",10} {"Ocupado",8} {"Utiliz.",8} {"Desloc.",8}");
        foreach (var d in dispositivos.Values.OrderBy(d => d.Nome, StringComparer.Ordinal))
        {
            double util = m.TempoFinal > 0 ? (double)d.TempoOcupado / m.TempoFinal : 0;
            sb.AppendLine($"{d.Nome,-14} {d.Tipo,-10} {d.PoliticaNome,-8} {d.RequisicoesAtendidas,10} " +
                          $"{d.TempoOcupado,8} {(util * 100).ToString("F2", Cult) + "%",8} {d.DeslocamentoTotal,8}");
        }
        sb.AppendLine("===========================================");
        return sb.ToString();
    }

    /// <summary>Linha compacta com as principais médias, útil para experimentos comparativos.</summary>
    public static string LinhaResumo(string rotulo, ColetorMetricas m)
    {
        var threads = m.Threads.Values.Where(t => t.Fim >= 0).ToList();
        double retorno = threads.Count > 0 ? threads.Average(t => (double)t.TempoRetorno) : 0;
        double espera = threads.Count > 0 ? threads.Average(t => (double)t.TempoEspera) : 0;
        double resposta = threads.Count > 0 ? threads.Average(t => (double)t.TempoResposta) : 0;
        return string.Format(Cult,
            "{0,-22} | tf={1,5} | ret={2,7:F2} | esp={3,7:F2} | resp={4,7:F2} | util={5,6:F2}% | trocas={6,4} | faltas={7,4}",
            rotulo, m.TempoFinal, retorno, espera, resposta, m.UtilizacaoCpu * 100, m.NumeroTrocasContexto, m.Faltas);
    }
}
