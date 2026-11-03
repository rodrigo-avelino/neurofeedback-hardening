# security-tests/ - suite de regressao de ataques

Cada teste corresponde a uma folha de uma arvore de ataque (`docs/03-attack-trees.md`) e a uma
linha da matriz de rastreabilidade (`docs/04-controls-traceability.md`).

## Contrato de um teste

Um teste e um procedimento repetivel com resultado passa/falha:

- **executa** a tecnica de ataque da folha (ex.: aplicar patch no IL, reusar licenca de outra
  maquina);
- **passa** quando o app resiste (recusa executar / nao vaza o segredo);
- **falha** quando o ataque tem sucesso.

No build 0 (baseline) os testes falham de proposito: provam que o buraco existe. Conforme o
blue team implementa controles, eles passam e assim permanecem (regressao).

## Convencao de nomes

`T-<folha>`, espelhando a arvore. Exemplos: `T-F1.2`, `T-F3.1`, `T-H1.1`.

## Estrutura (a criar)

```
security-tests/
  run-all.sh            # roda toda a suite (usado pela CI, camada 2)
  licenca/
    T-F1.2.sh           # patch no salto condicional -> app deve recusar abrir
    T-F3.1.sh           # reusar licenca de outra maquina -> app deve recusar
  algoritmo/
    T-H1.1.sh           # descompilar e extrair -> mede custo, verifica ofuscacao
  artifacts/            # saidas temporarias (gitignored)
```

## Metrica

Alem de passa/falha, registrar o **custo do ataque** (tempo/esforco do red team) por teste a
cada iteracao. A meta e a curva de custo subir, na filosofia do ASPIRE.
