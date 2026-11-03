# security-tests/ - suite de regressao de ataques

Cada teste corresponde a uma folha de uma arvore de ataque (`docs/03-attack-trees.md`) e a uma
linha da matriz de rastreabilidade (`docs/04-controls-traceability.md`).

## Contrato de um teste

Um teste e um procedimento repetivel com resultado passa/falha:

- **executa** a tecnica de ataque da folha contra o zip da release (ex.: aplicar patch no IL,
  reusar licenca de outra maquina);
- **observa de fora**, como o atacante: roda o executavel e verifica se a janela principal
  abriu (ou se o segredo vazou). Nada de flag de teste dentro do binario;
- **compara** o resultado com o estado declarado da folha em `estado-folhas.json`.

| Estado declarado | O ataque... | Resultado |
|---|---|---|
| `aberta` | funcionou | passa (buraco documentado) |
| `aberta` | falhou | falha: a folha fechou, atualizar o estado e a matriz |
| `fechada` | falhou | passa (o controle resiste) |
| `fechada` | funcionou | falha: regressao |

Assim a esteira fica verde com folhas abertas (no MATE sempre sobra alguma) e so fica vermelha
em regressao ou em estado desatualizado. O TDD acontece no PR `fix/<folha>`: mudar a folha para
`fechada` deixa a esteira vermelha ate o controle funcionar (ver `docs/06-plano-iteracoes.md`).

**Controle positivo.** Antes do ataque, o teste confirma que o app original abre com licenca
valida e recusa sem licenca. Se isso falhar, o resultado e erro (inconclusivo), nunca
`fechada`: um build quebrado que nem abre nao pode parecer seguro.

## Convencao de nomes

`T-<folha>`, espelhando a arvore. Exemplos: `T-F1.2`, `T-F3.1`, `T-H1.1`.

## Estrutura (a criar junto com a v0)

```
security-tests/
  run-all.ps1           # ponto de entrada (CI e local): run-all.ps1 -Artefato <zip da release>
  estado-folhas.json    # estado esperado de cada folha na build atual
  <projeto xunit.v3 + dnlib>/
    Licenca/            # T-F1.x, T-F2.x, T-F3.x
    Algoritmo/          # T-H1.x, T-H2.x
  artifacts/            # saidas temporarias (gitignored)
```

A esteira roda `run-all.ps1` num runner Windows, contra o mesmo zip que o red team recebe, e
pula a camada 2 enquanto o script nao existir.

## Metrica

Alem de passa/falha, o **custo do ataque** (tempo/esforco do red team) de cada tentativa vai
para `docs/metricas/custo-ataques.csv`, inclusive as que falharam. A meta e a curva de custo
subir, na filosofia do ASPIRE.
