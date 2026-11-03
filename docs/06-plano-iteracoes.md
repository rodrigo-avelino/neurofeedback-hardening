# Plano das iterações (novembro de 2026)

Como os ciclos red/blue rodam na prática: decisões de partida, sequência de builds, ritual de cada
ciclo, o que conta como build pronta, como um ataque vira teste sem travar a `main` e como o
custo de ataque é medido. Complementa o processo descrito em `05-tds-process.md`.

## Decisões de partida

| Decisão | Escolha | Por quê |
|---|---|---|
| Visibilidade do repo | público | CodeQL, proteção de branch e Actions saem de graça; o app é um mock justamente para poder ser exposto. O nome do produto real não entra no repo. |
| Onde roda a regressão MATE | runner Windows na CI | o alvo e o red team são Windows; Native AOT para win-x64 não compila a partir de Linux; anti-debug e binding de hardware usam a API do Windows |
| Papel do devcontainer | higiene e análise automatizada em Linux | SDK fixado pelo `global.json` e ferramentas pelo `dotnet-tools.json`, as mesmas versões da CI |
| Linguagem dos testes | C# (xunit.v3) com dnlib | mesmo stack do app real; dnlib é a biblioteca de manipulação de assembly do dnSpy |
| Modo do red team | caixa preta primeiro, caixa branca depois | o custo medido só vale para o MATE se o atacante não leu o código; a caixa branca vira uma segunda medição, registrada à parte |

## Sequência de builds

| Build | Controle principal | Folhas que tenta fechar |
|---|---|---|
| v0 | nenhum (baseline de propósito), entregue em 03/11 | nenhuma; mede o ponto de partida |
| v1 | licença assinada (ECDSA P-256, só a chave pública no cliente) + binding de hardware | G3 |
| v2 | integridade do binário: manifesto de hashes assinado, checagens espalhadas, resposta atrasada | G1 |
| v3 | anti-debug, anti-hook e ofuscação (renomeação, constantes, fluxo de controle) | G2, H1.2, H2.2 |
| v4 | Native AOT com metadados enxutos | H1.1, H2.1 e os ataques em IL de G1 e G2 |
| `hipotese/dsp-servidor` | DSP no servidor | árvore 2 inteira; acorda o ramo G4 |

A ordem segue a lógica de custo do ASPIRE: cada build fecha o caminho mais barato que sobrou
na anterior. Na v0, copiar ou editar a licença custa minutos e nenhuma habilidade. Com a
licença assinada (v1), o caminho mais barato vira patchear o `if`, o que demonstra na prática
que a raiz do problema é a integridade do binário. A v2 empurra o atacante para a execução
(G2), a v3 encarece G2 e a leitura do algoritmo, e a v4 tira o IL de cena. A ordem é hipótese,
não contrato: se o red achar um caminho mais barato, a build seguinte muda de alvo.

**Sem datas fixas por build.** Um ciclo fecha quando duas coisas aconteceram: o red passou por
todas as folhas ativas da build (sucesso, falha ou timebox, tudo no CSV) e o blue mergeou as
correções da build seguinte. A única data firme é o fim de novembro para os ciclos. Como a v0
saiu adiantada, a folga vira ciclos a mais (v5 em diante, ou a `hipotese/dsp-servidor`) ou mais
tempo de documentação, conforme o que os resultados pedirem.

## Ritual de um ciclo

| Etapa | Quem | O quê |
|---|---|---|
| Abertura | blue | cria a tag `vN`; a CI publica a release (zip win-x64 + SHA-256) |
| Ataque | red | ataca a `vN` em caixa preta, registra toda tentativa no CSV de custo, abre issue para cada ataque com sucesso e sobe o teste com a folha `aberta` |
| Meio do ciclo | todos | reunião red/blue de 30 min: o red mostra o que achou, os dois combinam o estado de cada folha, o blue fecha o escopo da `v(N+1)` |
| Correções | blue | em paralelo ao ataque, implementa o controle planejado e reage aos achados, em PRs `fix/<folha>` |
| Fechamento | blue | merge, coluna da build na matriz, nota da iteração e tag `v(N+1)` |

A reunião red/blue é o momento "purple" do purple teaming: os dois times juntos olhando o mesmo
resultado. Pode ser chamada de voz ou por escrito (uma issue ou uma thread); o que importa é
sair dela com o estado de cada folha combinado entre os dois lados.

## Build pronta

Uma build `vN` só é entregue ao red team quando:

- [ ] a esteira está verde na `main`;
- [ ] a tag `vN` existe e a CI publicou a release com o SHA-256;
- [ ] `security-tests/estado-folhas.json` declara o estado esperado de cada folha;
- [ ] a coluna `vN` da matriz (`04-controls-traceability.md`) está preenchida;
- [ ] existe a nota da iteração em `docs/iteracoes/vN.md` (modelo em
      `docs/iteracoes/modelo.md`).

## Como um ataque vira teste sem travar a main

No MATE sempre sobra folha aberta. Se o teste de uma folha aberta simplesmente falhasse, a
esteira nunca ficaria verde e a proteção de branch travaria a `main` para sempre. Por isso cada
folha tem um estado declarado em `security-tests/estado-folhas.json`, e o teste compara o
resultado do ataque com esse estado:

| Estado declarado | O ataque... | Resultado | Significado |
|---|---|---|---|
| `aberta` | funcionou | verde | buraco conhecido e documentado |
| `aberta` | falhou | vermelho | a folha fechou: atualizar o estado e a matriz |
| `fechada` | falhou | verde | o controle resiste |
| `fechada` | funcionou | vermelho | regressão |

O TDD fica assim:

1. O red sobe o teste com a folha `aberta`. A esteira fica verde e o buraco fica registrado.
2. O blue abre `fix/<folha>`. O primeiro commit muda a folha para `fechada` e a esteira fica
   vermelha.
3. O controle entra e a esteira fica verde.
4. Merge. A partir daí, se a folha reabrir, a esteira acusa a regressão.

**Controle positivo.** Antes de cada ataque, o teste confirma que o app original abre com uma
licença válida e recusa sem licença. Se isso falhar, o resultado é inconclusivo (erro), nunca
`fechada`: um build quebrado que nem abre não pode parecer seguro.

**Observação de fora.** O teste observa o app como o atacante observaria: roda o executável e
verifica se a janela principal abriu. Nada de flag ou modo de teste dentro do binário, que
seria superfície de ataque a mais.

## Medição do custo de ataque

O custo é a métrica principal do trabalho (`00-ideacao.md`), então ele é registrado desde a v0.

- Toda tentativa entra em `docs/metricas/custo-ataques.csv`, inclusive as que falharam. Uma
  folha fechada sem registro de tentativa não prova nada.
- Timebox de 2h por folha por build. Se estourar sem sucesso, a folha conta como fechada
  naquela build, com custo "mais de 2h".
- O modo é registrado: `preta` (só o artefato da release) ou `branca` (consultou `app/`).
- Na árvore 2, o red vence quando entrega uma reimplementação que reproduz a saída do stub num
  sinal de entrada que ele não viu (oráculo). O tempo até chegar lá é o custo de H2.

Colunas do CSV:

| Coluna | Exemplo | O que é |
|---|---|---|
| `data` | `2026-11-10` | dia da tentativa |
| `build` | `v0` | tag atacada |
| `folha` | `F1.2` | folha da árvore |
| `modo` | `preta` | `preta` ou `branca` |
| `atacante` | `nome` | quem atacou |
| `ferramentas` | `dnSpyEx` | ferramentas usadas |
| `minutos` | `35` | tempo gasto na tentativa |
| `resultado` | `sucesso` | `sucesso`, `falha` ou `timebox` |
| `evidencia` | link da issue | issue, print ou script |

## Regras de engajamento do red team

- Atacar só o zip da release da tag, conferindo o SHA-256.
- Caixa preta primeiro: só abrir `app/` depois de registrar a tentativa em caixa preta.
- O ambiente de ataque é a máquina pessoal de cada um (MATE). A infraestrutura (GitHub, CI,
  máquinas dos colegas) não é alvo.
- Binário adulterado não entra no repo: só scripts, passos e hashes.

## Estrutura combinada

```
app/
  NeuroMock.slnx
  NeuroMock/            app Avalonia (WinExe): telas e barreira de licença
  NeuroMock.Core/       lógica pura, sem Avalonia: licença, stub de DSP, sessão
  NeuroMock.Tests/      testes funcionais (xunit.v3)
security-tests/
  run-all.ps1           ponto de entrada da regressão MATE (CI e local)
  estado-folhas.json    estado esperado de cada folha na build atual
  ...                   projeto xunit.v3 + dnlib com os testes T-<folha>
docs/metricas/custo-ataques.csv
docs/iteracoes/vN.md
```

O app espelha a arquitetura do produto real: .NET 10, Avalonia 11.3, CommunityToolkit.Mvvm,
App e Core separados, publicação self-contained win-x64 sem single-file, sem ReadyToRun, sem
trimming e sem PDB. O sensor é simulado com sinal sintético. O launcher fica de fora: nenhuma
folha depende dele, e a release em zip cumpre o papel de canal de distribuição.

A esteira (`.github/workflows/security.yml`) já procura `app/NeuroMock/NeuroMock.csproj` e
`security-tests/run-all.ps1`. Enquanto eles não existirem, os jobs que dependem deles são
pulados.

## Configuração manual no GitHub

- [x] Repo público.
- [x] Convidar o red team como colaborador.
- [x] Criar o label `ataque`, usado pelo template de issue.
- [x] Proteger a `main` com um ruleset (e as tags `v*` com outro, que impede mover ou apagar): exigir PR, exigir os checks da esteira e bloquear force
      push. Os checks só aparecem para seleção depois da primeira execução da esteira.
- [x] Ligar os alertas do Dependabot e o secret scanning com push protection, gratuitos em repo
      público.
- [ ] A partir da v1: secret `NEUROMOCK_CHAVE_PRIVADA` com a chave privada do fornecedor, para o
      controle positivo emitir licença na esteira. Risco registrado: colaborador com escrita
      consegue ler um secret por um PR que altere a esteira. Atacar a esteira está fora das regras
      de engajamento; se o time quiser fechar esse caminho de verdade, o secret vai para um
      environment com aprovação manual do blue.

## Backlog fora da sequência de builds

- Atualizar o DFD de `01-threat-model.md`: o produto real é distribuído por instalador, sem
  auto-update, e tem um canal local (loopback) de integração com jogos, que deve entrar como
  superfície registrada e fora de escopo.
- DFD e árvore 2 como diagramas em `docs/assets/`.
- Fixar as actions por SHA e ligar o Dependabot para atualizá-las.
- Revisar a coluna ASPIRE da matriz ao implementar cada controle.
