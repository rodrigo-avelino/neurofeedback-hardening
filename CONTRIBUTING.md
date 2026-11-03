# Fluxo de trabalho (red/blue, purple teaming)

A sequência de builds, o ritual de cada ciclo, a medição de custo e as regras de engajamento do red
team estão em [docs/06-plano-iteracoes.md](docs/06-plano-iteracoes.md).

## Branches

- `main`: builds já validadas pelo red team (v0, v1, v2...). Protegida: só entra com a esteira
  verde.
- `hipotese/<nome>`: experimento de arquitetura (hardkey, servidor de auth, DSP no servidor).
  O red ataca a branch; só sobe para `main` o que se provar.
- `fix/<folha>`: correção de um ataque específico pelo blue team.
- `ataque/<folha>`: teste de regressão de um ataque, aberto pelo red team (passo a passo em
  [docs/07-guia-red-team.md](docs/07-guia-red-team.md)).

## Ciclo de um ataque

1. **Red** acha um ataque seguindo a árvore (`docs/03-attack-trees.md`) e abre uma **issue**
   pelo template "Ataque": build e SHA-256, folha (ex. `F1.2`), modo (caixa preta ou branca),
   passos, evidência, custo e impacto. Toda tentativa, inclusive as que falharam, vai também
   para `docs/metricas/custo-ataques.csv`.
2. O ataque vira um **teste de regressão** em `security-tests/`, e a folha entra em
   `security-tests/estado-folhas.json` como `aberta`. O teste confirma que o ataque funciona e a
   esteira fica verde: o buraco fica documentado sem travar a `main`.
3. **Blue** abre `fix/<folha>`. O primeiro commit muda a folha para `fechada` e a esteira fica
   vermelha: o teste nasce vermelho, como no TDD. O controle entra até a esteira ficar verde, e
   o PR só mergeia assim.
4. Depois do merge, o teste roda em todo build futuro: se a folha reabrir, a esteira acusa a
   regressão. Atualizar o estado da folha na matriz de `docs/04-controls-traceability.md`.

## Como adicionar um teste de regressão

- Um teste é um procedimento repetível com resultado passa/falha, escrito em C# (xunit.v3) no
  projeto de `security-tests/`. Patch de binário usa dnlib.
- Exemplo (pirataria): aplicar o patch conhecido no binário da release e verificar se o app
  abre sem licença. O resultado é comparado com o estado declarado da folha (ver
  `security-tests/README.md`).
- Todo teste começa pelo controle positivo: o app original abre com licença válida e recusa sem
  licença. Se isso falhar, o resultado é inconclusivo, nunca `fechada`.
- Nome do teste espelha a folha: `T-F1.2`.

## Papéis

- **Blue team**: Rodrigo. Constrói builds e controles.
- **Red team**: 2 colegas. Atacam e também mantêm a árvore de ataque e os casos de abuso.

## Regra de ouro

Nenhuma defesa entra na `main` sem o teste de regressão correspondente. Segurança é regra da
esteira, não disciplina individual.
