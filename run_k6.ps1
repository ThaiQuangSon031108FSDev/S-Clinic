$proc = Start-Process dotnet -ArgumentList "run --project SClinic\SClinic.csproj --launch-profile https" -PassThru -WindowStyle Hidden
Start-Sleep -Seconds 10
.\k6_bin\k6-v0.54.0-windows-amd64\k6.exe run SClinic.Tests\load_test.js > k6_result.txt
Get-Content k6_result.txt
Stop-Process -Id $proc.Id -Force
