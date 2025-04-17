# Go to repo root from /scripts
Set-Location ..

Write-Host "Packing local FSH modules..."

$projects = Get-ChildItem -Recurse -Filter *.csproj `
  | Where-Object { $_.FullName -match "\\framework\\" -and $_.FullName }

foreach ($proj in $projects) {
    dotnet pack $proj.FullName -c Release -o ./nupkgs
}
