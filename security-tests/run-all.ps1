<#
.SYNOPSIS
    Roda a suite de regressao MATE contra o zip de uma release.
.DESCRIPTION
    Os testes de security-tests/RegressaoMate extraem o zip (o mesmo que o red team recebe),
    atacam copias dele e comparam cada resultado com security-tests/estado-folhas.json. No fim,
    este script resume o resultado por folha; na CI, o resumo vai para a pagina da execucao.
.EXAMPLE
    ./security-tests/run-all.ps1 -Artefato .\neuromock-v1-win-x64.zip
.EXAMPLE
    ./security-tests/run-all.ps1 -Artefato .\neuromock-v1-win-x64.zip -ChavePrivada $HOME\.neuromock\fornecedor-p256.pem
#>
param(
    [Parameter(Mandatory = $true)]
    [string] $Artefato,

    # PEM da chave privada do fornecedor (so o blue tem; na esteira vem do secret). Sem ela,
    # fora da esteira, o controle positivo confere so a recusa sem licenca.
    [string] $ChavePrivada
)

$ErrorActionPreference = 'Stop'
if ($PSVersionTable.PSEdition -eq 'Core' -and -not $IsWindows) {
    throw 'A regressao MATE roda no Windows: o alvo e win-x64.'
}

$raiz = $PSScriptRoot
$artefatoAbs = (Resolve-Path $Artefato).Path
$trabalho = Join-Path $raiz 'artifacts'
if (Test-Path $trabalho) { Remove-Item -Recurse -Force $trabalho }
New-Item -ItemType Directory -Force $trabalho | Out-Null

$env:NFH_ARTEFATO = $artefatoAbs
$env:NFH_ESTADO = Join-Path $raiz 'estado-folhas.json'
$env:NFH_FIXTURES = Join-Path $raiz 'fixtures'
$env:NFH_TRABALHO = $trabalho
$env:NFH_RESULTADOS = Join-Path $trabalho 'resultados.jsonl'
if ($ChavePrivada) { $env:NFH_CHAVE_PRIVADA = Get-Content -Raw (Resolve-Path $ChavePrivada).Path }

$hash = (Get-FileHash $artefatoAbs -Algorithm SHA256).Hash.ToLowerInvariant()
Write-Host "Alvo: $artefatoAbs"
Write-Host "SHA-256: $hash"

dotnet run --project (Join-Path $raiz 'RegressaoMate') -c Release
$codigo = $LASTEXITCODE

# Resumo por folha: o estado declarado e o que os testes observaram.
$estado = Get-Content $env:NFH_ESTADO -Raw | ConvertFrom-Json
$resultados = @()
if (Test-Path $env:NFH_RESULTADOS) {
    $resultados = @(Get-Content $env:NFH_RESULTADOS | ForEach-Object { $_ | ConvertFrom-Json })
}

$folhas = @($estado.folhas.PSObject.Properties)
$tabela = @('| Folha | Estado declarado | Ataque | Situacao |', '|---|---|---|---|')
foreach ($folha in $folhas) {
    $r = $resultados | Where-Object { $_.folha -eq $folha.Name } | Select-Object -Last 1
    if ($r) {
        $situacao = if ($r.ok) { 'ok' } else { 'FALHA' }
        $tabela += "| $($folha.Name) | $($folha.Value) | $($r.ataque) | $situacao |"
    }
    else {
        $tabela += "| $($folha.Name) | $($folha.Value) | sem teste | |"
    }
}

$controle = $resultados | Where-Object { $_.controle } | Select-Object -Last 1
$textoControle = if (-not $controle) { 'nao rodou' }
    elseif (-not $controle.ok) { "FALHOU: $($controle.motivo)" }
    elseif ($controle.parcial) { 'parcial (sem a chave do fornecedor: so a recusa sem licenca foi conferida)' }
    else { 'ok' }
$abertas = @($folhas | Where-Object { $_.Value -eq 'aberta' }).Count
$ativas = @($folhas | Where-Object { $_.Value -ne 'inativa' }).Count

$resumo = @(
    '### Regressao MATE',
    '',
    "Alvo: ``$(Split-Path $artefatoAbs -Leaf)``, SHA-256 ``$hash``",
    '',
    "Controle positivo: $textoControle",
    '',
    "Folhas declaradas abertas: $abertas de $ativas ativas.",
    ''
) + $tabela

$texto = $resumo -join [Environment]::NewLine
Write-Host ''
Write-Host $texto
if ($env:GITHUB_STEP_SUMMARY) { $texto | Out-File -Append -Encoding utf8 $env:GITHUB_STEP_SUMMARY }

exit $codigo
