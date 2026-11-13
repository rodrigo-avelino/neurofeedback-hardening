# Contexto do projeto (para o Claude Code)

Este repositório documenta e executa o endurecimento de segurança de um aplicativo de
neurofeedback, como trabalho da cadeira de Segurança no Ciberespaço (turma T320-16, prof. José
Alzir Bruno Falcão). Leia este arquivo antes de agir no repo.

## O que é

- **Área do CyBOK**: Ciclo de Vida de Desenvolvimento Seguro (Secure Software Lifecycle), dentro
  de Segurança de Software e Plataforma; apoio em Software Security.
- **Método**: Test-Driven Security com purple teaming. Blue team constrói controles, red team
  ataca, cada ataque vira teste de regressão.
- **Objetivo (honesto)**: em cenário MATE não existe proteção absoluta. A métrica é elevar o
  custo do ataque acima do valor do ativo, medido a cada iteração.

## O software

- App real: produto comercial da empresa (o nome fica fora deste repo, que é público),
  Avalonia/.NET 10/MVVM, EEG BrainBit Flex 4 via XNeuroSDK, 100% local, distribuído por
  instalador sem auto-update, sem nenhum controle de segurança ("white canvas").
- **Neste repo o app é um mock** (`app/`): mesma arquitetura e superfícies de ataque, mas com o
  núcleo de processamento de sinal substituído por um stub sem valor comercial. Isso preserva o
  que importa para o trabalho sem expor a propriedade intelectual da empresa. Não colocar o
  algoritmo real da empresa aqui.

## Modelo de atacante e histórias

- Modelo: Man-At-The-End (MATE), o adversário controla a própria máquina de execução.
- Duas histórias (ver `docs/02-abuse-cases.md`):
  1. **Pirataria**: usar sem licença válida.
  2. **Roubo do algoritmo**: recuperar o pipeline de DSP a partir do binário.
- Fora de escopo (decidido com o orientador Joel Sotero): dados de paciente (EEG de um sensor
  isolado tem baixo valor) e ataques de rede (software local).

## Decisões já tomadas

- Fundamentação em **CyBOK + MATE/ASPIRE/MASVS-RESILIENCE**. MITRE ATT&CK não encaixa bem em
  cenário MATE (é taxonomia de intrusão de rede); se o enunciado exigir, entra só como
  mapeamento lateral de Defense Evasion.
- A hardkey de licença é do software legado; o app novo parte do zero.
- Repo público desde 02/11/2026: o nome do produto e da empresa reais não entram aqui (docs,
  código, commits, PRs).
- Esteira no GitHub Actions em duas camadas: higiene (SAST/SCA/segredos, em Linux) e regressão
  MATE (em runner Windows, contra o zip da release). Em tag `v*` publica a release com SHA-256.
  Branch protection barra merge com esteira vermelha.
- Ambiente de ataque é a máquina pessoal do red team (fiel ao MATE). Reprodutibilidade vem de
  versões fixadas (`global.json` para o SDK, `dotnet-tools.json` para as ferramentas), iguais no
  devcontainer (Linux: higiene e análise) e na CI.
- Testes de regressão em C# (xunit.v3) com dnlib. Cada folha tem estado declarado em
  `security-tests/estado-folhas.json`; a esteira só falha em regressão ou em estado
  desatualizado. Todo teste tem controle positivo e observa o app de fora, sem flag de teste no
  binário.
- Red team ataca em caixa preta primeiro; caixa branca é uma medição separada. Toda tentativa,
  inclusive as que falham, vai para `docs/metricas/custo-ataques.csv` (timebox de 2h por folha, teto e não cota).
- Launcher fora do escopo do mock: a release em zip é o canal de distribuição.
- Licença v1 (03/11/2026, branch `feat/v1-licenca-assinada`): ECDSA P-256 sobre cliente,
  validade e código da máquina; o cliente só tem a chave pública (`ChaveFornecedor.cs`). Código
  da máquina = hash de um identificador do sistema. Sem licença, o app grava
  `solicitacao-ativacao.json` com o código. Emissão em `app/NeuroMock.Fornecedor`, fora do
  pacote. A chave privada fica em `~/.neuromock/` na máquina do blue e, na esteira, no secret
  `NEUROMOCK_CHAVE_PRIVADA`; nunca no repo. O controle positivo emite a licença na hora para a
  máquina do runner; sem a chave, fora da esteira, ele fica parcial. A v1 só entra na `main`
  depois dos testes T-F3.x do red (regra de ouro).
- Integridade v2 (05/11/2026, branch `feat/v2-integridade`, contra o ramo G1): `manifesto.json`
  com o SHA-256 de todos os arquivos do pacote, assinado com a mesma chave das licenças por
  `app/ferramentas/assinar-pacote.cs`, no job `build-release` (o secret passa a chegar lá, só no
  passo da assinatura). Duas guardas diferentes (pacote inteiro com a chave do Core; arquivos de
  onde o código em execução foi carregado, com uma segunda cópia da chave, mascarada, no App) e
  duas respostas atrasadas em outros pontos, com falha fechada: o treino fecha sozinho entre 4 e
  14 s depois de abrir. Só em Release; nomes claros de propósito (renomear é a v3). O harness
  observa o treino por 30 s: `JanelaPrincipalAbriu` passou a significar aberto e continuou
  aberto. Os T-F1.x do red (PRs #17, #19 e #21, medidos contra a v1 em 5 a 15 min) entraram no
  PR da v2 (#15), que vira F1.1 a F1.3 para `fechada` (09/11/2026). Desenho em
  `docs/06-plano-iteracoes.md`.
- Ofuscação e execução v3 (09/11/2026, contra o ramo G2 e a árvore 2): o Obfuscar (gratuito,
  com a versão fixada no `app/ferramentas/ofuscar-pacote.ps1`) renomeia só o algoritmo (namespaces
  `Processamento` e `Sensor` do Core) por `app/ferramentas/ofuscar-pacote.ps1`, depois do publish
  e antes de assinar o manifesto (a ofuscação muda os bytes que o manifesto cobre), no job
  `build-release` e no teste local. `Licenciamento` e `Integridade` ficam legíveis de propósito,
  para os T-F1.x do red seguirem válidos pelo nome (decidido com o Rodrigo); a API pública é
  preservada, para o App não ofuscado continuar ligando. O fluxo de controle fica de fora (lacuna
  registrada, teto honesto; vem na v4 com Native AOT), por falta de ferramenta gratuita estável no
  .NET 10 com Avalonia. Anti-debug (F2.1), anti-hook por integridade em runtime (F2.2) e estado de
  licença redundante com reverificação (F2.3) alimentam as mesmas duas respostas atrasadas da v2,
  falha fechada, só em Release. Os parâmetros do DSP saíram do `parametros.json` em claro para um
  bloco mascarado decodificado em runtime (H1.2). Controle positivo completo e T-F1.x/T-F3.x
  verdes contra o zip v3 ofuscado e assinado; as folhas F2.x, H1.2 e H2.2 só viram `fechada`
  quando os testes do red chegarem (qui 12/11). Desenho em `docs/06-plano-iteracoes.md`, seção
  "Desenho da v3".
- Native AOT v4 (11/11/2026, branch `claude/v4-native-aot-cb46fc`, contra os ataques em IL de G1 e
  G2 e a árvore 2): `PublishAot` no csproj do App e `IsAotCompatible` no Core. O publish win-x64
  vira um único `NeuroMock.exe` nativo (mais as libs nativas do Avalonia: Skia, HarfBuzz, ANGLE),
  sem IL nem metadados gerenciados: somem o `NeuroMock.dll`, o `NeuroMock.Core.dll`, o `deps.json`
  e o `runtimeconfig.json`. Os ataques do red em IL (T-F1.x, stub, hook por
  `DOTNET_STARTUP_HOOKS`/Harmony) e o oráculo por reflexão (T-H1.2, T-H2.2) perdem o alvo
  gerenciado; encarece a análise estática do algoritmo (H1.1, H2.1). A guarda 2 da v2 passa a
  conferir o hash da imagem nativa (`Environment.ProcessPath`) contra o manifesto, porque
  `Assembly.Location` e os `.json` do runtime somem no AOT; a guarda 1, o manifesto assinado e as
  respostas atrasadas seguem. O Obfuscar sai do `build-release` (não há mais `Core.dll` a renomear;
  o AOT subsume e cobre a lacuna de fluxo de controle da v3). O `global.json` fixa o SDK 10.0.403
  (ILCompiler 10.0.14, fora do advisory alto GHSA-p3jp-9v83-76w9 do 10.0.12 que o SDK 10.0.401
  traz); fixar o ILCompiler por `PackageReference` quebra o AOT. Link nativo usa o MSVC (vswhere no
  PATH; automático no `windows-latest`). Teto honesto: H2.2 não fecha, o DSP ainda roda no cliente
  (só o servidor fecha). Controle positivo verificado contra o zip AOT assinado; os testes do red
  re-expressos contra o nativo (T-F1.x, T-F2.2, T-H1.2, T-H2.2) e os novos T-H1.1/T-H2.1 chegam no
  encontro de ter 17/11. Desenho em `docs/06-plano-iteracoes.md`, seção "Desenho da v4".
- Calendário até a apresentação de ter 24/11 (decidido em 03/11/2026): um ciclo por encontro
  presencial de terça e quinta. Qui 05/11 fecha a v0 e lança a v1; ter 10/11 a v2; qui 12/11 a
  v3; ter 17/11 a v4; qui 19/11 fecha tudo; depois só documentação e ensaio. O blue anda uma
  build à frente numa branch; os testes do red das folhas que a build seguinte fecha precisam
  estar na `main` antes do encontro (tabela no plano). Se faltar teste, o blue pode escrevê-lo
  a partir da issue depois da tentativa em caixa preta. Fusível: a v4 vira recomendação. A
  reunião de cada encontro se chama "reunião red/blue" nos docs.
- Mock na mesma linha de versões do produto real (Avalonia 11.3, CommunityToolkit.Mvvm 8.4,
  xunit.v3 3.2), no patch mais recente de cada uma. Licença da v0 em `licenca.json` ao lado
  do executável; sem trial na v0, então F3.4 fica inativa.
- Achado arquitetural: a única mitigação que derrota o MATE no roubo do algoritmo é mover o DSP
  para um servidor (o cliente deixa de conter o segredo). Local, ofuscação e .NET Native AOT são
  endurecimento parcial.

## Papéis

- Blue team: Rodrigo (constrói builds e controles).
- Red team: 2 colegas (atacam, mantêm árvores e casos de abuso).

## Estrutura

- `docs/00-ideacao.md` .. `05-tds-process.md`: ideação, threat model (STRIDE+MATE), casos de
  abuso, árvores de ataque, rastreabilidade, processo.
- `docs/06-plano-iteracoes.md`: sequência de builds (v0 a v4), calendário até a apresentação
  de 24/11, ritual do ciclo, build pronta, medição de custo, regras de engajamento.
- `docs/07-guia-red-team.md`: passo a passo do red team e bloco de contexto para a IA deles.
- `docs/metricas/custo-ataques.csv` e `docs/iteracoes/`: registro de custo e notas por build.
- `CONTRIBUTING.md`: fluxo red/blue, branches, como um ataque vira teste.
- `.github/workflows/security.yml`: a esteira. Os jobs que dependem de
  `app/NeuroMock/NeuroMock.csproj` ou de `security-tests/run-all.ps1` são pulados enquanto eles
  não existem.
- `.github/ISSUE_TEMPLATE/ataque.yml` e `.github/pull_request_template.md`: formatos de issue
  de ataque e de PR.
- `global.json`, `dotnet-tools.json`, `.devcontainer/`, `security-tests/`, `app/`.

## Convenções

- Escrever docs em PT-BR. Evitar travessões (em dash) nos textos.
- Testes de regressão nomeados `T-<folha>` espelhando a árvore (ex. `T-F1.2`).
- Branches: `main` (builds validadas), `hipotese/<nome>` (experimentos de arquitetura),
  `fix/<folha>` (correções do blue), `ataque/<folha>` (testes de ataque do red).
- Toda defesa que entra na `main` vem com o teste de regressão correspondente e atualiza a
  matriz em `docs/04-controls-traceability.md`.

## Próximos passos abertos

- [x] Plano das iterações, esteira em .NET 10 e matriz completa (02/11/2026).
- [x] App mock v0 (`app/NeuroMock.slnx`: licença ingênua, stub de DSP, sensor simulado) e
  harness de `security-tests/` (`run-all.ps1`, `estado-folhas.json`, controle positivo)
  (03/11/2026).
- [x] Tag `v0` publicada (03/11/2026): release com o zip e o SHA-256, nota em
  `docs/iteracoes/v0.md`.
- [x] Primeiros testes do red contra a v0: T-F3.1, T-F3.2 e T-F3.3 (05/11/2026). O blue somou
  T-F3.2 e T-F3.3 na forma da v1, porque os do red só exercitam o formato da v0.
- [x] Tag `v1` publicada (05/11/2026): release com o zip e o SHA-256, nota em
  `docs/iteracoes/v1.md`.
- [x] Testes T-F1.x do red contra a v1 (07/11/2026): T-F1.1, T-F1.2 e T-F1.3, mesclados no PR
  da v2.
- [x] Tag `v2` publicada (09/11/2026): PR #15 com os T-F1.x do red e F1.x `fechada`, release
  com o zip e o SHA-256, nota em `docs/iteracoes/v2.md`.
- [ ] Testes T-F2.x, T-H1.2 e T-H2.2 do red contra a v2, para a v3 no encontro de qui 12/11.
- [x] Configuração manual no GitHub (colaboradores, label, rulesets, secret): ver o plano.
- [ ] Atualizar o DFD (instalador sem auto-update, canal local dos jogos) e gerar o DFD e a
  árvore de ataque 2 como diagramas em `docs/assets/`.
- [ ] v4 Native AOT na branch `claude/v4-native-aot-cb46fc` (draft PR), para o encontro de ter
  17/11: csproj com `PublishAot`, guarda 2 na imagem nativa, Obfuscar fora do `build-release`,
  `global.json` no SDK 10.0.403 e docs. Falta instalar o SDK 10.0.403 na máquina do blue e chegarem
  os testes T-H1.1/T-H2.1 do red, mais a re-expressão de T-F1.x/T-F2.2/T-H1.2/T-H2.2 contra o
  nativo, que a esteira exige para ficar verde.
