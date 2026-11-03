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

## Estrutura

```
security-tests/
  run-all.ps1             # ponto de entrada (CI e local)
  estado-folhas.json      # estado declarado de cada folha na build atual
  fixtures/
    licenca-valida.json   # licenca legitima da "Clinica de Teste" (controle positivo, F3.1)
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
(ver `app/README.md`), e rode:

```powershell
./security-tests/run-all.ps1 -Artefato .\neuromock-v0-win-x64.zip
```

O controle positivo abre o app de verdade duas vezes: as janelas piscam na tela e o harness as
fecha. A esteira roda o mesmo script num runner Windows, contra o zip da propria build, e
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
ou `copia.InstalarLicencaDeTeste()`. O que o atacante sabe do alvo (nomes, offsets) vem da
tentativa em caixa preta e fica no teste, nao no harness.

## Metrica

Alem de passa/falha, o **custo do ataque** (tempo/esforco do red team) de cada tentativa vai
para `docs/metricas/custo-ataques.csv`, inclusive as que falharam. A meta e a curva de custo
subir, na filosofia do ASPIRE.
