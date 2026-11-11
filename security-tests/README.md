# security-tests/ - suite de regressao de ataques

Cada teste corresponde a uma folha de uma arvore de ataque (`docs/03-attack-trees.md`) e a uma
linha da matriz de rastreabilidade (`docs/04-controls-traceability.md`).

## Contrato de um teste

Um teste e um procedimento repetivel com resultado passa/falha:

- **executa** a tecnica de ataque da folha contra o zip da release (ex.: aplicar patch no IL,
  reusar licenca de outra maquina);
- **observa de fora**, como o atacante: roda o executavel e verifica se a janela principal
  abriu (ou se o segredo vazou). Nada de flag de teste dentro do binario. Desde a v2 o alvo
  responde a adulteracao com atraso (o treino abre e fecha sozinho alguns segundos depois),
  entao o harness observa o treino por 30 s (`CopiaDoAlvo.ObservacaoTreino`) e
  `resultado.JanelaPrincipalAbriu` so e verdadeiro se ele continuou aberto ate o fim;
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

**Controle positivo.** Antes do ataque, o teste confirma que o app original recusa sem licenca
e abre o treino com uma licenca legitima para a maquina que roda a suite. Desde a v1 essa
licenca e emitida na hora: o harness le o codigo da maquina na solicitacao de ativacao que o
proprio app grava e assina com a chave privada do fornecedor (secret da esteira). Sem a chave,
fora da esteira, o controle fica parcial: so a recusa sem licenca e conferida. Desde a v2 o
treino legitimo tambem precisa continuar aberto durante a observacao: o controle pega falso
positivo da verificacao de integridade (por exemplo, um pacote publicado sem o manifesto
assinado). Se o controle falhar, o resultado e erro (inconclusivo), nunca `fechada`: um build
quebrado que nem abre nao pode parecer seguro.

## Convencao de nomes

`T-<folha>`, espelhando a arvore. Exemplos: `T-F1.2`, `T-F3.1`, `T-H1.1`.

## Estrutura

```
security-tests/
  run-all.ps1             # ponto de entrada (CI e local)
  estado-folhas.json      # estado declarado de cada folha na build atual
  fixtures/
    licenca-valida.json   # licenca legitima da "Clinica de Teste", emitida para OUTRA maquina (F3.1)
  RegressaoMate/          # projeto xunit.v3 + dnlib
    Alvo.cs               # extrai o zip, da uma copia por teste, roda e observa o app
    EstadoFolhas.cs       # compara o resultado do ataque com o estado declarado
    ControlePositivo.cs   # o alvo original abre com licenca e recusa sem
    Assembleia.cs         # abre, altera e grava um assembly com dnlib
    Licenca/              # T-F1.x, T-F2.x, T-F3.x (o red team cria)
    Algoritmo/            # T-H1.x, T-H2.x (o red team cria)
  artifacts/              # saidas temporarias (gitignored)
```

## Como rodar

So no Windows (o alvo e win-x64). Pegue o zip da release, ou gere um com `dotnet publish`
(ver `app/README.md`; desde a v2, assine a pasta publicada com `app/ferramentas/assinar-pacote.cs`
antes de zipar, senao o treino fecha sozinho e o controle positivo acusa; desde a v3, ofusque a
pasta com `app/ferramentas/ofuscar-pacote.ps1` ANTES de assinar, porque a ofuscacao muda os bytes
que o manifesto cobre), e rode:

```powershell
./security-tests/run-all.ps1 -Artefato .\neuromock-v1-win-x64.zip
```

Assim, sem a chave do fornecedor, o controle positivo sai parcial, e e o normal para o red
team. O blue, que tem a chave, roda o controle completo:

```powershell
./security-tests/run-all.ps1 -Artefato .\neuromock-v1-win-x64.zip -ChavePrivada $HOME\.neuromock\fornecedor-p256.pem
```

O controle positivo abre o app de verdade: as janelas piscam na tela e o harness as fecha. A esteira roda o mesmo script num runner Windows, contra o zip da propria build, e
escreve o resumo por folha na pagina da execucao.

## Como escrever um teste

Um arquivo por folha, com o nome espelhando a arvore. O esqueleto:

```csharp
public sealed class T_F1_2
{
    [Fact(DisplayName = "T-F1.2 Patch de bytes: NOP no salto condicional")]
    public void Ataque()
    {
        ControlePositivo.Garantir();

        using var copia = Alvo.NovaCopia();     // copia descartavel do zip da release
        copia.RemoverLicenca();                 // o pirata nao tem licenca
        Assembleia.Modificar(copia.Caminho("<assembly alvo>.dll"), modulo =>
        {
            // a alteracao que o red team achou no dnSpy, agora com dnlib
        });

        var resultado = copia.Executar();       // roda e observa de fora
        EstadoFolhas.Verificar("F1.2", ataqueFuncionou: resultado.JanelaPrincipalAbriu);
    }
}
```

Em ataque que nao mexe no binario (ramo G3), troque a alteracao por `copia.InstalarLicenca(json)`
ou `copia.InstalarLicencaDeTeste()` (a licenca legitima de outra maquina). O codigo desta
maquina sai da solicitacao de ativacao, depois de uma execucao sem licenca:
`copia.LerCodigoDaMaquina()`, ou tudo de uma vez com `copia.ObterCodigoDaMaquina()`, que roda
sem licenca e devolve o codigo. Os testes `T-F3.2-v1.cs` e `T-F3.3-v1.cs` sao exemplos. O que o atacante sabe do alvo (nomes, offsets) vem da
tentativa em caixa preta e fica no teste, nao no harness.

## Metrica

Alem de passa/falha, o **custo do ataque** (tempo/esforco do red team) de cada tentativa vai
para `docs/metricas/custo-ataques.csv`, inclusive as que falharam. A meta e a curva de custo
subir, na filosofia do ASPIRE.
