$ErrorActionPreference = "Stop"

if (-Not (Test-Path build)) {
    New-Item -ItemType Directory -Path build | Out-Null
}
Set-Location build

cmake ..

cmake --build .

Start-Process -NoNewWindow -Wait ".\Debug\cLox.exe"

Set-Location ..
Remove-Item -Recurse -Force build