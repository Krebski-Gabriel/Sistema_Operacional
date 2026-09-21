using SimuladorSO.Testes;

// Executa todas as suítes de teste automatizadas. Retorna código de saída 0
// quando todos os testes passam e 1 caso haja qualquer falha, permitindo uso
// em integração contínua ("dotnet run" na pasta do projeto de testes).

Console.WriteLine("Executando testes automatizados do Simulador de SO...\n");

TestesMemoria.Executar();
TestesEscalonamento.Executar();
TestesArquivos.Executar();
TestesIntegracao.Executar();

Console.WriteLine($"Testes executados: {Verificador.Total}");
Console.WriteLine($"Falhas:            {Verificador.Falhas}");

if (Verificador.Falhas > 0)
{
    Console.WriteLine();
    foreach (var m in Verificador.Mensagens)
        Console.WriteLine(m);
    Console.WriteLine("\nRESULTADO: FALHOU");
    return 1;
}

Console.WriteLine("\nRESULTADO: TODOS OS TESTES PASSARAM");
return 0;
