# Plano das iterações (novembro de 2026)

Como os ciclos red/blue rodam na prática: decisões de partida, sequência de builds e calendário até a apresentação, ritual de cada
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

| Build | Sai em | Controle principal | Folhas que tenta fechar |
|---|---|---|---|
| v0 | ter 03/11 (saiu) | nenhum (baseline de propósito) | nenhuma; mede o ponto de partida |
| v1 | qui 05/11 | licença assinada (ECDSA P-256, só a chave pública no cliente) + binding de hardware | G3 |
| v2 | ter 10/11 | integridade do binário: manifesto de hashes assinado, checagens espalhadas, resposta atrasada | G1 |
| v3 | qui 12/11 | anti-debug, anti-hook e ofuscação (renomeação, constantes, fluxo de controle) | G2, H1.2, H2.2 |
| v4 | ter 17/11 | Native AOT com metadados enxutos | H1.1, H2.1 e os ataques em IL de G1 e G2 |
| `hipotese/dsp-servidor` | recomendação no relatório | DSP no servidor | árvore 2 inteira; acorda o ramo G4 |

A ordem segue a lógica de custo do ASPIRE: cada build fecha o caminho mais barato que sobrou
na anterior. Na v0, copiar ou editar a licença custa minutos e nenhuma habilidade. Com a
licença assinada (v1), o caminho mais barato vira patchear o `if`, o que demonstra na prática
que a raiz do problema é a integridade do binário. A v2 empurra o atacante para a execução
(G2), a v3 encarece G2 e a leitura do algoritmo, e a v4 tira o IL de cena. A ordem é hipótese,
não contrato: se o red achar um caminho mais barato, a build seguinte muda de alvo.

## Calendário até a apresentação (24/11)

A apresentação foi antecipada para ter 24/11. O time se encontra presencialmente às terças e
quintas, e cada encontro fecha um ciclo: a reunião red/blue consolida a build atual e lança a
seguinte.

| Encontro | Fecha | Lança | Testes do red que precisam estar na `main` |
|---|---|---|---|
| qui 05/11 | v0 | v1 | T-F3.1, T-F3.2, T-F3.3 (só mexem no `licenca.json`, sem patch no binário) |
| ter 10/11 | v1 | v2 | T-F1.1, T-F1.2, T-F1.3 |
| qui 12/11 | v2 | v3 | T-F2.x, T-H1.2, T-H2.2 |
| ter 17/11 | v3 | v4 | T-H1.1, T-H2.1 |
| qui 19/11 | v4 | nada | consolidação final; até 24/11 é documentação e ensaio |

Regras para caber no calendário:

- **O blue anda uma build à frente.** Quando o encontro começa, a build seguinte já está pronta
  numa branch; o encontro só consolida.
- **Sem teste, a folha não fecha.** Pela regra de ouro, a folha só vira `fechada` com o teste
  dela na `main`. Se o red não conseguir escrever um teste a tempo, o blue pode escrevê-lo a
  partir dos passos da issue, depois que a tentativa em caixa preta foi registrada: a regra de o
  blue não escrever teste existe para não revelar o alvo antes do ataque.
- **F2.1 e F2.3 podem ser medidas na mão.** Debugger e edição de memória são difíceis de
  automatizar na esteira. O custo vai para o CSV e a matriz registra que a folha não tem
  regressão automática.
- **Fusível.** Se um ciclo atrasar, a v4 vira recomendação no relatório e o dia 19 não se move.
  As hipóteses de servidor ficam como recomendação argumentada.
- **Nota da iteração no próprio encontro**, para o dia 19 ser montagem, não escrita do zero.

## Ritual de um ciclo

| Etapa | Quem | O quê |
|---|---|---|
| Entre encontros | red | ataca a build atual em caixa preta, registra toda tentativa no CSV de custo, abre issue para cada ataque com sucesso e escreve o teste com a folha `aberta` |
| Entre encontros | blue | prepara a build seguinte numa branch: o controle planejado e a reação aos achados, em PRs `fix/<folha>` |
| No encontro | todos | reunião red/blue: o red mostra o que achou e quanto custou; os testes do red entram na `main`; no PR da build seguinte, as folhas que ela fecha viram `fechada`; merge, coluna da build na matriz, nota da iteração e tag `v(N+1)` |

A reunião red/blue é o momento "purple" do purple teaming: os dois times juntos olhando o mesmo
resultado. Se alguém faltar a um encontro, vale por escrito (uma issue ou uma thread); o que
importa é sair dela com o estado de cada folha combinado entre os dois lados.

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
- Timebox de 2h por folha por build. É um teto, não uma cota: folha que cai em minutos não
  consome o resto, e o teto só pesa nas que resistem. Ele fica alto para preservar a medição:
  com teto baixo, as builds finais viram todas "mais de 1h" e a curva de custo achata onde
  deveria subir. Se estourar sem sucesso, a folha conta como fechada naquela build, com custo
  "mais de 2h".
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
- [ ] Convidar o red team como colaborador.
- [ ] Criar o label `ataque`, usado pelo template de issue.
- [ ] Proteger a `main` com um ruleset: exigir PR, exigir os checks da esteira e bloquear force
      push. Os checks só aparecem para seleção depois da primeira execução da esteira.
- [ ] Ligar os alertas do Dependabot e o secret scanning com push protection, gratuitos em repo
      público.

## Backlog fora da sequência de builds

- Atualizar o DFD de `01-threat-model.md`: o produto real é distribuído por instalador, sem
  auto-update, e tem um canal local (loopback) de integração com jogos, que deve entrar como
  superfície registrada e fora de escopo.
- DFD e árvore 2 como diagramas em `docs/assets/`.
- Fixar as actions por SHA e ligar o Dependabot para atualizá-las.
- Revisar a coluna ASPIRE da matriz ao implementar cada controle.
