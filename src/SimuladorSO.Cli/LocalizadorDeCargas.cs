namespace SimuladorSO.Cli;

/// <summary>
/// Descobre a pasta "cargas/" do repositório mesmo quando o simulador é
/// executado de outro diretório (por exemplo, a partir de bin/Release). Isso
/// permite usar atalhos como "--carga mista" em vez do caminho completo.
/// </summary>
public static class LocalizadorDeCargas
{
    /// <summary>Procura a pasta "cargas" subindo a partir do diretório atual e do binário.</summary>
    public static string? PastaDeCargas()
    {
        foreach (var inicio in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            var dir = new DirectoryInfo(inicio);
            for (int i = 0; i < 8 && dir != null; i++, dir = dir.Parent)
            {
                var candidato = Path.Combine(dir.FullName, "cargas");
                if (Directory.Exists(candidato))
                    return candidato;
            }
        }
        return null;
    }

    /// <summary>Lista os arquivos de carga disponíveis, em ordem alfabética.</summary>
    public static IReadOnlyList<string> Disponiveis()
    {
        var pasta = PastaDeCargas();
        if (pasta == null) return Array.Empty<string>();
        var arquivos = Directory.GetFiles(pasta, "*.txt");
        Array.Sort(arquivos, StringComparer.Ordinal);
        return arquivos;
    }

    /// <summary>
    /// Resolve o que o usuário digitou em um caminho de arquivo existente.
    /// Aceita o caminho completo, o nome do arquivo ou apenas o nome curto
    /// (por exemplo "mista", "mista.txt" ou "cargas/mista.txt").
    /// </summary>
    public static string? Resolver(string entrada)
    {
        if (string.IsNullOrWhiteSpace(entrada)) return null;
        if (File.Exists(entrada)) return entrada;

        var pasta = PastaDeCargas();
        if (pasta == null) return null;

        foreach (var nome in new[] { entrada, entrada + ".txt" })
        {
            var candidato = Path.Combine(pasta, Path.GetFileName(nome));
            if (File.Exists(candidato)) return candidato;
        }
        return null;
    }

    /// <summary>Texto amigável listando as cargas prontas (para ajuda e mensagens de erro).</summary>
    public static string TextoDisponiveis()
    {
        var arquivos = Disponiveis();
        if (arquivos.Count == 0)
            return "  (pasta 'cargas/' nao encontrada a partir do diretorio atual)";
        return string.Join(Environment.NewLine,
            arquivos.Select(a => $"  {Path.GetFileNameWithoutExtension(a),-22} {DescricaoDe(a)}"));
    }

    /// <summary>Lê a primeira linha de comentário do arquivo como descrição curta.</summary>
    public static string DescricaoDe(string caminho)
    {
        try
        {
            foreach (var linha in File.ReadLines(caminho))
            {
                var t = linha.Trim();
                if (t.StartsWith('#'))
                {
                    var texto = t.TrimStart('#', ' ');
                    if (texto.Length > 0) return texto.Length > 60 ? texto[..60] + "..." : texto;
                }
                else if (t.Length > 0)
                {
                    break;
                }
            }
        }
        catch (IOException) { /* descrição é opcional */ }
        return "";
    }
}
