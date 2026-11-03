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
  Avalonia/.NET/MVVM, EEG BrainBit Flex 4 via XNeuroSDK, 100% local, instalado por launcher, sem
  nenhum controle de segurança ("white canvas").
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
- Esteira no GitHub Actions em duas camadas: higiene (SAST/SCA/segredos) e regressão MATE
  (reaplica ataques corrigidos). Branch protection barra merge com esteira vermelha.
- Ambiente de ataque é a máquina pessoal do red team (fiel ao MATE); reprodutibilidade vem do
  devcontainer, que é o mesmo ambiente da CI.
- Achado arquitetural: a única mitigação que derrota o MATE no roubo do algoritmo é mover o DSP
  para um servidor (o cliente deixa de conter o segredo). Local, ofuscação e .NET Native AOT são
  endurecimento parcial.

## Papéis

- Blue team: Rodrigo (constrói builds e controles).
- Red team: 2 colegas (atacam, mantêm árvores e casos de abuso).

## Estrutura

- `docs/00-ideacao.md` .. `05-tds-process.md`: ideação, threat model (STRIDE+MATE), casos de
  abuso, árvores de ataque, rastreabilidade, processo.
- `CONTRIBUTING.md`: fluxo red/blue, branches, como um ataque vira teste.
- `.github/workflows/security.yml`: a esteira (jobs em TODO até o app ter csproj).
- `.devcontainer/`, `security-tests/`, `app/`.

## Convenções

- Escrever docs em PT-BR. Evitar travessões (em dash) nos textos.
- Testes de regressão nomeados `T-<folha>` espelhando a árvore (ex. `T-F1.2`).
- Branches: `main` (builds validadas), `hipotese/<nome>` (experimentos de arquitetura),
  `fix/<folha>` (correções).
- Toda defesa que entra na `main` vem com o teste de regressão correspondente e atualiza a
  matriz em `docs/04-controls-traceability.md`.

## Próximos passos abertos

- [ ] Esqueleto do app mock (Avalonia + verificação de licença ingênua = build 0).
- [ ] Primeiros testes de regressão (`T-F1.2` patch no IL, `T-F3.1` reuso de licença).
- [ ] Ligar os jobs da esteira ao `app/` e ao `security-tests/`.
- [ ] Gerar o DFD e a árvore de ataque 2 como diagramas em `docs/assets/`.
