# Fluxo de trabalho (red/blue, purple teaming)

## Branches

- `main`: builds já validadas pelo red team (v0, v1, v2...). Protegida: só entra com a esteira
  verde.
- `hipotese/<nome>`: experimento de arquitetura (hardkey, servidor de auth, DSP no servidor).
  O red ataca a branch; só sobe para `main` o que se provar.
- `fix/<folha>`: correção de um ataque específico pelo blue team.

## Ciclo de um ataque

1. **Red** acha um ataque seguindo a árvore (`docs/03-attack-trees.md`). Abre uma **issue** com:
   - a folha da árvore (ex. `F1.2`);
   - passos para reproduzir e evidência (print, binário patchado, script);
   - o impacto.
2. O ataque vira um **teste de regressão** em `security-tests/` que, na build atual, **falha de
   propósito** (prova que o buraco existe).
3. **Blue** implementa o controle num PR `fix/<folha>`, que inclui o teste. O PR só mergeia com
   a esteira verde.
4. Depois do merge, o teste roda em todo build futuro. Atualizar o estado da folha na matriz de
   `docs/04-controls-traceability.md`.

## Como adicionar um teste de regressão

- Um teste é um procedimento repetível com resultado passa/falha.
- Exemplo (pirataria): aplicar o patch conhecido no binário e verificar que o app **recusa**
  abrir. Se abrir, o teste falha (a defesa regrediu).
- Nome do teste espelha a folha: `T-F1.2`.

## Papéis

- **Blue team**: Rodrigo. Constrói builds e controles.
- **Red team**: 2 colegas. Atacam e também mantêm a árvore de ataque e os casos de abuso.
- Fixar o número de iterações por sprint para o build 0 não engolir o semestre.

## Regra de ouro

Nenhuma defesa entra na `main` sem o teste de regressão correspondente. Segurança é regra da
esteira, não disciplina individual.
