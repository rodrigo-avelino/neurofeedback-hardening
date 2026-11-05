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
- [ ] Tag `v1` depois do merge do PR #6, com release e nota em `docs/iteracoes/v1.md`.
- [ ] Testes T-F1.x do red (patch no binário) para a v2, no encontro de ter 10/11.
- [x] Configuração manual no GitHub (colaboradores, label, rulesets, secret): ver o plano.
- [ ] Atualizar o DFD (instalador sem auto-update, canal local dos jogos) e gerar o DFD e a
  árvore de ataque 2 como diagramas em `docs/assets/`.
