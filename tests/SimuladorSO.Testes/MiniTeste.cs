namespace SimuladorSO.Testes;

/// <summary>
/// Arcabouço mínimo de asserções para os testes automatizados. Não depende de
/// bibliotecas externas, o que permite executar os testes em ambiente limpo e
/// offline com um simples "dotnet run".
/// </summary>
public static class Verificador
{
    public static int Total { get; private set; }
    public static int Falhas { get; private set; }
    public static readonly List<string> Mensagens = new();
    private static string _contexto = "";

    public static void DefinirContexto(string contexto) => _contexto = contexto;

    public static void Igual<T>(T esperado, T obtido, string mensagem)
    {
        Total++;
        if (!EqualityComparer<T>.Default.Equals(esperado, obtido))
        {
            Falhas++;
            Mensagens.Add($"[FALHA] {_contexto} :: {mensagem} — esperado <{esperado}>, obtido <{obtido}>");
        }
    }

    public static void Verdadeiro(bool condicao, string mensagem)
    {
        Total++;
        if (!condicao)
        {
            Falhas++;
            Mensagens.Add($"[FALHA] {_contexto} :: {mensagem}");
        }
    }

    public static void Falso(bool condicao, string mensagem) => Verdadeiro(!condicao, mensagem);

    public static void Lanca<TEx>(Action acao, string mensagem) where TEx : Exception
    {
        Total++;
        try
        {
            acao();
            Falhas++;
            Mensagens.Add($"[FALHA] {_contexto} :: {mensagem} — esperava excecao {typeof(TEx).Name}, nada foi lancado");
        }
        catch (TEx) { /* esperado */ }
        catch (Exception e)
        {
            Falhas++;
            Mensagens.Add($"[FALHA] {_contexto} :: {mensagem} — excecao inesperada {e.GetType().Name}: {e.Message}");
        }
    }
}
