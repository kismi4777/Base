# Запуск MCP-сервера Unity без сломанного системного SOCKS-прокси.
# Использование: правый клик → "Выполнить с PowerShell" или из терминала в папке Tools.

$ErrorActionPreference = "Stop"
$uvBin = Join-Path $env:USERPROFILE ".local\bin"
if (Test-Path $uvBin) { $env:Path = "$uvBin;$env:Path" }

# Обход socks=127.0.0.1:10808 из настроек Windows (когда VPN не запущен)
$env:NO_PROXY = "*"
Remove-Item Env:HTTP_PROXY, Env:HTTPS_PROXY, Env:ALL_PROXY, Env:http_proxy, Env:https_proxy, Env:all_proxy -ErrorAction SilentlyContinue

# pypi.org часто недоступен; зеркало (дублирует Tools/unity-mcp/Server/uv.toml)
$env:UV_INDEX_URL = "https://pypi.tuna.tsinghua.edu.cn/simple"

$serverPath = Join-Path $PSScriptRoot "unity-mcp\Server"
if (-not (Test-Path (Join-Path $serverPath "pyproject.toml"))) {
    Write-Host "Сервер не найден. Выполните:"
    Write-Host "  git clone --depth 1 --branch beta https://github.com/CoplayDev/unity-mcp.git `"$($PSScriptRoot)\unity-mcp`""
    exit 1
}

Write-Host "Запуск MCP HTTP на http://127.0.0.1:8080 ..."
$venvMcp = Join-Path $serverPath ".venv\Scripts\mcp-for-unity.exe"
if (Test-Path $venvMcp) {
    & $venvMcp --transport http --http-host 127.0.0.1 --http-port 8080
} else {
    Write-Host "venv не найден. Один раз выполните в Server: uv sync"
    uvx --from $serverPath mcp-for-unity --transport http --http-host 127.0.0.1 --http-port 8080
}
