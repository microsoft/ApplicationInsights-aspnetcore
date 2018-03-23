Get-Process -name dotnet
Write-Host "Stopping"
Get-Process -name dotnet | Stop-Process
Write-Host "Stopped"
Get-Process -name dotnet