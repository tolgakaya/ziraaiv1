# Test script for async plant analysis API

# Test minimal base64 image
$testImageBase64 = "/9j/4AAQSkZJRgABAQEAYABgAAD/2wBDAAEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQH/2wBDAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQH/wAARCAABAAEDAREAAhEBAxEB/8QAFQABAQAAAAAAAAAAAAAAAAAAAAv/xAAUEAEAAAAAAAAAAAAAAAAAAAAA/8QAFQEBAQAAAAAAAAAAAAAAAAAAAAX/xAAUEQEAAAAAAAAAAAAAAAAAAAAA/9oADAMBAAIRAxEAPwA/AB"

$requestBody = @{
    image = "data:image/jpeg;base64,$testImageBase64"
    farmer_id = "farmer_test_001"
    sponsor_id = "sponsor_test_xyz"
    location = "Antalya, Turkey"
    gps_coordinates = @{
        lat = 36.8969
        lng = 30.7133
    }
    crop_type = "tomato"
    field_id = "field_test_01"
    urgency_level = "high"
    notes = "Test mesajı - yapraklarda sararma var"
} | ConvertTo-Json -Depth 3

Write-Host "Testing async endpoint..."
Write-Host "Request body: $requestBody"

# Test async endpoint
try {
    $response = Invoke-RestMethod -Uri "https://localhost:5001/api/plantanalyses/analyze-async" `
                                  -Method POST `
                                  -ContentType "application/json" `
                                  -Body $requestBody `
                                  -SkipCertificateCheck
    
    Write-Host "✓ Async endpoint response:" 
    $response | ConvertTo-Json -Depth 3
    
    # Test RabbitMQ health
    Write-Host "`n Testing RabbitMQ health..."
    $healthResponse = Invoke-RestMethod -Uri "https://localhost:5001/api/test/rabbitmq-health" `
                                       -Method GET `
                                       -SkipCertificateCheck
    
    Write-Host "✓ RabbitMQ health response:"
    $healthResponse | ConvertTo-Json -Depth 2
    
} catch {
    Write-Host "✗ Error testing API:"
    Write-Host $_.Exception.Message
    Write-Host $_.Exception.ResponseBody
}