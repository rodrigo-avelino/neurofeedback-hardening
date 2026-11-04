# Neurofeedback Hardening

Projeto da cadeira de Segurança no Ciberespaço (CyBOK). Documenta o processo de transformar
um aplicativo de neurofeedback, hoje sem nenhum controle de segurança, em um software
endurecido, de forma iterativa e com fundamentação.

A área do CyBOK que ancora o trabalho é o **Ciclo de Vida de Desenvolvimento Seguro**
(Secure Software Lifecycle). O método é **Test-Driven Security com purple teaming**: um time
azul constrói controles, um time vermelho tenta rompê-los, e cada ataque bem-sucedido vira
um teste de regressão que impede o retorno da falha.

## O método, em uma figura

```
  abuse cases  ->  modelagem de ameaças  ->  árvore de ataque  ->  controles por iteração
       |                 (STRIDE)                  (por história)              |
       |                                                                      v
       +--------------------->  cada ataque do red vira teste de regressão  <-+
                                      (roda na esteira a cada build)
```

## Papéis

- **Blue team**: constrói as builds com controles de segurança crescentes.
- **Red team**: ataca cada build seguindo as árvores de ataque.
- Cada ataque bem-sucedido vira uma issue, o blue corrige num PR, e o controle entra na
  esteira como teste de regressão permanente.

## Como navegar

| Documento | Conteúdo |
|---|---|
| [docs/00-ideacao.md](docs/00-ideacao.md) | Consolidação da ideação: problema, solução, benefício |
| [docs/01-threat-model.md](docs/01-threat-model.md) | Cenário, DFD, fronteiras de confiança, STRIDE e o modelo MATE |
| [docs/02-abuse-cases.md](docs/02-abuse-cases.md) | As histórias do atacante (casos de abuso) |
| [docs/03-attack-trees.md](docs/03-attack-trees.md) | Árvores de ataque das duas histórias |
| [docs/04-controls-traceability.md](docs/04-controls-traceability.md) | Rastreabilidade: controle -> CWE -> CyBOK -> ASPIRE -> teste |
| [docs/05-tds-process.md](docs/05-tds-process.md) | O ciclo de iterações, a suíte de testes e as métricas |
| [docs/06-plano-iteracoes.md](docs/06-plano-iteracoes.md) | Sequência de builds, calendário até a apresentação de 24/11, ritual de cada ciclo, medição de custo e regras de engajamento |
| [docs/07-guia-red-team.md](docs/07-guia-red-team.md) | Passo a passo do red team em cada ciclo e contexto para assistentes de IA |
| [CONTRIBUTING.md](CONTRIBUTING.md) | Fluxo red/blue, estratégia de branches, como criar um teste de regressão |

## Sobre o app neste repositório

O código em [`app/`](app/) é uma **aplicação figurada** (mock), com a mesma arquitetura do
produto real (Avalonia/.NET 10/MVVM, verificação de licença, leitura de SDK, armazenamento
local), porém com o núcleo de processamento de sinal substituído por um stub
sem valor comercial. Isso preserva as superfícies de ataque que importam para o trabalho sem
expor propriedade intelectual. Ver [app/README.md](app/README.md).

## Ambiente

- CI de segurança: [`.github/workflows/security.yml`](.github/workflows/security.yml)
- Ambiente reprodutível: [`.devcontainer/`](.devcontainer/)
- Testes de regressão de ataque: [`security-tests/`](security-tests/)
