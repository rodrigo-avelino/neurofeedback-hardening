<#
.SYNOPSIS
    Ofusca o NeuroMock.Core.dll de um pacote publicado (v3).
.DESCRIPTION
    Roda o Obfuscar (gratuito, versao fixada neste script) sobre a pasta publicada: renomeia os
    membros internos do algoritmo (namespaces Processamento e Sensor) e preserva a API publica e as
    classes de Licenciamento, Integridade e Sessao. Entra depois do publish e antes de assinar o
    manifesto, porque a ofuscacao muda os bytes dos assemblies e o manifesto tem de cobrir o
    resultado. Mesma ordem na esteira (job build-release) e no teste local.
.EXAMPLE
    ./app/ferramentas/ofuscar-pacote.ps1 -Pasta .\publicado
#>
param(
    [Parameter(Mandatory = $true)]
    [string] $Pasta
)

$ErrorActionPreference = 'Stop'

# Versao do Obfuscar fixada aqui (nao no manifesto de ferramentas): no runner da esteira o
# `dotnet tool run` sobre um manifesto com mais de uma ferramenta recusava o comando.
$versaoObfuscar = '2.2.50'
$pastaAbs = (Resolve-Path $Pasta).Path
$coreDll = Join-Path $pastaAbs 'NeuroMock.Core.dll'
if (-not (Test-Path $coreDll)) { throw "NeuroMock.Core.dll nao encontrado em $pastaAbs" }

# Obfuscar exige pasta de saida diferente da de entrada; copiamos a DLL ofuscada de volta depois.
$saida = Join-Path $pastaAbs '_ofuscado'
if (Test-Path $saida) { Remove-Item -Recurse -Force $saida }
$ferramenta = Join-Path $pastaAbs '_obftool'
if (Test-Path $ferramenta) { Remove-Item -Recurse -Force $ferramenta }

# Barras normais no XML: funcionam no Windows e evitam escapar a barra invertida.
$inPath = $pastaAbs -replace '\\', '/'
$outPath = $saida -replace '\\', '/'
$config = Join-Path ([System.IO.Path]::GetTempPath()) ("obfuscar-" + [guid]::NewGuid().ToString('N') + ".xml")

@"
<?xml version='1.0'?>
<Obfuscator>
  <Var name="InPath" value="$inPath" />
  <Var name="OutPath" value="$outPath" />
  <Var name="KeepPublicApi" value="true" />
  <Var name="HidePrivateApi" value="true" />
  <Var name="HideStrings" value="false" />
  <!-- Renomeacao so do algoritmo (decisao da v3): o Core e o unico modulo ofuscado; dentro dele,
       Licenciamento, Integridade e Sessao ficam legiveis (os testes T-F1.x do red acham o alvo
       pelo nome), e Processamento/Sensor tem os membros internos renomeados. A API publica fica
       intacta para o NeuroMock.dll nao ofuscado continuar ligando. -->
  <Module file="$inPath/NeuroMock.Core.dll">
    <SkipNamespace name="NeuroMock.Core.Licenciamento" />
    <SkipNamespace name="NeuroMock.Core.Integridade" />
    <SkipNamespace name="NeuroMock.Core.Sessao" />
  </Module>
</Obfuscator>
"@ | Set-Content -Encoding utf8 $config

try {
    # Instala o Obfuscar numa pasta propria (versao fixada), em vez de resolver pelo manifesto de
    # ferramentas: no runner da esteira o `dotnet tool run` sobre um manifesto com mais de uma
    # ferramenta recusava "obfuscar.console". O install le o comando do proprio pacote.
    dotnet tool install Obfuscar.GlobalTool --version $versaoObfuscar --tool-path $ferramenta
    if ($LASTEXITCODE -ne 0) { throw "instalacao do Obfuscar $versaoObfuscar falhou (codigo $LASTEXITCODE)" }

    $exe = Join-Path $ferramenta 'obfuscar.console.exe'
    if (-not (Test-Path $exe)) { throw "obfuscar.console.exe nao encontrado em $ferramenta" }
    & $exe $config
    if ($LASTEXITCODE -ne 0) { throw "obfuscar falhou (codigo $LASTEXITCODE)" }

    $ofuscado = Join-Path $saida 'NeuroMock.Core.dll'
    if (-not (Test-Path $ofuscado)) { throw "Obfuscar nao gerou $ofuscado" }
    Copy-Item -Force $ofuscado $coreDll
    Write-Host "Ofuscado: NeuroMock.Core.dll (renomeacao de Processamento e Sensor)."
}
finally {
    if (Test-Path $saida) { Remove-Item -Recurse -Force $saida }
    if (Test-Path $ferramenta) { Remove-Item -Recurse -Force $ferramenta }
    if (Test-Path $config) { Remove-Item -Force $config }
}
