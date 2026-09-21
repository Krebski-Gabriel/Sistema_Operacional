using SimuladorSO.Nucleo.Memoria;

namespace SimuladorSO.Testes;

public static class TestesMemoria
{
    public static void Executar()
    {
        TraducaoDeEnderecos();
        FaltaEDepoisAcerto();
        SubstituicaoFifo();
        SubstituicaoLru();
    }

    private static void TraducaoDeEnderecos()
    {
        Verificador.DefinirContexto("Memoria/Traducao");
        var g = new GerenciadorMemoria(tamanhoPagina: 4, numeroMolduras: 4, new SubstituicaoFifo());
        Verificador.Igual(0L, g.PaginaDe(0), "endereco 0 -> pagina 0");
        Verificador.Igual(0L, g.PaginaDe(3), "endereco 3 -> pagina 0");
        Verificador.Igual(1L, g.PaginaDe(4), "endereco 4 -> pagina 1");
        Verificador.Igual(2L, g.PaginaDe(9), "endereco 9 -> pagina 2");
        Verificador.Igual(1L, g.DeslocamentoDe(9), "deslocamento de 9 (pagina 4) = 1");
    }

    private static void FaltaEDepoisAcerto()
    {
        Verificador.DefinirContexto("Memoria/Falta");
        var g = new GerenciadorMemoria(4, 4, new SubstituicaoFifo());
        var tab = new TabelaDePaginas();
        Verificador.Igual(ResultadoAcesso.Falta, g.Acessar(tab, 0), "primeiro acesso gera falta");
        var carga = g.TratarFalta(1, tab, 0);
        Verificador.Falso(carga.HouveSubstituicao, "primeira carga nao substitui");
        Verificador.Igual(ResultadoAcesso.Acerto, g.Acessar(tab, 0), "apos carregamento vira acerto");
        Verificador.Igual(ResultadoAcesso.Acerto, g.Acessar(tab, 3), "mesma pagina, deslocamento diferente, acerto");
    }

    private static void SubstituicaoFifo()
    {
        Verificador.DefinirContexto("Memoria/FIFO");
        var g = new GerenciadorMemoria(4, 1, new SubstituicaoFifo()); // 1 moldura forca substituicao
        var tab = new TabelaDePaginas();
        g.Acessar(tab, 0); g.TratarFalta(1, tab, 0);                 // pagina 0
        Verificador.Igual(ResultadoAcesso.Acerto, g.Acessar(tab, 0), "pagina 0 residente");
        g.Acessar(tab, 4);                                          // pagina 1 -> falta
        var carga = g.TratarFalta(1, tab, 4);
        Verificador.Verdadeiro(carga.HouveSubstituicao, "carregar pagina 1 substitui pagina 0");
        Verificador.Igual(0L, carga.PaginaVitima, "vitima FIFO e a pagina 0");
        Verificador.Igual(ResultadoAcesso.Falta, g.Acessar(tab, 0), "pagina 0 foi removida -> falta");
    }

    private static void SubstituicaoLru()
    {
        Verificador.DefinirContexto("Memoria/LRU");
        var g = new GerenciadorMemoria(4, 2, new SubstituicaoLru()); // 2 molduras
        var tab = new TabelaDePaginas();
        g.Acessar(tab, 0); g.TratarFalta(1, tab, 0);   // pagina 0
        g.Acessar(tab, 4); g.TratarFalta(1, tab, 4);   // pagina 1
        g.Acessar(tab, 0);                             // usa pagina 0 (0 vira mais recente)
        g.Acessar(tab, 8);                             // pagina 2 -> falta
        var carga = g.TratarFalta(1, tab, 8);
        Verificador.Verdadeiro(carga.HouveSubstituicao, "houve substituicao");
        Verificador.Igual(1L, carga.PaginaVitima, "LRU remove a pagina 1 (menos recente)");
        Verificador.Igual(ResultadoAcesso.Acerto, g.Acessar(tab, 0), "pagina 0 permanece residente");
    }
}
