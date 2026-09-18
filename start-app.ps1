$repo = Split-Path -Parent $MyInvocation.MyCommand.Path
$apiPath = Join-Path $repo 'OisipanApi'
$mvcPath = Join-Path $repo 'OisipanMvc'

Write-Host 'Starting Oisipan API on http://localhost:5188...'
Start-Process powershell -ArgumentList '-NoLogo', '-NoProfile', '-Command', "Set-Location '$apiPath'; dotnet run --project .\BackendApi.csproj --urls http://localhost:5188" -WindowStyle Normal

Start-Sleep -Seconds 2

Write-Host 'Starting Oisipan MVC on http://localhost:5110...'
Start-Process powershell -ArgumentList '-NoLogo', '-NoProfile', '-Command', "Set-Location '$mvcPath'; dotnet run --project .\FrontendMvc.csproj --urls http://localhost:5110" -WindowStyle Normal

Write-Host 'Done.'
Write-Host 'Open: http://localhost:5110'
Write-Host 'API health: http://localhost:5188'
