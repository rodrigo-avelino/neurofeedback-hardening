# Guia do red team

O passo a passo do red team em cada ciclo, do zip da release até o teste de regressão. As
regras completas estão em `06-plano-iteracoes.md`; o fluxo red/blue, em `CONTRIBUTING.md`.

## Antes do primeiro ciclo (uma vez)

1. Aceitar o convite de colaborador no GitHub e clonar o repo.
2. Instalar o SDK do .NET 10. O `global.json` da raiz fixa a versão.
3. Instalar as ferramentas de engenharia reversa no Windows:
   - [dnSpyEx](https://github.com/dnSpyEx/dnSpy/releases): descompila, depura e edita o IL;
   - [ILSpy](https://github.com/icsharpcode/ILSpy/releases): descompilação;
   - [x64dbg](https://x64dbg.com): depuração nativa, que vai importar quando chegar o Native AOT
     (v4);
   - para os ramos G2 e G3, o que preferirem (editor de memória, Harmony, scripts próprios).
4. Ler `03-attack-trees.md`: cada folha é um ataque, e o estado dela por build é o que se mede.

## Em cada ciclo

### 1. Pegar o alvo

Baixar o zip da build na [página de Releases](https://github.com/rodrigo-avelino/neurofeedback-hardening/releases)
e conferir o SHA-256 contra o `.sha256` da release e a nota `docs/iteracoes/vN.md`:

```powershell
Get-FileHash .\neuromock-v0-win-x64.zip -Algorithm SHA256
```

Hash diferente: não ataque, avise o blue. O zip da release é o único alvo válido.

### 2. Atacar em caixa preta

- Só o zip e as ferramentas. Nada de abrir `app/` antes de registrar a tentativa em caixa preta.
- Uma folha por vez, com timebox de 2h por folha por build.
- Para F3.1 (licença copiada de outra máquina), a licença legítima "de outra clínica" é
  `security-tests/fixtures/licenca-valida.json`.
- O app procura a licença no arquivo `licenca.json`, na pasta do executável.

### 3. Registrar toda tentativa

Uma linha em `docs/metricas/custo-ataques.csv` por tentativa, inclusive as que falharam. Uma
folha fechada sem tentativa registrada não prova nada.

```
data,build,folha,modo,atacante,ferramentas,minutos,resultado,evidencia
2026-11-04,v0,F1.2,preta,nome,dnSpyEx,35,sucesso,link da issue
```

`modo` é `preta` ou `branca`; `resultado` é `sucesso`, `falha` ou `timebox`.

### 4. Ataque que funcionou vira issue

Abrir uma issue pelo template **Ataque**: build e SHA-256, folha, modo, passos, evidência,
custo e impacto. Binário adulterado não vai anexado: passos, script e hash bastam.

### 5. A issue vira teste de regressão

1. Branch `ataque/<folha>` a partir da `main`.
2. Teste `T-<folha>` em `security-tests/RegressaoMate/`, seguindo o esqueleto de
   `security-tests/README.md`: copia o zip, refaz o ataque, roda o app e chama
   `EstadoFolhas.Verificar("<folha>", ataqueFuncionou: ...)`.
3. A folha continua `aberta` em `security-tests/estado-folhas.json`: o teste documenta o buraco
   e a esteira fica verde. Quem muda a folha para `fechada` é o blue, no PR da correção.
4. Rodar local antes do PR:

   ```powershell
   ./security-tests/run-all.ps1 -Artefato .\neuromock-v0-win-x64.zip
   ```

   A partir da v1, o resumo local mostra o controle positivo como "parcial": ele precisa da
   chave privada do fornecedor, que só o blue e a esteira têm. É o esperado; na esteira ele roda
   completo.

5. Abrir o PR para a `main`. A esteira roda o teste contra o zip da própria build, e o merge só
   entra com ela verde.

### 6. Reunião red/blue

No meio do ciclo, 30 minutos, por chamada ou por escrito: o red mostra o que funcionou, o que
não funcionou e quanto custou; os dois times combinam o estado de cada folha; o blue fecha o
escopo da build seguinte. É o momento "purple" do purple teaming. Não há data fixa por build: o
ciclo fecha quando vocês passaram por todas as folhas ativas e o blue entregou a próxima.

### 7. Caixa branca (opcional)

Depois de registrar a caixa preta, pode ler `app/` e tentar de novo. A linha do CSV vai com
`modo` = `branca`: é uma segunda medição, separada da primeira.

## Árvore 2 (algoritmo)

O alvo é o processamento de sinal dentro do binário (um stub com algoritmo de verdade):
extrair os parâmetros (H1.2), localizar o módulo (H2.1) e entender a lógica (H2.2). A meta é
reimplementar o processamento a ponto de reproduzir a saída. A verificação por oráculo, com
uma entrada que vocês não viram, fica combinada na primeira reunião red/blue. Registrem o tempo de cada
etapa.

## O que não fazer

- Atacar a infraestrutura: GitHub, esteira, máquinas dos colegas.
- Commitar binário adulterado, licença emitida por vocês ou qualquer segredo.
- Escrever teste que dependa de flag ou modo de teste dentro do binário: o teste observa o app
  de fora, como um atacante.

## Contexto para a IA de vocês

Colem o bloco abaixo no começo da conversa com o assistente (Claude, ChatGPT, Copilot). No
Claude Code dentro do repo, ele já lê o `CLAUDE.md` da raiz, que é escrito do ponto de vista do
blue; o bloco deixa claro que vocês são o red.

```text
Você está ajudando o RED TEAM de um trabalho acadêmico de segurança de software (cadeira de
Segurança no Ciberespaço, método Test-Driven Security com purple teaming). O ataque é
autorizado: o alvo é um aplicativo de neurofeedback FIGURADO (mock), construído pelo próprio
blue team para o exercício, sem valor comercial.
Repo: https://github.com/rodrigo-avelino/neurofeedback-hardening

Modelo de atacante: Man-At-The-End (MATE). O atacante controla a própria máquina Windows e
tem o binário (.NET 10, Avalonia, publicado self-contained win-x64). Duas histórias:
1. Pirataria: usar o app sem licença válida (árvore 1, folhas F1.x a F5.x).
2. Roubo do algoritmo: recuperar o processamento de sinal a partir do binário (árvore 2,
   folhas H1.x e H2.x).
As árvores estão em docs/03-attack-trees.md; o guia do red team, em docs/07-guia-red-team.md.

Regras que você deve seguir:
- Caixa preta primeiro: trabalhe só com o zip da release e ferramentas de engenharia reversa
  (dnSpyEx, ILSpy, x64dbg, dnlib). NÃO abra nem leia nada em app/ (o código-fonte do alvo) até
  eu dizer que a tentativa em caixa preta foi registrada.
- Timebox de 2h por folha por build. Ajude a registrar cada tentativa, inclusive as que
  falharam, em docs/metricas/custo-ataques.csv.
- Ataque que funcionou vira issue pelo template "Ataque" e depois um teste T-<folha> em C#
  (xunit.v3 + dnlib) em security-tests/RegressaoMate/, numa branch ataque/<folha>, seguindo o
  esqueleto de security-tests/README.md. O teste ataca uma cópia do zip, observa o app de fora
  e chama EstadoFolhas.Verificar("<folha>", ataqueFuncionou: ...). Não mude o estado da folha
  para "fechada": isso é do blue.
- Não ataque a infraestrutura (GitHub, CI, máquinas de terceiros) e não commite binário
  adulterado, licença emitida por nós nem segredo.
- Escreva em PT-BR e evite travessões.
```

Quem usa o Claude Code pode reforçar a caixa preta negando a leitura de `app/` no
`.claude/settings.local.json` (arquivo pessoal, fora do git). Não substitui a disciplina, porque
o agente ainda alcança o código por outros caminhos, mas evita a leitura acidental:

```json
{
  "permissions": {
    "deny": ["Read(./app/**)"]
  }
}
```
