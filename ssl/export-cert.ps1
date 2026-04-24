# Export certificate to PFX
$pwd = ConvertTo-SecureString -String 'password' -Force -AsPlainText
$cert = Get-ChildItem -Path Cert:\CurrentUser\My | Where-Object { $_.Subject -like '*resume-app-local.duckdns.org*' } | Select-Object -First 1
Export-PfxCertificate -Cert $cert -FilePath 'c:\Users\paula\Desktop\USA\App\finalProject\ResumeApp\ssl\certificate.pfx' -Password $pwd
Write-Host "Certificate exported to certificate.pfx"