# app/ - aplicacao figurada (mock)

Este diretorio hospeda uma **aplicacao figurada** com a mesma arquitetura do produto real
(Avalonia/.NET 10/MVVM, verificacao de licenca, leitura de SDK, armazenamento local de
sessao), mas com o nucleo de processamento de sinal substituido por um **stub** sem valor
comercial (por exemplo, um filtro generico ou gerador de ruido com a mesma assinatura de
funcao do modulo real).

Por que mock:

- preserva as superficies de ataque que importam para o trabalho (licenca, integridade do
  binario, arquivos locais), identicas as do produto real;
- nao expoe a propriedade intelectual da empresa (o pipeline de DSP);
- quando o processo estiver validado, basta trocar o stub pelo modulo real no ambiente
  fechado da empresa, e os controles ja vem testados.

Cuidado registrado: o stub precisa ter complexidade parecida com a do real no ponto que a
defesa protege. Para testar ocultacao de algoritmo, o stub deve ser um algoritmo de verdade,
nao um `return input`, senao a conclusao nao transfere.

> **Red team**: pelas regras de engajamento (`docs/06-plano-iteracoes.md`), o codigo daqui so
> e lido depois de registrar a tentativa em caixa preta contra o zip da release.

## Estrutura

```
NeuroMock.slnx
NeuroMock/            app Avalonia (WinExe): barreira de licenca, tela de treino, tela de ativacao
NeuroMock.Core/       logica pura, sem Avalonia: Licenciamento, Processamento (stub de DSP),
                      Sensor (sinal sintetico), Sessao (CSV local)
NeuroMock.Fornecedor/ lado do fornecedor: par de chaves e emissao de licenca (fora do pacote)
NeuroMock.Tests/      testes funcionais do Core e da emissao (xunit.v3)
ferramentas/          emissor de licenca do fornecedor (file-based app)
```

O `Directory.Build.props` fixa o que vale para os tres projetos: .NET 10, auditoria de
pacotes no restore (vulnerabilidade moderada ou pior quebra o build) e Release sem PDB, como o
pacote que vai para a clinica.

## Como rodar

```bash
dotnet test app/NeuroMock.slnx
```

```bash
dotnet run --project app/NeuroMock
```

Sem licenca, o app abre so a tela de ativacao: ela mostra o codigo da maquina e grava
`solicitacao-ativacao.json` na pasta do executavel (no `dotnet run`, em
`app/NeuroMock/bin/Debug/net10.0/`). O cliente manda esse arquivo ao fornecedor, que devolve o
`licenca.json` assinado para aquela maquina.

A partir da v1 a licenca e assinada com ECDSA P-256. O cliente so tem a chave publica
(embutida em `ChaveFornecedor.cs`); a privada fica com o fornecedor, fora de qualquer repo
(o emissor se recusa a grava-la dentro de um). Na esteira ela existe so como o secret
`NEUROMOCK_CHAVE_PRIVADA`.

Gerar o par de chaves, uma vez:

```bash
dotnet run app/ferramentas/emitir-licenca.cs -- gerar-chave ~/.neuromock/fornecedor-p256.pem
```

Emitir uma licenca para a maquina do cliente:

```bash
dotnet run app/ferramentas/emitir-licenca.cs -- emitir ~/.neuromock/fornecedor-p256.pem "Nome do cliente" 2026-12-31 solicitacao-ativacao.json
```

O pacote que o red team ataca e o zip win-x64 que a esteira publica em cada tag `v*`:
`dotnet publish app/NeuroMock/NeuroMock.csproj -c Release -r win-x64 --self-contained true`.

## Estado

- [x] Esqueleto Avalonia/.NET 10: `NeuroMock.slnx` com App, Core (sem Avalonia) e Tests
- [x] Verificacao de licenca (build 0: ingenua, de proposito)
- [x] Licenca v1: assinada (ECDSA P-256) e presa a maquina, com fluxo de ativacao
- [x] Stub do nucleo de processamento (filtros, FFT, indice por bandas, limiar adaptativo)
- [x] Sensor simulado com sinal sintetico
- Trial: a v0 nao tem; a folha F3.4 fica inativa ate o trial ser adotado.
- Launcher: fora do escopo das iteracoes. Nenhuma folha depende dele, e a release em zip
  cumpre o papel de canal de distribuicao.
