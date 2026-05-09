$ports = @(5000, 7000, 5100, 7100, 5200, 7200, 5433, 4317, 5010)
$connections = Get-NetTCPConnection -LocalPort $ports -ErrorAction SilentlyContinue
foreach ($conn in $connections) {
    Write-Host "Killing PID $($conn.OwningProcess) on port $($conn.LocalPort)"
    Stop-Process -Id $conn.OwningProcess -Force
}
