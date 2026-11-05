# Rastreabilidade de controles

Esta é a tabela que transforma "fizemos um pentest" em "seguimos um método". Cada folha da
árvore de ataque liga-se a um teste, a uma fraqueza (CWE), a uma área do CyBOK, a uma linha de
defesa do ASPIRE e a um controle MASVS-RESILIENCE, com o resultado empírico do red team por
build.

## Como preencher

- **Folha**: id da folha na árvore (`03-attack-trees.md`).
- **Teste**: id do teste de regressão em `security-tests/`.
- **Controle (blue)**: o que o time azul implementou (ou planeja) para fechar a folha.
- **Build alvo**: em que build o controle está planejado (`06-plano-iteracoes.md`).
- **CWE / CyBOK / ASPIRE / MASVS**: fundamentação da escolha.
- **v0..v4**: estado por build, igual ao declarado em `security-tests/estado-folhas.json` na
  tag. `A` = aberta, medida (o ataque teve sucesso), `A*` = aberta por expectativa (nenhum
  controle mira a folha e o red ainda não a mediu), `F` = fechada (o controle resiste ao ataque
  conhecido, provado por teste de regressão), `-` = n/a (ramo inativo), `?` = build ainda não
  saiu.

A coluna da build é preenchida no fechamento do ciclo. O custo de cada tentativa fica em
`docs/metricas/custo-ataques.csv`. Na v0, o calendário levou o red direto às folhas que a v1
fecha: só F3.1, F3.2 e F3.3 foram medidas; as outras ficaram `A*`.

## Matriz

| Folha | Teste | Controle (blue) | Build alvo | CWE | CyBOK KA | ASPIRE | MASVS | v0 | v1 | v2 | v3 | v4 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| F1.1 | T-F1.1 | integridade do binário: manifesto de hashes assinado, checado em vários pontos | v2 | CWE-602 | Software Security; Secure SW Lifecycle | anti-tampering | RESILIENCE-2 | A* | A* | ? | ? | ? |
| F1.2 | T-F1.2 | idem (integridade do binário) | v2 | CWE-602 | Software Security | anti-tampering | RESILIENCE-2 | A* | A* | ? | ? | ? |
| F1.3 | T-F1.3 | idem | v2 | CWE-602 | Software Security | anti-tampering | RESILIENCE-2 | A* | A* | ? | ? | ? |
| F2.1 | T-F2.1 | anti-debug + checagem em runtime | v3 | CWE-602 | Software Security | anti-tampering | RESILIENCE-4 | A* | A* | ? | ? | ? |
| F2.2 | T-F2.2 | detecção de hook + integridade em runtime | v3 | CWE-602 | Software Security | anti-tampering | RESILIENCE-4 | A* | A* | ? | ? | ? |
| F2.3 | T-F2.3 | estado de licença sem flag única em memória; verificação redundante | v3 | CWE-642 | Software Security | anti-tampering | RESILIENCE-2 | A* | A* | ? | ? | ? |
| F3.1 | T-F3.1 | binding de licença ao hardware | v1 | CWE-1390 | AAA | renovabilidade | n/a | A | F | ? | ? | ? |
| F3.2 | T-F3.2 | validação assimétrica (só a chave pública no cliente) | v1 | CWE-321 | Cryptography; AAA | ocultação de dados | CRYPTO-2 | A | F | ? | ? | ? |
| F3.3 | T-F3.3 | licença assinada (ECDSA); cifrar não basta, a chave estaria no cliente | v1 | CWE-345 | Cryptography | ocultação de dados | CRYPTO-1 | A | F | ? | ? | ? |
| F3.4 | T-F3.4 | marcador de trial resistente a rollback | se houver trial | CWE-642 | AAA | renovabilidade | n/a | - | - | - | - | - |
| F4.x | T-F4.x | ativação online + atestação | hipótese | CWE-294 | Distributed Systems Security | atestação remota | n/a | - | - | - | - | - |
| F5.x | T-F5.x | challenge-response com hardkey | hipótese | CWE-287 | Hardware Security; AAA | atestação remota | n/a | - | - | - | - | - |
| H1.1 | T-H1.1 | ofuscação + .NET Native AOT | v4 | CWE-540 | Software Security | ocultação de algoritmo | RESILIENCE-3 | A* | A* | ? | ? | ? |
| H1.2 | T-H1.2 | parâmetros fora de arquivo em claro; ofuscação de constantes | v3 | CWE-312 | Software Security | ocultação de dados | RESILIENCE-3 | A* | A* | ? | ? | ? |
| H2.1 | T-H2.1 | renomeação (v3) + Native AOT sem metadados (v4) | v3, v4 | CWE-540 | Software Security | ocultação de algoritmo | RESILIENCE-3 | A* | A* | ? | ? | ? |
| H2.2 | T-H2.2 | ofuscação (control-flow, renaming) | v3 | CWE-540 | Software Security | ocultação de algoritmo | RESILIENCE-3 | A* | A* | ? | ? | ? |

Legendas:

- CyBOK KA: AAA = Authentication, Authorisation & Accountability.
- MASVS é um padrão para mobile; a categoria RESILIENCE entra por analogia: RESILIENCE-1
  integridade da plataforma, RESILIENCE-2 anti-tampering, RESILIENCE-3 anti análise estática,
  RESILIENCE-4 anti análise dinâmica. `n/a` = o MASVS não tem controle correspondente
  (licenciamento não é coberto por ele).
- G4 e G5 só acordam se uma hipótese de arquitetura for adotada. O resultado da branch
  `hipotese/...` vai na nota da iteração.
- F3.4 está inativa porque o mock não tem trial na v0. A folha acorda se o trial for adotado.

## Notas de mapeamento

- **CWE-602 em G1 e G2.** A fraqueza raiz é uma só: a política de licença é imposta apenas no
  cliente, que o atacante controla. Muda a técnica (estática ou em execução), não a fraqueza. É
  também o que fundamenta a recomendação de mover o núcleo para um servidor.
- **CWE-642 em F2.3 e F3.4.** Estado crítico (a flag de licenciado, o marcador de trial, o
  relógio) guardado onde o atacante escreve.
- **CWE-1390 em F3.1.** Uma licença que não está presa à máquina não prova que quem a usa é o
  licenciado.
- **CWE-345 em F3.3.** O problema do arquivo de licença editável é autenticidade, não sigilo. Por
  isso o controle é assinatura; cifrar com chave embutida no cliente não resolve.
- **CWE-540 na árvore 2.** O algoritmo viaja dentro de um código que descompila quase em fonte.
  A ofuscação, que é o controle, é ela mesma CWE-656 (segurança por obscuridade): é o teto
  honesto do MATE, registrado em `03-attack-trees.md`.
- **Revisão de 02/11/2026** em relação ao primeiro mapeamento: os IDs MASVS foram corrigidos
  (anti-tampering é RESILIENCE-2 e anti análise dinâmica é RESILIENCE-4); a CWE-489 (código de
  debug esquecido em produção) saiu de F2.1 porque não descreve a folha; CWE-693 e CWE-284, que
  são pilares genéricos, deram lugar a CWEs mais específicas; entraram as folhas F2.3, H1.2 e
  H2.1, que estavam na árvore e faltavam aqui; as colunas foram até a v4.

> As colunas de CWE/ASPIRE/MASVS continuam sujeitas a revisão ao implementar cada controle. O
> objetivo é que nenhuma escolha fique sem fundamentação.
