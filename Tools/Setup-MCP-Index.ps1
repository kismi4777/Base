# Однократная настройка зеркала PyPI для uv/uvx (Unity MCP for Unity).
# uvx не читает uv.toml из папки проекта — нужен пользовательский %APPDATA%\uv\uv.toml

$ErrorActionPreference = "Stop"
$uvConfigDir = Join-Path $env:APPDATA "uv"
$uvConfigPath = Join-Path $uvConfigDir "uv.toml"
$indexUrl = "https://pypi.tuna.tsinghua.edu.cn/simple"

New-Item -ItemType Directory -Force -Path $uvConfigDir | Out-Null

$content = @"
# MCP for Unity: pypi.org недоступен без VPN
index-url = "$indexUrl"
"@

Set-Content -Path $uvConfigPath -Value $content -Encoding utf8
Write-Host "Записано: $uvConfigPath"
Write-Host "index-url = $indexUrl"
Write-Host ""
Write-Host "Далее в Tools\unity-mcp\Server выполните: uv sync"
Write-Host "Затем перезапустите MCP в Unity или запустите Start-MCP-Server.ps1"
