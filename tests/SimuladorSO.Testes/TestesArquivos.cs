using SimuladorSO.Nucleo.Arquivos;

namespace SimuladorSO.Testes;

public static class TestesArquivos
{
    public static void Executar()
    {
        CicloBasico();
        DescritoresEPosicao();
        OperacoesInvalidas();
        Permissoes();
        Diretorios();
    }

    private static SistemaDeArquivos NovoFs() => new(totalBlocos: 64, tamanhoBloco: 100);

    private static void CicloBasico()
    {
        Verificador.DefinirContexto("Arquivos/CicloBasico");
        var fs = NovoFs();
        var d = new Dictionary<string, DescritorAberto>();
        fs.CriarArquivo("/a.txt", 0);
        fs.Abrir(d, "/a.txt", "h1", escrita: true, clock: 1);
        Verificador.Verdadeiro(d.ContainsKey("h1"), "descritor criado ao abrir");
        Verificador.Igual(1, fs.TabelaGlobal.Count, "uma entrada na tabela global");
        fs.Escrever(d, "h1", 250, clock: 2);   // 250 bytes -> 3 blocos (100 cada)
        var no = fs.Resolver("/a.txt")!;
        Verificador.Igual(250L, no.Tamanho, "tamanho apos escrita");
        Verificador.Igual(3, no.Blocos.Count, "3 blocos alocados (alocacao encadeada)");
        fs.Fechar(d, "h1");
        Verificador.Falso(d.ContainsKey("h1"), "descritor removido ao fechar");
        Verificador.Igual(0, fs.TabelaGlobal.Count, "tabela global esvazia ao fechar");
    }

    private static void DescritoresEPosicao()
    {
        Verificador.DefinirContexto("Arquivos/Descritores");
        var fs = NovoFs();
        var d = new Dictionary<string, DescritorAberto>();
        fs.CriarArquivo("/b.bin", 0);
        fs.Abrir(d, "/b.bin", "w", escrita: true, 0);
        fs.Escrever(d, "w", 120, 1);
        fs.Fechar(d, "w");
        fs.Abrir(d, "/b.bin", "r", escrita: false, 2);
        long lido = fs.Ler(d, "r", 50);
        Verificador.Igual(50L, lido, "le 50 dos 120 bytes");
        long lido2 = fs.Ler(d, "r", 100);
        Verificador.Igual(70L, lido2, "le apenas os 70 restantes");
        fs.Fechar(d, "r");
    }

    private static void OperacoesInvalidas()
    {
        Verificador.DefinirContexto("Arquivos/Invalidas");
        var fs = NovoFs();
        var d = new Dictionary<string, DescritorAberto>();
        Verificador.Lanca<ExcecaoSistemaArquivos>(
            () => fs.Abrir(d, "/naoexiste.txt", "h", false, 0), "abrir arquivo inexistente falha");
        Verificador.Lanca<ExcecaoSistemaArquivos>(
            () => fs.Ler(d, "handleInexistente", 10), "ler por descritor nao aberto falha");
        Verificador.Lanca<ExcecaoSistemaArquivos>(
            () => fs.Remover("/naoexiste", 0), "remover caminho inexistente falha");

        fs.CriarArquivo("/c.txt", 0);
        fs.Abrir(d, "/c.txt", "h1", true, 0);
        Verificador.Lanca<ExcecaoSistemaArquivos>(
            () => fs.Remover("/c.txt", 1), "remover arquivo aberto falha");

        // recriar arquivo existente deve falhar
        Verificador.Lanca<ExcecaoSistemaArquivos>(
            () => fs.CriarArquivo("/c.txt", 0), "criar arquivo duplicado falha");
    }

    private static void Permissoes()
    {
        Verificador.DefinirContexto("Arquivos/Permissoes");
        var fs = NovoFs();
        var d = new Dictionary<string, DescritorAberto>();
        fs.CriarArquivo("/somente_leitura.txt", 0, new Permissoes(leitura: true, escrita: false));
        Verificador.Lanca<ExcecaoSistemaArquivos>(
            () => fs.Abrir(d, "/somente_leitura.txt", "h", escrita: true, 0),
            "abrir para escrita sem permissao falha");
        fs.Abrir(d, "/somente_leitura.txt", "h", escrita: false, 0); // leitura ok
        Verificador.Verdadeiro(d.ContainsKey("h"), "abrir para leitura permitido");
    }

    private static void Diretorios()
    {
        Verificador.DefinirContexto("Arquivos/Diretorios");
        var fs = NovoFs();
        fs.CriarDiretorio("/docs", 0);
        fs.CriarArquivo("/docs/x.txt", 1);
        fs.CriarArquivo("/docs/y.txt", 1);
        var itens = fs.Listar("/docs");
        Verificador.Igual(2, itens.Count, "listagem com 2 itens");
        Verificador.Lanca<ExcecaoSistemaArquivos>(
            () => fs.CriarArquivo("/inexistente/z.txt", 0), "criar em diretorio inexistente falha");
    }
}
