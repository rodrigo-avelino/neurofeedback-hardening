# Processo de Test-Driven Security

## O ciclo

1. **Modelagem** (uma vez, no início): STRIDE sobre o DFD deriva as histórias; cada história
   vira uma árvore de ataque; cada folha vira um teste.
2. **Build 0 (baseline)**: o app sem controles. O red team roda a suíte inteira; quase todas as
   folhas ficam abertas. É o ponto de comparação.
3. **Iteração** (repete por N sprints):
   - o red team ataca a build atual seguindo as árvores;
   - cada ataque com sucesso vira uma issue com evidência (passos, prints, script);
   - o blue team implementa o controle num Pull Request que inclui o teste de regressão;
   - o teste entra com a folha declarada `aberta` (prova que a falha existe, sem travar a
     `main`); no PR do blue a folha passa a `fechada`, o teste fica vermelho e só volta ao verde
     quando o controle funciona;
   - a partir daí ele roda em todo build futuro.
4. **Fim**: quando o custo de fechar as folhas restantes passa do valor de fechá-las. No MATE
   sempre sobra folha aberta; isso é documentado como conclusão, não como fracasso.

## Hipóteses de arquitetura

Controles estruturais (hardkey, servidor de autenticação, mover o DSP para o servidor) são
avaliados como **experimentos em branch**: abre-se `hipotese/<nome>`, o blue implementa, o red
ataca a branch, e mede-se o ganho real contra a árvore. Só o que se prova sobe para a branch
principal. Quando uma hipótese adota servidor, os ramos G4 (ativação online) acordam e viram
alvos testáveis.

## A esteira (DevSecOps)

A cada push, a esteira (`.github/workflows/security.yml`) roda duas camadas:

- **Camada 1 - higiene**, em Linux: análise estática (CodeQL), dependências vulneráveis e
  segredos vazados. Ferramentas prontas, apenas configuradas.
- **Camada 2 - regressão MATE**, em runner Windows: gera o zip win-x64 da release e reaplica
  contra ele cada ataque conhecido, comparando o resultado com o estado declarado de cada folha
  em `security-tests/estado-folhas.json`. Uma folha fechada que volta a abrir derruba a esteira.
  É o que impede uma defesa de regredir.

Em tag `v*`, a esteira publica a release que o red team ataca, com o SHA-256 do zip.

A **proteção de branch** barra o merge na principal com a esteira vermelha: uma defesa que
regride não entra.

## Métricas

- Folhas abertas por build (cobertura da árvore de ataque).
- Custo do ataque por folha (tempo/esforço do red team), na filosofia do ASPIRE.
- Testes de regressão acumulados e regressões barradas.

## Ambiente de ataque e reprodutibilidade

- O ataque exploratório acontece na **máquina pessoal** do red team, fiel ao modelo MATE
  (o atacante controla o próprio ambiente).
- A **reprodutibilidade** vem de versões fixadas: o `global.json` fixa o SDK e o
  `dotnet-tools.json` fixa as ferramentas, iguais no devcontainer (`.devcontainer/`, Linux, para
  higiene e análise automatizada) e na CI.
- A regressão MATE roda num runner Windows, a mesma plataforma do alvo e do red team, contra o
  mesmo zip da release que o red ataca. Ferramentas interativas de engenharia reversa (dnSpyEx,
  x64dbg) rodam no Windows do red team; a esteira padroniza a parte automatizada.
