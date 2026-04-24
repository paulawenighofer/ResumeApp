# Generate self-signed certificate for HTTPS
$cert = New-SelfSignedCertificate -DnsName "resume-app-local.duckdns.org","grafana.resume-app-local.duckdns.org","prometheus.resume-app-local.duckdns.org" -CertStoreLocation Cert:\CurrentUser\My -NotAfter (Get-Date).AddYears(1) -KeyAlgorithm RSA -KeyLength 2048

# Export to PFX (for conversion)
$pfxPath = Join-Path $PSScriptRoot "certificate.pfx"
$password = ConvertTo-SecureString -String "password" -Force -AsPlainText
$cert | Export-PfxCertificate -FilePath $pfxPath -Password $password

# Convert to PEM format using openssl (if available) or create alternate cert
Write-Host "Certificate created with thumbprint: $($cert.Thumbprint)"
Write-Host "Exported to: $pfxPath"
Write-Host "Note: For nginx, you need cert.pem and key.pem in PEM format"