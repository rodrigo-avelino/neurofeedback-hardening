#:project ../NeuroMock.Core/NeuroMock.Core.csproj

// Emissor de licença do fornecedor (build 0).
// Uso: dotnet run app/ferramentas/emitir-licenca.cs -- "Nome do cliente" AAAA-MM-DD > licenca.json

using System;
using System.Globalization;
using NeuroMock.Core.Licenciamento;

if (args.Length != 2
    || !DateOnly.TryParseExact(args[1], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var validade))
{
    Console.Error.WriteLine("uso: dotnet run app/ferramentas/emitir-licenca.cs -- \"Nome do cliente\" AAAA-MM-DD");
    return 1;
}

Console.WriteLine(EmissorLicenca.Emitir(args[0], validade));
return 0;
