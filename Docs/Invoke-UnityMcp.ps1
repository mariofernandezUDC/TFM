param([string]$Method, [string]$ParamsJson='{}')
$taskRoot=Split-Path $PSScriptRoot -Parent
$sid=(Get-Content "$taskRoot/Unity/Logs/mcp-session-id.txt").Trim()
$body=@{jsonrpc='2.0';id=10;method=$Method;params=($ParamsJson | ConvertFrom-Json)} | ConvertTo-Json -Depth 30 -Compress
$r=Invoke-WebRequest http://127.0.0.1:8080/mcp -Method Post -ContentType application/json -Headers @{Accept='application/json, text/event-stream';'Mcp-Session-Id'=$sid} -Body ([Text.Encoding]::UTF8.GetBytes($body)) -TimeoutSec 50
foreach($line in $r.Content -split "`n") { if($line.StartsWith('data: ')) { $v=$line.Substring(6) | ConvertFrom-Json; if($v.id) { $v | ConvertTo-Json -Depth 40 -Compress } } }
