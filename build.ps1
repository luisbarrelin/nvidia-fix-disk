[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compiler -PathType Leaf)) {
    throw 'The 64-bit .NET Framework C# compiler was not found.'
}

$source = Join-Path $PSScriptRoot 'ControleNvTopps.cs'
$buildDirectory = Join-Path $PSScriptRoot 'build'
$output = Join-Path $buildDirectory 'NVIDIA-Log-Control.exe'
New-Item -ItemType Directory -Path $buildDirectory -Force | Out-Null

& $compiler /nologo /target:winexe /platform:x64 /codepage:65001 /optimize+ /r:System.Windows.Forms.dll /r:System.Drawing.dll "/out:$output" $source
if ($LASTEXITCODE -ne 0) {
    throw "Compilation failed with exit code $LASTEXITCODE."
}

Get-Item -LiteralPath $output
