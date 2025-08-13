# Test FreeImageHost provider selection in Staging environment
# Larger base64 image for testing (small red square 10x10 pixels)
$testImage = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAoAAAAKCAYAAACNMs+9AAAASElEQVQYV2P8//8/AzYwirkI6f4f7W5i6YEJMqKDBRfIyCIogJGhsIiCHYwMZGQoLKJgByMDGRkKiyjYwchARobCIgp2MDKQkSEBAFp8pRIw+1YfAAAAAElFTkSuQmCC"

$payload = @{
    image = $testImage
    farmer_id = "test_farmer"
    sponsor_id = "test_sponsor"
    field_id = "test_field"
    crop_type = "test_crop"
    location = "test_location"
} | ConvertTo-Json

Write-Host "Testing Plant Analysis API with FreeImageHost provider..."
Write-Host "Expected: imageUrl should contain freeimage.host domain, NOT localhost"
Write-Host ""

try {
    # Skip certificate validation for localhost testing
    add-type @"
using System.Net;
using System.Security.Cryptography.X509Certificates;
public class TrustAllCertsPolicy : ICertificatePolicy {
    public bool CheckValidationResult(
        ServicePoint srvPoint, X509Certificate certificate,
        WebRequest request, int certificateProblem) {
        return true;
    }
}
"@
    [System.Net.ServicePointManager]::CertificatePolicy = New-Object TrustAllCertsPolicy
    [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12
    
    # Try the plant analysis endpoint directly on HTTPS (with AllowAnonymous it should work)
    Write-Host "Testing Plant Analysis endpoint on HTTPS..."
    $response = Invoke-RestMethod -Uri "https://localhost:5001/api/plantanalyses/analyze" -Method POST -Body $payload -ContentType "application/json"
    Write-Host "Success!"
    Write-Host "Response: $($response | ConvertTo-Json -Depth 10)"
} catch {
    Write-Host "Error occurred:"
    Write-Host $_.Exception.Message
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $errorContent = $reader.ReadToEnd()
        Write-Host "Error content: $errorContent"
    }
}