# Rastreabilidade de controles

Esta é a tabela que transforma "fizemos um pentest" em "seguimos um método". Cada folha da
árvore de ataque liga-se a um teste, a uma fraqueza (CWE), a uma área do CyBOK, a uma linha de
defesa do ASPIRE e a um controle MASVS-RESILIENCE, com o resultado empírico do red team por
build.

## Como preencher

- **Folha**: id da folha na árvore (`03-attack-trees.md`).
- **Teste**: id do teste de regressão em `security-tests/`.
- **Controle (blue)**: o que o time azul implementou para fechar a folha.
- **CWE / CyBOK / ASPIRE / MASVS**: fundamentação da escolha.
- **b0..b3**: estado por build. `A` = aberto (ataque tem sucesso), `F` = fechado, `-` = n/a.

## Matriz

| Folha | Teste | Controle (blue) | CWE | CyBOK KA | ASPIRE | MASVS | b0 | b1 | b2 | b3 |
|---|---|---|---|---|---|---|---|---|---|---|
| F1.1 | T-F1.1 | assinatura + verificação de integridade | CWE-693 | Software Security; Secure SW Lifecycle | anti-tampering | RESILIENCE-3 | A | ? | ? | ? |
| F1.2 | T-F1.2 | idem (integridade do binário) | CWE-693 | Software Security | anti-tampering | RESILIENCE-3 | A | ? | ? | ? |
| F1.3 | T-F1.3 | idem | CWE-913 | Software Security | anti-tampering | RESILIENCE-3 | A | ? | ? | ? |
| F2.1 | T-F2.1 | anti-debug + checagem em runtime | CWE-489 | Software Security | anti-tampering | RESILIENCE-2 | A | ? | ? | ? |
| F2.2 | T-F2.2 | integridade em runtime | CWE-913 | Software Security | anti-tampering | RESILIENCE-3 | A | ? | ? | ? |
| F3.1 | T-F3.1 | binding de licença ao hardware | CWE-284 | AAA | renovabilidade | RESILIENCE-1 | A | ? | ? | ? |
| F3.2 | T-F3.2 | validação assimétrica (sem segredo no cliente) | CWE-321 | Cryptography; AAA | ocultação de dados | RESILIENCE-1 | A | ? | ? | ? |
| F3.3 | T-F3.3 | licença cifrada/assinada | CWE-312 | Cryptography | ocultação de dados | STORAGE-1 | A | ? | ? | ? |
| F3.4 | T-F3.4 | marcador de trial resistente a rollback | CWE-672 | AAA | renovabilidade | RESILIENCE-1 | A | ? | ? | ? |
| F4.x | T-F4.x | ativação online + atestação | CWE-294 | Distributed Systems Security | atestação remota | RESILIENCE-4 | - | ? | ? | ? |
| F5.x | T-F5.x | challenge-response com hardkey | CWE-287 | Hardware Security; AAA | atestação remota | RESILIENCE-4 | - | ? | ? | ? |
| H1.1 | T-H1.1 | ofuscação + .NET Native AOT | CWE-656 | Software Security | ocultação de algoritmo | RESILIENCE-3 | A | ? | ? | ? |
| H2.2 | T-H2.2 | ofuscação (control-flow, renaming) | CWE-656 | Software Security | ocultação de algoritmo | RESILIENCE-3 | A | ? | ? | ? |

Legenda CyBOK KA: AAA = Authentication, Authorisation & Accountability.

> As colunas de CWE/ASPIRE/MASVS são um primeiro mapeamento e devem ser revisadas ao
> implementar cada controle. O objetivo é que nenhuma escolha fique sem fundamentação.
