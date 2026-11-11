# Plano das iterações (novembro de 2026)

Como os ciclos red/blue rodam na prática: decisões de partida, sequência de builds e calendário até a apresentação, ritual de cada
ciclo, o que conta como build pronta, como um ataque vira teste sem travar a `main` e como o
custo de ataque é medido. Complementa o processo descrito em `05-tds-process.md`.

## Decisões de partida

| Decisão | Escolha | Por quê |
|---|---|---|
| Visibilidade do repo | público | CodeQL, proteção de branch e Actions saem de graça; o app é um mock justamente para poder ser exposto. O nome do produto real não entra no repo. |
| Onde roda a regressão MATE | runner Windows na CI | o alvo e o red team são Windows; Native AOT para win-x64 não compila a partir de Linux; anti-debug e binding de hardware usam a API do Windows |
| Papel do devcontainer | higiene e análise automatizada em Linux | SDK fixado pelo `global.json` e ferramentas pelo `dotnet-tools.json`, as mesmas versões da CI |
| Linguagem dos testes | C# (xunit.v3) com dnlib | mesmo stack do app real; dnlib é a biblioteca de manipulação de assembly do dnSpy |
| Modo do red team | caixa preta primeiro, caixa branca depois | o custo medido só vale para o MATE se o atacante não leu o código; a caixa branca vira uma segunda medição, registrada à parte |

## Sequência de builds

| Build | Sai em | Controle principal | Folhas que tenta fechar |
|---|---|---|---|
| v0 | ter 03/11 (saiu) | nenhum (baseline de propósito) | nenhuma; mede o ponto de partida |
| v1 | qui 05/11 (saiu) | licença assinada (ECDSA P-256, só a chave pública no cliente) + binding de hardware | G3 |
| v2 | ter 10/11 (saiu seg 09/11) | integridade do binário: manifesto de hashes assinado, checagens espalhadas, resposta atrasada | G1 |
| v3 | qui 12/11 | anti-debug, anti-hook e ofuscação (renomeação, constantes, fluxo de controle) | G2, H1.2, H2.2 |
| v4 | ter 17/11 | Native AOT com metadados enxutos | H1.1, H2.1 e os ataques em IL de G1 e G2 |
| `hipotese/dsp-servidor` | recomendação no relatório | DSP no servidor | árvore 2 inteira; acorda o ramo G4 |
| `hipotese/ativacao-online` | recomendação no relatório | servidor de ativação: licença anual, limite de ativações por cliente, troca de máquina pelo próprio cliente | nenhuma folha nova; resolve a reativação manual da v1 e acorda o ramo G4 |

A ordem segue a lógica de custo do ASPIRE: cada build fecha o caminho mais barato que sobrou
na anterior. Na v0, copiar ou editar a licença custa minutos e nenhuma habilidade. Com a
licença assinada (v1), o caminho mais barato vira patchear o `if`, o que demonstra na prática
que a raiz do problema é a integridade do binário. A v2 empurra o atacante para a execução
(G2), a v3 encarece G2 e a leitura do algoritmo, e a v4 tira o IL de cena. A ordem é hipótese,
não contrato: se o red achar um caminho mais barato, a build seguinte muda de alvo.

**Hipótese da ativação online.** A v1 amarra a licença à máquina: trocar de máquina exige pedir
licença nova ao fornecedor, e a licença antiga continua valendo até vencer. Um servidor de
ativação simples (licença anual, limite de ativações por cliente, troca de máquina feita pelo
próprio cliente) resolve esses dois pontos e pode tirar a chave privada da esteira e da máquina
do blue. Em troca, exige internet na ativação e acorda o ramo G4 (emular o servidor, replay,
forçar o modo offline). Não muda G1: o patch no binário continua valendo. Com o prazo de 24/11,
ela entra como recomendação no relatório, não como build.

## Desenho da v2 (integridade do binário)

Com a licença da v1, o caminho mais barato que sobra é o patch no binário (G1): mudar o
resultado da verificação de licença, apagar o `if` que a consulta ou trocar a chave pública
embutida pela de um atacante para assinar licenças próprias. A v2 não esconde o `if`: protege o
binário que o contém.

| Peça | Como funciona | O que encarece |
|---|---|---|
| Manifesto assinado | `manifesto.json` na raiz do pacote, com o SHA-256 de todos os arquivos publicados (assemblies, runtime, bibliotecas nativas, `deps.json`, `runtimeconfig.json` e o executável), assinado com a chave do fornecedor. A esteira assina no job `build-release`, depois do publish. Só ficam de fora a licença e a solicitação de ativação, que mudam na máquina do cliente. | Um byte alterado em qualquer arquivo invalida a assinatura; corrigir o hash no manifesto não adianta, e assinar de novo exige a chave privada. |
| Duas guardas diferentes | A guarda 1 começa na inicialização e varre o pacote inteiro com o verificador e a chave do Core: remonta o manifesto com os hashes reais e confere a assinatura sobre ele. A guarda 2 nasce com a janela do treino e confere os arquivos de onde o código em execução foi de fato carregado (app, Core, criptografia, núcleo do runtime, executável, `deps.json` e `runtimeconfig.json`), com implementação e chave próprias do App. | Um mesmo patch não derruba as duas. Assembly carregado de fora da pasta também acusa. |
| Resposta atrasada, com falha fechada | As guardas não decidem nada: cada uma deixa um selo. Duas respostas, em outros pontos (a janela do treino e o view model), em momentos sorteados (entre 4 e 14 s depois de o treino abrir) e por caminhos diferentes (uma fecha a janela, a outra encerra o processo fora da thread da interface), conferem os dois selos. Selo ausente conta como adulteração. | O treino abre e o patch parece ter funcionado; quando ele fecha, a pilha não leva à checagem. Tirar uma guarda não libera o app: é preciso achar as duas respostas. |
| Chave pública protegida | Uma segunda cópia da chave mora no App, em outra forma (o ponto da curva, mascarado). Cada guarda confere o manifesto com uma das cópias. | Trocar a chave exige achar e trocar as duas, além de assinar um manifesto próprio e emitir uma licença própria. |

Decisões que acompanham o desenho:

- **Nomes claros de propósito.** O IL continua legível (`Guardas`, `ChaveEmbutida`). Renomear é
  a v3, e juntar os dois controles na mesma build borraria a medição do custo de cada um.
- **A resposta não fere o paciente.** Ela fecha o treino e nunca mexe no sinal ou no índice: num
  app clínico, um falso positivo da integridade não pode virar feedback errado para o paciente.
- **Só em Release.** Em Debug as respostas não existem, porque o app de desenvolvimento não tem
  manifesto assinado. Não é flag: o código delas não está no IL de Debug, e o de Release não tem
  caminho para desligá-las.
- **Mesma chave das licenças.** Reusa a infraestrutura e o secret da v1. O cabeçalho do conteúdo
  assinado é diferente nos dois casos, então uma assinatura de licença não serve de assinatura de
  manifesto. No produto real, a chave que assina o pacote e a que emite licenças seriam
  separadas.
- **O harness observa o treino.** Com resposta atrasada, ver a janela do treino deixou de bastar:
  a regressão observa o treino por 30 s, e só conta como aberto o que continuou aberto. Com isso
  o controle positivo também pega falso positivo da integridade. Os T-F1.x do red, escritos
  contra a v1 com `JanelaPrincipalAbriu`, valem sem mudança.
- **Teto honesto.** Tudo continua no cliente e em IL legível: quem acha as duas respostas (ou as
  duas guardas e as duas cópias da chave) vence. O ganho é o custo de achar, não uma barreira. A
  v3 encarece achar (ofuscação, anti-debug), e a v4 tira o IL de cena.
- **Versões antigas.** Sem auto-update, a v1 continua publicada e patchável, com o mesmo formato
  de licença. Na medição, o red ataca só a build do ciclo; no produto real, isso pede
  renovabilidade (versão no formato da licença, fim do suporte às versões antigas).

## Desenho da v3 (anti-debug, anti-hook e ofuscação)

Com o binário conferido na v2 (G1 fechado), o caminho mais barato que sobra sai da edição
estática e vai para a execução (G2): um debugger que altera o retorno da validação (F2.1), um
hook ou detour do método de licença (F2.2) ou a edição da memória do processo (F2.3). Na árvore
2, o algoritmo ainda viaja com os parâmetros em claro (H1.2) e numa lógica que descompila quase
em fonte (H2.2). A v3 encarece esses dois caminhos: manipular o app em execução e ler o
algoritmo.

| Peça | Como funciona | O que encarece |
|---|---|---|
| Anti-debug em runtime (F2.1) | Em Release, uma verificação de depurador (`IsAttached`, `IsLogging` e as APIs do Windows `IsDebuggerPresent` e `CheckRemoteDebuggerPresent`) deixa um selo na inicialização, e as respostas atrasadas conferem o depurador de novo no momento sorteado, pegando também quem anexa depois de o treino abrir. | Alterar o retorno da validação com um depurador anexado passa a fechar o treino alguns segundos depois, longe do ponto do ataque. |
| Anti-hook por integridade em runtime (F2.2) | O método de decisão da licença é compilado (`PrepareMethod`) e o início do seu código nativo é lido. Um detour (os padrões de salto que Harmony e MonoMod escrevem no prólogo) deixa o selo negativo. | Trocar o método por um stub em execução (hook/detour) é detectado, e a resposta atrasada fecha o treino. |
| Estado de licença redundante (F2.3) | Não há uma flag "licenciado" única em memória: a decisão não fica guardada num `bool` de vida longa. As respostas atrasadas reverificam a licença do zero (assinatura, máquina e prazo) no momento sorteado e conferem a consistência de uma cópia redundante e codificada do estado. | Editar na memória a flag que liberou a tela não basta: a reverificação independente refaz a conta e discorda. |
| Parâmetros fora de arquivo em claro (H1.2) | O `parametros.json` embutido sai do pacote. Os números do pipeline passam a ser decodificados em runtime de um bloco de bytes mascarado (no molde da chave embutida da v2), sem o JSON em claro e sem os imediatos numéricos no IL. | Dumpar a configuração deixa de ser abrir um arquivo: os valores não aparecem como recurso nem como constantes legíveis na descompilação. |
| Renomeação do algoritmo (H2.1 parcial, H2.2) | Na esteira e no teste local, depois do publish e antes de assinar o manifesto, o Obfuscar renomeia os membros internos dos namespaces do algoritmo (`Processamento` e `Sensor`) no `NeuroMock.Core.dll`. A API pública é preservada (`KeepPublicApi`), para o `NeuroMock.dll` não ofuscado continuar ligando. | Ler a lógica fica mais caro: os nomes dos passos internos somem e, com os parâmetros empacotados, a descompilação perde os rótulos e as constantes. |

Decisões que acompanham o desenho:

- **Mesma resposta da v2.** O que a v3 detecta (depurador, hook, estado de licença
  inconsistente) alimenta as mesmas duas respostas atrasadas da v2, em pontos diferentes, em
  momentos sorteados e por caminhos diferentes (uma fecha a janela, a outra encerra o processo
  fora da thread da interface). Falha fechada: selo ausente ou negativo conta como adulteração.
  Só em Release, e nunca mexe no sinal nem no índice, porque num app clínico um falso positivo
  da verificação não pode virar feedback errado para o paciente.
- **Obfuscar, versão fixada.** A ofuscação é o Obfuscar (gratuito), com a versão fixada no
  `app/ferramentas/ofuscar-pacote.ps1`. Ela muda os bytes dos assemblies, então entra depois do
  publish e antes da assinatura do manifesto, na esteira (job `build-release`) e no teste local;
  o manifesto passa a cobrir os bytes ofuscados.
- **Renomeação só do algoritmo.** O Obfuscar renomeia apenas `Processamento` e `Sensor`, onde a
  matriz mapeia H2.1 e H2.2. `Licenciamento` e `Integridade` ficam legíveis, então os testes
  T-F1.x do red (que acham o alvo pelo nome: `VerificadorLicenca`, `LicencaValida`,
  `AssinaturaConfere`) continuam válidos e verdes sem mudança. A licença não depende de
  obscuridade: é protegida pela integridade (v2) e pelos controles de runtime da v3 (F2.x). Os
  T-F3.x não dependem de nomes.
- **API pública preservada.** O `NeuroMock.dll` (App, com o XAML compilado do Avalonia) não é
  ofuscado, e a API pública do Core é mantida, para as ligações compiladas e as chamadas do App
  continuarem resolvendo. Renomear também o App e a sua superfície pública fica para a v4, junto
  com o Native AOT.
- **Fluxo de controle fica de fora.** Nenhuma ferramenta gratuita faz ofuscação de fluxo de
  controle de forma estável no .NET 10 com Avalonia self-contained sem risco alto de quebrar o
  pacote. A v3 entrega renomeação e ocultação de constantes; o fluxo de controle fica como
  lacuna registrada (teto honesto) e some de vez na v4, quando o Native AOT tira o IL de cena.
- **Nomes do IL: a medição compara as duas builds.** Na v2 os nomes ficaram claros de propósito;
  na v3 a renomeação do algoritmo é o próprio controle. O red mede descompilando o `Core.dll` da
  v2 e o da v3 e comparando a legibilidade.
- **Teto honesto.** Tudo continua no cliente. Anti-debug, anti-hook e estado redundante elevam o
  custo de G2, não o fecham em definitivo: quem desliga as duas respostas (ou as reimplementa)
  vence, e os parâmetros decodificados existem em memória em runtime. O ganho é o custo de achar
  e de manter o ataque, não uma barreira. F2.1 e F2.3 podem ser medidas na mão (debugger e
  edição de memória são difíceis de automatizar na esteira).

## Desenho da v4 (Native AOT)

Com o binário conferido (v2) e a execução encarecida (v3), o que ainda viaja no cliente é o IL
gerenciado: o App e o Core descompilam quase em fonte, e os ataques das árvores 1 e 2 que
reescrevem ou leem esse IL (patch do `if`, stub do verificador, hook, dump de parâmetros, leitura
do algoritmo) continuam com alvo. A v4 publica o app em Native AOT: o `dotnet publish -r win-x64`
compila todo o código gerenciado (App, Core, Avalonia, runtime) para um único executável nativo,
sem IL nem metadados gerenciados. O que sobra no pacote é o `NeuroMock.exe` nativo e as bibliotecas
nativas do Avalonia (Skia, HarfBuzz, ANGLE); somem o `NeuroMock.dll`, o `NeuroMock.Core.dll`, o
`deps.json` e o `runtimeconfig.json`.

| Peça | Como funciona | O que encarece |
|---|---|---|
| Publicação Native AOT | `PublishAot` no csproj do App e `IsAotCompatible` no Core. O publish win-x64 gera o executável nativo. As ligações compiladas do Avalonia (`AvaloniaUseCompiledBindingsByDefault`) e o MVVM por geração de código (CommunityToolkit) já evitam reflexão, então o AOT compila e roda sem configuração extra de trimming. | Ler ou reescrever o algoritmo e a licença deixa de ser abrir um assembly no dnSpy/ILSpy: não há IL nem metadados gerenciados. O atacante cai em engenharia reversa de código nativo (Ghidra/IDA), bem mais cara (H1.1, H2.1). |
| IL de G1 e G2 sem alvo | Os ataques do red em IL (patch do salto do portão, NOP na validação, stub do verificador, hook por `DOTNET_STARTUP_HOOKS`/Harmony) dependem de um assembly gerenciado e de JIT. No executável nativo não há nenhum dos dois. | O ataque estático em IL (F1.1, F1.2, F1.3) e o hook em runtime (F2.2) perdem o alvo: a reescrita teria de ser em código nativo, sem as ferramentas de .NET. |
| Guarda 2 adaptada à imagem única | A guarda 2 da v2 conferia os arquivos de onde o código foi carregado (App, Core, cripto, runtime, `deps.json`, `runtimeconfig.json`). No AOT esses arquivos não existem: todo o código em execução está no `NeuroMock.exe`. A guarda passa a conferir o hash da imagem nativa em execução (`Environment.ProcessPath`) contra o manifesto, com a cópia da chave que mora no App. | Conferir a imagem nativa cobre de uma vez o que antes eram vários assemblies. Falha fechada mantida: sem o caminho da imagem ou com hash divergente, conta como adulteração. |
| Ofuscação subsumida | A renomeação do Obfuscar (v3) operava no `NeuroMock.Core.dll`, que não existe mais. O passo sai da esteira; o AOT remove nomes e constantes do IL de vez e cobre a lacuna de fluxo de controle que a v3 registrou (o código nativo não traz o fluxo em IL). | A legibilidade que a v3 reduzia por renomeação, a v4 remove por construção: não há símbolos gerenciados a ler. |

Decisões que acompanham o desenho:

- **A integridade e a licença seguem no cliente.** O AOT não move nada para fora: é endurecimento,
  não barreira. Anti-debug (F2.1), estado redundante (F2.3), reverificação da licença e as duas
  respostas atrasadas continuam, agora dentro do nativo. O anti-hook por detour em IL (F2.2) fica
  sem alvo (Harmony e `DOTNET_STARTUP_HOOKS` não se aplicam ao nativo): a detecção vira inerte em
  AOT, de propósito, porque o próprio AOT é o controle da folha.
- **ILCompiler sem advisory, pelo SDK.** Ligar o `PublishAot` puxa o compilador AOT. A versão que
  o SDK 10.0.401 traz (ILCompiler 10.0.12) tem o advisory alto GHSA-p3jp-9v83-76w9 (OOB no decoder
  Brotli), que o gate de SCA barraria no restore. A correção é o SDK 10.0.403, cujo ILCompiler
  10.0.14 está fora do advisory: o `global.json` passa a fixar o 10.0.403. Fixar o pacote
  `Microsoft.DotNet.ILCompiler` por `PackageReference` não serve (quebra o AOT com
  "PrivateSdkAssemblies is required"); a versão tem de vir do SDK.
- **Link nativo precisa do MSVC.** O AOT win-x64 linka com o `link.exe` do MSVC, que o ILCompiler
  localiza pelo `vswhere`. Na esteira (`windows-latest`) o `vswhere` já está no PATH. Localmente é
  preciso o SDK 10.0.403 e o `vswhere` acessível (Developer Command Prompt, ou o diretório do
  Installer do Visual Studio no PATH).
- **Teto honesto: H2.2 não fecha.** O algoritmo ainda roda no cliente e os parâmetros ainda
  materializam em memória. O AOT encarece muito reproduzir o algoritmo (sem IL, sem oráculo por
  reflexão), mas quem faz a engenharia reversa do nativo ou dirige o executável como caixa preta
  ainda reproduz a saída. H2.2 só fecha movendo o DSP para o servidor (recomendação arquitetural).
  A matriz marca a v4 como parcial em H2.2.
- **O harness do red muda de alvo.** Os testes de regressão que reescrevem ou leem IL (T-F1.1,
  T-F1.2, T-F1.3, T-F2.2, T-H1.2, T-H2.2) abrem assemblies gerenciados com dnlib/reflexão, que o
  pacote AOT não tem: contra o nativo eles perdem o alvo. Re-expressá-los (análise estática do
  nativo, ou declarar a neutralização) é trabalho do red, no encontro de ter 17/11. Os testes de
  caixa preta (controle positivo, T-F3.x) seguem valendo sem mudança. Pela regra de ouro, o
  `estado-folhas.json` só muda no fechamento do ciclo, com os testes do red.
- **Só em Release.** Como v2 e v3, as guardas e respostas só existem em Release, e o AOT é da
  publicação de Release. Em Debug o app roda em IL, para desenvolvimento.

## Calendário até a apresentação (24/11)

A apresentação foi antecipada para ter 24/11. O time se encontra presencialmente às terças e
quintas, e cada encontro fecha um ciclo: a reunião red/blue consolida a build atual e lança a
seguinte.

| Encontro | Fecha | Lança | Testes do red que precisam estar na `main` |
|---|---|---|---|
| qui 05/11 | v0 | v1 | T-F3.1, T-F3.2, T-F3.3 (só mexem no `licenca.json`, sem patch no binário) |
| ter 10/11 | v1 | v2 | T-F1.1, T-F1.2, T-F1.3 |
| qui 12/11 | v2 | v3 | T-F2.x, T-H1.2, T-H2.2 |
| ter 17/11 | v3 | v4 | T-H1.1, T-H2.1 |
| qui 19/11 | v4 | nada | consolidação final; até 24/11 é documentação e ensaio |

Regras para caber no calendário:

- **O blue anda uma build à frente.** Quando o encontro começa, a build seguinte já está pronta
  numa branch; o encontro só consolida.
- **Sem teste, a folha não fecha.** Pela regra de ouro, a folha só vira `fechada` com o teste
  dela na `main`. Se o red não conseguir escrever um teste a tempo, o blue pode escrevê-lo a
  partir dos passos da issue, depois que a tentativa em caixa preta foi registrada: a regra de o
  blue não escrever teste existe para não revelar o alvo antes do ataque.
- **F2.1 e F2.3 podem ser medidas na mão.** Debugger e edição de memória são difíceis de
  automatizar na esteira. O custo vai para o CSV e a matriz registra que a folha não tem
  regressão automática.
- **Fusível.** Se um ciclo atrasar, a v4 vira recomendação no relatório e o dia 19 não se move.
  As hipóteses de servidor ficam como recomendação argumentada.
- **Nota da iteração no próprio encontro**, para o dia 19 ser montagem, não escrita do zero.

## Ritual de um ciclo

| Etapa | Quem | O quê |
|---|---|---|
| Entre encontros | red | ataca a build atual em caixa preta, registra toda tentativa no CSV de custo, abre issue para cada ataque com sucesso e escreve o teste com a folha `aberta` |
| Entre encontros | blue | prepara a build seguinte numa branch: o controle planejado e a reação aos achados, em PRs `fix/<folha>` |
| No encontro | todos | reunião red/blue: o red mostra o que achou e quanto custou; os testes do red entram na `main`; no PR da build seguinte, as folhas que ela fecha viram `fechada`; merge, coluna da build na matriz, nota da iteração e tag `v(N+1)` |

A reunião red/blue é o momento "purple" do purple teaming: os dois times juntos olhando o mesmo
resultado. Se alguém faltar a um encontro, vale por escrito (uma issue ou uma thread); o que
importa é sair dela com o estado de cada folha combinado entre os dois lados.

## Build pronta

Uma build `vN` só é entregue ao red team quando:

- [ ] a esteira está verde na `main`;
- [ ] a tag `vN` existe e a CI publicou a release com o SHA-256;
- [ ] `security-tests/estado-folhas.json` declara o estado esperado de cada folha;
- [ ] a coluna `vN` da matriz (`04-controls-traceability.md`) está preenchida;
- [ ] existe a nota da iteração em `docs/iteracoes/vN.md` (modelo em
      `docs/iteracoes/modelo.md`).

## Como um ataque vira teste sem travar a main

No MATE sempre sobra folha aberta. Se o teste de uma folha aberta simplesmente falhasse, a
esteira nunca ficaria verde e a proteção de branch travaria a `main` para sempre. Por isso cada
folha tem um estado declarado em `security-tests/estado-folhas.json`, e o teste compara o
resultado do ataque com esse estado:

| Estado declarado | O ataque... | Resultado | Significado |
|---|---|---|---|
| `aberta` | funcionou | verde | buraco conhecido e documentado |
| `aberta` | falhou | vermelho | a folha fechou: atualizar o estado e a matriz |
| `fechada` | falhou | verde | o controle resiste |
| `fechada` | funcionou | vermelho | regressão |

O TDD fica assim:

1. O red sobe o teste com a folha `aberta`. A esteira fica verde e o buraco fica registrado.
2. O blue abre `fix/<folha>`. O primeiro commit muda a folha para `fechada` e a esteira fica
   vermelha.
3. O controle entra e a esteira fica verde.
4. Merge. A partir daí, se a folha reabrir, a esteira acusa a regressão.

**Controle positivo.** Antes de cada ataque, o teste confirma que o app original abre com uma
licença válida e recusa sem licença. Se isso falhar, o resultado é inconclusivo (erro), nunca
`fechada`: um build quebrado que nem abre não pode parecer seguro.

**Observação de fora.** O teste observa o app como o atacante observaria: roda o executável e
verifica se a janela principal abriu e, desde a v2, se continuou aberta durante a observação
(30 s). Nada de flag ou modo de teste dentro do binário, que seria superfície de ataque a mais.

## Medição do custo de ataque

O custo é a métrica principal do trabalho (`00-ideacao.md`), então ele é registrado desde a v0.

- Toda tentativa entra em `docs/metricas/custo-ataques.csv`, inclusive as que falharam. Uma
  folha fechada sem registro de tentativa não prova nada.
- Timebox de 2h por folha por build. É um teto, não uma cota: folha que cai em minutos não
  consome o resto, e o teto só pesa nas que resistem. Ele fica alto para preservar a medição:
  com teto baixo, as builds finais viram todas "mais de 1h" e a curva de custo achata onde
  deveria subir. Se estourar sem sucesso, a folha conta como fechada naquela build, com custo
  "mais de 2h".
- O modo é registrado: `preta` (só o artefato da release) ou `branca` (consultou `app/`).
- Na árvore 2, o red vence quando entrega uma reimplementação que reproduz a saída do stub num
  sinal de entrada que ele não viu (oráculo). O tempo até chegar lá é o custo de H2.

Colunas do CSV:

| Coluna | Exemplo | O que é |
|---|---|---|
| `data` | `2026-11-10` | dia da tentativa |
| `build` | `v0` | tag atacada |
| `folha` | `F1.2` | folha da árvore |
| `modo` | `preta` | `preta` ou `branca` |
| `atacante` | `nome` | quem atacou |
| `ferramentas` | `dnSpyEx` | ferramentas usadas |
| `minutos` | `35` | tempo gasto na tentativa |
| `resultado` | `sucesso` | `sucesso`, `falha` ou `timebox` |
| `evidencia` | link da issue | issue, print ou script |

## Regras de engajamento do red team

- Atacar só o zip da release da tag, conferindo o SHA-256.
- Caixa preta primeiro: só abrir `app/` depois de registrar a tentativa em caixa preta.
- O ambiente de ataque é a máquina pessoal de cada um (MATE). A infraestrutura (GitHub, CI,
  máquinas dos colegas) não é alvo.
- Binário adulterado não entra no repo: só scripts, passos e hashes.

## Estrutura combinada

```
app/
  NeuroMock.slnx
  NeuroMock/            app Avalonia (WinExe): telas e barreira de licença
  NeuroMock.Core/       lógica pura, sem Avalonia: licença, stub de DSP, sessão
  NeuroMock.Tests/      testes funcionais (xunit.v3)
security-tests/
  run-all.ps1           ponto de entrada da regressão MATE (CI e local)
  estado-folhas.json    estado esperado de cada folha na build atual
  ...                   projeto xunit.v3 + dnlib com os testes T-<folha>
docs/metricas/custo-ataques.csv
docs/iteracoes/vN.md
```

O app espelha a arquitetura do produto real: .NET 10, Avalonia 11.3, CommunityToolkit.Mvvm,
App e Core separados, publicação self-contained win-x64 (até a v3 sem single-file, sem ReadyToRun,
sem trimming e sem PDB; a v4 publica em Native AOT, um executável nativo, single-file e trimmed). O
sensor é simulado com sinal sintético. O launcher fica de fora: nenhuma folha depende dele, e a
release em zip cumpre o papel de canal de distribuição.

A esteira (`.github/workflows/security.yml`) já procura `app/NeuroMock/NeuroMock.csproj` e
`security-tests/run-all.ps1`. Enquanto eles não existirem, os jobs que dependem deles são
pulados.

## Configuração manual no GitHub

- [x] Repo público.
- [x] Convidar o red team como colaborador.
- [x] Criar o label `ataque`, usado pelo template de issue.
- [x] Proteger a `main` com um ruleset (e as tags `v*` com outro, que impede mover ou apagar): exigir PR, exigir os checks da esteira e bloquear force
      push. Os checks só aparecem para seleção depois da primeira execução da esteira.
- [x] Ligar os alertas do Dependabot e o secret scanning com push protection, gratuitos em repo
      público.
- [x] A partir da v1: secret `NEUROMOCK_CHAVE_PRIVADA` com a chave privada do fornecedor, para o
      controle positivo emitir licença na esteira. Risco registrado: colaborador com escrita
      consegue ler um secret por um PR que altere a esteira. Atacar a esteira está fora das regras
      de engajamento. Decisão (03/11/2026): secret simples, proporcional a um trabalho acadêmico;
      no produto real, a chave de emissão fica num environment com aprovação manual, ou fora da
      esteira de vez.

## Backlog fora da sequência de builds

- Atualizar o DFD de `01-threat-model.md`: o produto real é distribuído por instalador, sem
  auto-update, e tem um canal local (loopback) de integração com jogos, que deve entrar como
  superfície registrada e fora de escopo.
- DFD e árvore 2 como diagramas em `docs/assets/`.
- Fixar as actions por SHA e ligar o Dependabot para atualizá-las.
- Revisar a coluna ASPIRE da matriz ao implementar cada controle.
