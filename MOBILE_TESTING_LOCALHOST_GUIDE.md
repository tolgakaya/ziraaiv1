# Mobile Deep Link Testing - Localhost & Staging Guide

## Overview
Bu rehber mobil uygulama deep linking sistemini localhost ve staging ortamında nasıl test edeceğinizi gösterir.

## 🚀 Hızlı Test Senaryoları

### Senaryo 1: Mobil Browser Simülasyonu (En Kolay)

#### Chrome DevTools ile Mobil Test
```bash
# 1. Chrome'u aç ve localhost'a git
https://localhost:5001

# 2. DevTools aç (F12)
# 3. Device toolbar aktive et (Ctrl+Shift+M)
# 4. Cihaz seç (iPhone 12 Pro, Galaxy S20, vb.)
# 5. Test redemption link'ini aç
https://localhost:5001/redeem/SPONSOR-2025-ABC123
```

**Beklenen Davranış:**
- ✅ User-Agent detection çalışacak
- ✅ Mobil layout gösterilecek
- ✅ Deep link denemesi yapılacak (`ziraai://redeem?code=...`)
- ✅ 3 saniye sonra fallback gösterilecek

#### PowerShell ile Mobil User-Agent Test
```powershell
# Mobile User-Agent ile test
$mobileHeaders = @{
    'User-Agent' = 'Mozilla/5.0 (iPhone; CPU iPhone OS 14_0 like Mac OS X) AppleWebKit/605.1.15'
    'Accept' = 'text/html,application/xhtml+xml'
}

$response = Invoke-WebRequest -Uri "https://localhost:5001/redeem/TEST-CODE-123" -Headers $mobileHeaders
Write-Host "Response received:" -ForegroundColor Green
$response.Content | Out-File -FilePath "mobile_response.html"
Write-Host "Response saved to mobile_response.html" -ForegroundColor Gray

# HTML'de deep link kontrolü
if ($response.Content -match "ziraai://") {
    Write-Host "✅ Deep link found in response" -ForegroundColor Green
} else {
    Write-Host "❌ Deep link not found" -ForegroundColor Red
}
```

### Senaryo 2: Gerçek Mobil Cihaz Testi

#### Android Cihaz ile Test
```bash
# 1. Android cihazı aynı ağa bağla
# 2. Localhost yerine IP kullan
ipconfig  # Windows'ta IP öğren (örn: 192.168.1.100)

# 3. Mobil browser'da test et
https://192.168.1.100:5001/redeem/SPONSOR-2025-ABC123

# 4. Chrome Remote Debugging ile izle
chrome://inspect/#devices
```

#### iOS Cihaz ile Test
```bash
# 1. iOS cihazı aynı ağa bağla
# 2. Safari'de test et
https://192.168.1.100:5001/redeem/SPONSOR-2025-ABC123

# 3. Mac'te Safari Web Inspector ile debug
Safari → Develop → [Device Name] → localhost
```

### Senaryo 3: Deep Link Protocol Handler Testi

#### Windows'ta Custom Protocol Kaydetme
```powershell
# Registry'ye custom protocol ekle (Admin gerekli)
function Register-ZiraAIProtocol {
    $protocolKey = "HKEY_CLASSES_ROOT\ziraai"
    
    # Protocol key oluştur
    New-Item -Path "Registry::$protocolKey" -Force
    Set-ItemProperty -Path "Registry::$protocolKey" -Name "(Default)" -Value "URL:ZiraAI Protocol"
    Set-ItemProperty -Path "Registry::$protocolKey" -Name "URL Protocol" -Value ""
    
    # Command key oluştur
    $commandKey = "$protocolKey\shell\open\command"
    New-Item -Path "Registry::$commandKey" -Force
    Set-ItemProperty -Path "Registry::$commandKey" -Name "(Default)" -Value "`"notepad.exe`" `"%1`""
    
    Write-Host "✅ ZiraAI protocol registered" -ForegroundColor Green
    Write-Host "Deep links will now open in Notepad for testing" -ForegroundColor Gray
}

# Protocol'ü kaydet
Register-ZiraAIProtocol

# Test et
Start-Process "ziraai://redeem?code=TEST&token=ABC123"
```

#### macOS'ta Custom Protocol Testi
```bash
# Temporary app bundle oluştur
mkdir -p /tmp/ZiraAITest.app/Contents/MacOS
cat > /tmp/ZiraAITest.app/Contents/Info.plist << EOF
<?xml version="1.0" encoding="UTF-8"?>
<plist>
<dict>
    <key>CFBundleURLTypes</key>
    <array>
        <dict>
            <key>CFBundleURLSchemes</key>
            <array>
                <string>ziraai</string>
            </array>
        </dict>
    </array>
</dict>
</plist>
EOF

# Register and test
open /tmp/ZiraAITest.app
open "ziraai://redeem?code=TEST&token=ABC123"
```

## 🧪 Kapsamlı Test Scripti

### Mobil Test Otomasyonu
```powershell
# test_mobile_redemption.ps1
param(
    [string]$BaseUrl = "https://localhost:5001",
    [string]$TestCode = "MOBILE-TEST-$(Get-Date -Format 'HHmmss')",
    [string]$SponsorToken = ""
)

[System.Net.ServicePointManager]::ServerCertificateValidationCallback = {$true}

Write-Host "🧪 Mobile Redemption Testing Suite" -ForegroundColor Cyan
Write-Host "=================================" -ForegroundColor Cyan

# Test User Agents
$userAgents = @{
    "iPhone 12 Pro" = "Mozilla/5.0 (iPhone; CPU iPhone OS 14_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/14.0 Mobile/15E148 Safari/604.1"
    "Samsung Galaxy S21" = "Mozilla/5.0 (Linux; Android 11; SM-G991B) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/89.0.4389.72 Mobile Safari/537.36"
    "iPad Pro" = "Mozilla/5.0 (iPad; CPU OS 14_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/14.0 Mobile/15E148 Safari/604.1"
    "Desktop Chrome" = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/89.0.4389.72 Safari/537.36"
}

# Create test code if sponsor token provided
if ($SponsorToken) {
    Write-Host "`n1️⃣ Creating test sponsorship code..." -ForegroundColor Yellow
    
    $codeData = @{
        farmerName = "Mobile Test User"
        farmerPhone = "555$(Get-Random -Minimum 1000000 -Maximum 9999999)"
        amount = 50.00
        description = "Mobile deep link test - $(Get-Date -Format 'yyyy-MM-dd HH:mm')"
        expiryDate = (Get-Date).AddDays(7).ToString("yyyy-MM-ddTHH:mm:ss")
    }
    
    try {
        $headers = @{
            'Authorization' = "Bearer $SponsorToken"
            'Content-Type' = 'application/json'
        }
        
        $codeResponse = Invoke-RestMethod -Uri "$BaseUrl/api/v1/sponsorship/codes" -Method POST -Body ($codeData | ConvertTo-Json) -Headers $headers
        $TestCode = $codeResponse.data.code
        
        Write-Host "   ✅ Test code created: $TestCode" -ForegroundColor Green
        
        # Send link to enable redemption
        $linkData = @{
            codes = @(@{
                code = $TestCode
                recipientName = $codeData.farmerName
                recipientPhone = $codeData.farmerPhone
            })
            sendVia = "Test"
            customMessage = "Mobile test link"
        }
        
        Invoke-RestMethod -Uri "$BaseUrl/api/v1/sponsorship/send-link" -Method POST -Body ($linkData | ConvertTo-Json) -Headers $headers | Out-Null
        Write-Host "   ✅ Redemption link activated" -ForegroundColor Green
    }
    catch {
        Write-Host "   ❌ Failed to create test code: $($_.Exception.Message)" -ForegroundColor Red
        $TestCode = "FALLBACK-TEST-CODE"
    }
}

Write-Host "`n2️⃣ Testing different User-Agents..." -ForegroundColor Yellow

foreach ($device in $userAgents.GetEnumerator()) {
    Write-Host "`n📱 Testing: $($device.Key)" -ForegroundColor Cyan
    
    $headers = @{
        'User-Agent' = $device.Value
        'Accept' = 'text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8'
    }
    
    try {
        $response = Invoke-WebRequest -Uri "$BaseUrl/redeem/$TestCode" -Headers $headers -TimeoutSec 10
        
        $isMobileUA = $device.Value -match "(Mobile|Android|iPhone|iPad)"
        $hasDeepLink = $response.Content -match "ziraai://"
        $hasAppStoreLink = $response.Content -match "(apps\.apple\.com|play\.google\.com)"
        $hasFallback = $response.Content -match "fallback"
        
        Write-Host "   Status Code: $($response.StatusCode)" -ForegroundColor Gray
        Write-Host "   Is Mobile UA: $isMobileUA" -ForegroundColor Gray
        Write-Host "   Has Deep Link: $hasDeepLink" -ForegroundColor $(if($hasDeepLink) {"Green"} else {"Red"})
        Write-Host "   Has App Store Link: $hasAppStoreLink" -ForegroundColor $(if($hasAppStoreLink) {"Green"} else {"Yellow"})
        Write-Host "   Has Fallback UI: $hasFallback" -ForegroundColor $(if($hasFallback) {"Green"} else {"Yellow"})
        
        # Mobile User-Agent kontrolü
        if ($isMobileUA -and $hasDeepLink) {
            Write-Host "   ✅ Mobile detection working correctly" -ForegroundColor Green
        } elseif (!$isMobileUA -and !$hasDeepLink) {
            Write-Host "   ✅ Desktop detection working correctly" -ForegroundColor Green
        } else {
            Write-Host "   ⚠️  User-Agent detection may need review" -ForegroundColor Yellow
        }
        
        # Save response for detailed analysis
        $filename = "response_$($device.Key -replace ' ', '_').html"
        $response.Content | Out-File -FilePath $filename -Encoding UTF8
        Write-Host "   📄 Response saved to: $filename" -ForegroundColor Gray
        
    }
    catch {
        Write-Host "   ❌ Request failed: $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host "`n3️⃣ Testing Deep Link Extraction..." -ForegroundColor Yellow

# Test mobile response for deep link details
$mobileHeaders = @{
    'User-Agent' = $userAgents["iPhone 12 Pro"]
    'Accept' = 'text/html'
}

try {
    $mobileResponse = Invoke-WebRequest -Uri "$BaseUrl/redeem/$TestCode" -Headers $mobileHeaders
    
    # Extract deep link using regex
    if ($mobileResponse.Content -match "ziraai://redeem\?code=([^&]+)&token=([^'`"]+)") {
        $extractedCode = $matches[1]
        $extractedToken = $matches[2]
        
        Write-Host "   ✅ Deep link extracted successfully" -ForegroundColor Green
        Write-Host "   📝 Code: $extractedCode" -ForegroundColor Gray
        Write-Host "   🔑 Token: $($extractedToken.Substring(0, 20))..." -ForegroundColor Gray
        
        # Test deep link format
        $deepLink = "ziraai://redeem?code=$extractedCode&token=$extractedToken"
        Write-Host "   🔗 Full deep link: $deepLink" -ForegroundColor Cyan
        
        # Validate token format (JWT should have 3 parts separated by dots)
        if ($extractedToken -match "^[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+$") {
            Write-Host "   ✅ Token format appears valid (JWT)" -ForegroundColor Green
        } else {
            Write-Host "   ⚠️  Token format unexpected" -ForegroundColor Yellow
        }
    } else {
        Write-Host "   ❌ Deep link not found in mobile response" -ForegroundColor Red
    }
}
catch {
    Write-Host "   ❌ Mobile response test failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n4️⃣ Testing JSON API Response..." -ForegroundColor Yellow

try {
    $apiHeaders = @{
        'Accept' = 'application/json'
        'Content-Type' = 'application/json'
        'User-Agent' = $userAgents["iPhone 12 Pro"]
    }
    
    $apiResponse = Invoke-RestMethod -Uri "$BaseUrl/api/v1/redeem/$TestCode" -Headers $apiHeaders
    
    if ($apiResponse.success) {
        Write-Host "   ✅ API redemption successful" -ForegroundColor Green
        Write-Host "   👤 User: $($apiResponse.data.user.fullName)" -ForegroundColor Gray
        Write-Host "   📧 Email: $($apiResponse.data.user.email)" -ForegroundColor Gray
        Write-Host "   🔑 Token: $($apiResponse.data.authentication.token.Substring(0, 20))..." -ForegroundColor Gray
        
        # Check if deep link info is provided
        if ($apiResponse.data.deepLink) {
            Write-Host "   🔗 Deep link provided: $($apiResponse.data.deepLink)" -ForegroundColor Green
        }
        
        if ($apiResponse.data.appStoreUrls) {
            Write-Host "   📱 App store URLs provided" -ForegroundColor Green
        }
    } else {
        Write-Host "   ❌ API redemption failed: $($apiResponse.message)" -ForegroundColor Red
    }
}
catch {
    Write-Host "   ❌ API test failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n5️⃣ Performance Testing..." -ForegroundColor Yellow

# Test response times
$iterations = 5
$responseTimes = @()

for ($i = 1; $i -le $iterations; $i++) {
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    
    try {
        Invoke-WebRequest -Uri "$BaseUrl/redeem/$TestCode" -Headers $mobileHeaders -TimeoutSec 5 | Out-Null
        $stopwatch.Stop()
        $responseTimes += $stopwatch.ElapsedMilliseconds
        Write-Host "   Test $i: $($stopwatch.ElapsedMilliseconds)ms" -ForegroundColor Gray
    }
    catch {
        Write-Host "   Test $i: Failed" -ForegroundColor Red
    }
}

if ($responseTimes.Count -gt 0) {
    $avgTime = ($responseTimes | Measure-Object -Average).Average
    $maxTime = ($responseTimes | Measure-Object -Maximum).Maximum
    $minTime = ($responseTimes | Measure-Object -Minimum).Minimum
    
    Write-Host "   📊 Performance Summary:" -ForegroundColor Green
    Write-Host "      Average: $([Math]::Round($avgTime, 2))ms" -ForegroundColor Gray
    Write-Host "      Min: $($minTime)ms" -ForegroundColor Gray
    Write-Host "      Max: $($maxTime)ms" -ForegroundColor Gray
    
    if ($avgTime -lt 1000) {
        Write-Host "   ✅ Performance is excellent (<1s)" -ForegroundColor Green
    } elseif ($avgTime -lt 3000) {
        Write-Host "   ⚠️  Performance is acceptable (<3s)" -ForegroundColor Yellow
    } else {
        Write-Host "   ❌ Performance may need optimization (>3s)" -ForegroundColor Red
    }
}

Write-Host "`n📋 Test Summary" -ForegroundColor Cyan
Write-Host "===============" -ForegroundColor Cyan
Write-Host "Test Code: $TestCode" -ForegroundColor White
Write-Host "Base URL: $BaseUrl" -ForegroundColor White
Write-Host "Timestamp: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor White

Write-Host "`n💡 Next Steps:" -ForegroundColor Yellow
Write-Host "1. Review generated HTML files for detailed analysis" -ForegroundColor Gray
Write-Host "2. Test deep link on actual mobile devices" -ForegroundColor Gray
Write-Host "3. Verify app store fallback behavior" -ForegroundColor Gray
Write-Host "4. Test with real mobile app when available" -ForegroundColor Gray

Write-Host "`n🎉 Mobile testing completed!" -ForegroundColor Green
```

### Script Kullanımı
```powershell
# Sadece mevcut endpoint'leri test et
./test_mobile_redemption.ps1

# Yeni test kodu oluştur ve test et
./test_mobile_redemption.ps1 -SponsorToken "eyJ0eXAiOiJKV1Q..."

# Farklı URL ile test et
./test_mobile_redemption.ps1 -BaseUrl "https://192.168.1.100:5001"

# Staging ortamında test et
./test_mobile_redemption.ps1 -BaseUrl "https://staging.ziraai.com" -SponsorToken "..."
```

## 🔧 Localhost Network Konfigürasyonu

### Local Network'te Mobil Test
```bash
# 1. Windows Firewall'da port açma
netsh advfirewall firewall add rule name="ZiraAI Dev Server" dir=in action=allow protocol=TCP localport=5001

# 2. IP adresini öğrenme
ipconfig | findstr IPv4

# 3. Mobil cihazdan erişim
# iPhone/Android browser'da:
https://192.168.1.XXX:5001/redeem/CODE
```

### SSL Certificate for Mobile Testing
```powershell
# Self-signed certificate oluştur (mobil test için)
$cert = New-SelfSignedCertificate -DnsName "localhost", "192.168.1.100" -CertStoreLocation "cert:\LocalMachine\My"

# Certificate'i trusted root'a ekle
$store = New-Object System.Security.Cryptography.X509Certificates.X509Store("Root","LocalMachine")
$store.Open("ReadWrite")
$store.Add($cert)
$store.Close()

Write-Host "Certificate installed. Restart API with HTTPS binding." -ForegroundColor Green
```

## 📱 Gerçek Cihaz Test Rehberi

### iOS Cihazda Test
```bash
# 1. Settings → Safari → Advanced → Web Inspector ON
# 2. Mac'te Safari → Develop → [Device] menüsünden debug
# 3. Network tab'da deep link attemptlerini izle
# 4. Console'da JavaScript errorları kontrol et
```

### Android Cihazda Test
```bash
# 1. Developer Options → USB Debugging ON
# 2. Chrome'da chrome://inspect/#devices
# 3. Remote debugging ile cihaza bağlan
# 4. Network ve Console tab'larını izle
```

### Debug Önerileri
```javascript
// Browser console'da deep link debug
console.log('Testing deep link attempt...');

// Override window.location for testing
const originalLocation = window.location;
Object.defineProperty(window, 'location', {
  value: {
    ...originalLocation,
    href: '',
    assign: (url) => {
      console.log('Deep link attempt:', url);
      if (url.startsWith('ziraai://')) {
        console.log('✅ Deep link detected:', url);
        // Simulate app not installed
        setTimeout(() => {
          console.log('⚠️ App not installed, showing fallback');
          document.getElementById('fallback').style.display = 'block';
        }, 2000);
      }
    }
  },
  writable: true
});
```

Bu rehber ile localhost ve staging ortamında mobil deep linking sistemini kapsamlı şekilde test edebilirsin! 🚀📱