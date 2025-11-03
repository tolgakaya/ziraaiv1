# Excel File Examples for Bulk Dealer Invitation

Bu klasörde toplu bayi davetiyesi için **3 farklı örnek CSV dosyası** bulunmaktadır. Excel dosyasına dönüştürmek için bu CSV dosyalarını Excel'de açıp `.xlsx` olarak kaydedebilirsiniz.

---

## Dosya Listesi

### 1️⃣ **1_basic_example.csv** - Temel Örnek (Tüm Alanlar Dolu)

**Kullanım Senaryosu**: Her bayi için tam bilgi verildiğinde

**İçerik Özeti**:
- 10 bayi
- Tüm alanlar dolu (Email, Phone, DealerName, PackageTier, CodeCount)
- Farklı telefon formatları (4 format örneği)
- Farklı paket seviyeleri (S, M, L, XL)

**Örnek Satırlar**:
```
Email                           Phone             DealerName      PackageTier  CodeCount
ahmet.yilmaz@example.com        +905551234567     Ahmet Yılmaz    M            50
mehmet.kaya@example.com         05552345678       Mehmet Kaya     L            100
ayse.demir@example.com          5553456789        Ayşe Demir      S            25
fatma.ozturk@example.com        905554567890      Fatma Öztürk    XL           200
```

**Ne Zaman Kullanılır**:
- Her bayi için özel paket seviyesi ve kod sayısı verilecekse
- Bayi isimleri biliniyor ve kaydedilmek isteniyorsa
- Farklı bayilere farklı paketler atanacaksa

---

### 2️⃣ **2_minimal_example.csv** - Minimal Örnek (Sadece Zorunlu Alanlar)

**Kullanım Senaryosu**: Varsayılan değerler kullanılacak, sadece iletişim bilgileri verilecek

**İçerik Özeti**:
- 5 bayi
- Sadece Email ve Phone dolu
- DealerName, PackageTier, CodeCount boş (API request'teki default değerler kullanılacak)

**Örnek Satırlar**:
```
Email                    Phone             DealerName  PackageTier  CodeCount
dealer1@example.com      +905551111111     
dealer2@example.com      05552222222       
dealer3@example.com      5553333333        
dealer4@example.com      905554444444      
```

**Ne Zaman Kullanılır**:
- Tüm bayilere aynı paket ve kod sayısı verilecekse
- Bayi isimleri bilinmiyorsa veya önemsizse
- Hızlı toplu davetiye gönderilecekse

**API Request Örneği**:
```json
{
  "file": "2_minimal_example.xlsx",
  "invitationType": "Invite",
  "defaultTier": "M",          // Bu değer kullanılacak
  "defaultCodeCount": 50,      // Bu değer kullanılacak
  "sendSms": true
}
```

**Sonuç**: Her 5 bayi için M paketi ve 50 kod atanacak.

---

### 3️⃣ **3_mixed_example.csv** - Karışık Örnek (Bazı Alanlar Dolu/Boş)

**Kullanım Senaryosu**: Bazı bayiler için özel değerler, bazıları için varsayılan değerler

**İçerik Özeti**:
- 10 bayi
- Bazı satırlarda tüm alanlar dolu
- Bazı satırlarda sadece Email ve Phone
- Bazı satırlarda DealerName var, PackageTier/CodeCount yok
- Bazı satırlarda PackageTier var, DealerName/CodeCount yok

**Örnek Satırlar**:
```
Email                           Phone             DealerName           PackageTier  CodeCount
complete.data@example.com       +905551234567     Tam Bilgi Bayisi     M            50
only.email.phone@example.com    05552345678       
with.name@example.com           5553456789        Sadece İsimli Bayi   
with.tier@example.com           905554567890                           L            
with.codes@example.com          +905555678901                                       100
```

**Ne Zaman Kullanılır**:
- Bazı bayiler için özel, bazıları için varsayılan değerler kullanılacaksa
- Excel'i manuel olarak dolduran kişi bazı alanları atladıysa
- Mevcut bir Excel listesi var ve eksik alanlar varsayılanlarla doldurulacaksa

**API Request Örneği**:
```json
{
  "file": "3_mixed_example.xlsx",
  "invitationType": "Invite",
  "defaultTier": "S",          // Boş PackageTier için kullanılacak
  "defaultCodeCount": 25,      // Boş CodeCount için kullanılacak
  "sendSms": true
}
```

**Sonuç (satır satır)**:
1. `complete.data@example.com` → M paketi, 50 kod (Excel'den)
2. `only.email.phone@example.com` → S paketi, 25 kod (varsayılan)
3. `with.name@example.com` → S paketi, 25 kod (varsayılan), "Sadece İsimli Bayi" ismiyle
4. `with.tier@example.com` → L paketi (Excel'den), 25 kod (varsayılan)
5. `with.codes@example.com` → S paketi (varsayılan), 100 kod (Excel'den)

---

## Sütun Açıklamaları

### 📧 **Email** (ZORUNLU)
- **Format**: Geçerli email formatı (örn: `user@domain.com`)
- **Kısıtlar**: 
  - Benzersiz olmalı (sistemde kayıtlı olmamalı)
  - Geçerli email formatı
- **Örnek**: `ahmet.yilmaz@example.com`

### 📱 **Phone** (ZORUNLU)
- **Format**: Türk telefon numarası (4 farklı format desteklenir)
- **Desteklenen Formatlar**:
  - `+905551234567` (Uluslararası format)
  - `905551234567` (Ülke kodu ile)
  - `05551234567` (Yerel format)
  - `5551234567` (Kısa format)
- **Kısıtlar**:
  - Benzersiz olmalı (sistemde kayıtlı olmamalı)
  - Türk telefon numarası (5xx ile başlamalı)
- **Örnek**: `+905551234567`, `05552345678`, `5553456789`

### 👤 **DealerName** (OPSİYONEL)
- **Format**: Metin (bayi adı)
- **Boşsa**: Email'in @ işaretinden önceki kısmı kullanılır
- **Örnek**: `Ahmet Yılmaz`
- **Boş Örnek**: `dealer1@example.com` → "dealer1" kullanılır

### 📦 **PackageTier** (OPSİYONEL)
- **Format**: S, M, L veya XL
- **Boşsa**: API request'teki `defaultTier` değeri kullanılır
- **Örnekler**:
  - `S` = Small (Küçük paket)
  - `M` = Medium (Orta paket)
  - `L` = Large (Büyük paket)
  - `XL` = Extra Large (Çok büyük paket)

### 🔢 **CodeCount** (OPSİYONEL)
- **Format**: Pozitif tam sayı
- **Boşsa**: API request'teki `defaultCodeCount` değeri kullanılır
- **Minimum**: 1
- **Örnek**: `50`, `100`, `250`

---

## Telefon Numarası Format Örnekleri

| Excel'deki Değer | Geçerli mi? | Açıklama |
|------------------|-------------|----------|
| `+905551234567` | ✅ Geçerli | Uluslararası format (tavsiye edilen) |
| `905551234567` | ✅ Geçerli | Ülke kodu ile |
| `05551234567` | ✅ Geçerli | Yerel format (0 ile başlayan) |
| `5551234567` | ✅ Geçerli | Kısa format (0 olmadan) |
| `+90 555 123 45 67` | ❌ Geçersiz | Boşluk içermemeli |
| `0555-123-4567` | ❌ Geçersiz | Tire içermemeli |
| `+1-555-123-4567` | ❌ Geçersiz | Sadece Türk numaraları (+90) |
| `5551234` | ❌ Geçersiz | Eksik basamak (11 basamak olmalı) |

---

## Excel Dosyası Oluşturma Adımları

### Yöntem 1: CSV'den Excel'e Dönüştürme

1. Yukarıdaki CSV dosyalarından birini seç
2. Excel'de aç (Dosya → Aç → CSV dosyasını seç)
3. "Farklı Kaydet" → `.xlsx` formatını seç
4. Dosyayı kaydet

### Yöntem 2: Sıfırdan Excel Oluşturma

1. Yeni Excel dosyası oluştur
2. İlk satıra (header) şu sütun isimlerini yaz:
   ```
   Email | Phone | DealerName | PackageTier | CodeCount
   ```
3. İkinci satırdan itibaren bayi bilgilerini doldur
4. Dosyayı `.xlsx` olarak kaydet

### Yöntem 3: Mevcut Listeden Uyarlama

Eğer elinde mevcut bir Excel listesi varsa:

1. Sütun isimlerini yukarıdaki formata uygun değiştir
2. `Email` ve `Phone` sütunlarının dolu olduğundan emin ol
3. Diğer sütunları opsiyonel olarak doldur
4. Dosyayı `.xlsx` formatında kaydet

---

## Validasyon Kuralları

### ✅ Geçerli Excel Dosyası

```csv
Email,Phone,DealerName,PackageTier,CodeCount
ahmet@example.com,+905551234567,Ahmet Yılmaz,M,50
mehmet@example.com,05552345678,Mehmet Kaya,L,100
```

**Neden Geçerli**:
- İlk satır header (sütun isimleri)
- Email formatları doğru
- Telefon numaraları desteklenen formatlarda
- PackageTier değerleri S/M/L/XL'den biri
- CodeCount pozitif tam sayı

---

### ❌ Geçersiz Excel Örnekleri

#### Örnek 1: Geçersiz Email
```csv
Email,Phone,DealerName,PackageTier,CodeCount
invalid-email,+905551234567,Ahmet Yılmaz,M,50
ahmet@,05552345678,Mehmet Kaya,L,100
```

**Hata**: 
- Satır 2: Email formatı geçersiz (@ ve domain yok)
- Satır 3: Email formatı geçersiz (domain eksik)

---

#### Örnek 2: Geçersiz Telefon
```csv
Email,Phone,DealerName,PackageTier,CodeCount
ahmet@example.com,123456,Ahmet Yılmaz,M,50
mehmet@example.com,+1-555-1234,Mehmet Kaya,L,100
```

**Hata**:
- Satır 2: Telefon numarası çok kısa (Türk telefon formatı değil)
- Satır 3: Yabancı telefon numarası (sadece +90 desteklenir)

---

#### Örnek 3: Duplicate Email/Phone
```csv
Email,Phone,DealerName,PackageTier,CodeCount
ahmet@example.com,+905551234567,Ahmet Yılmaz,M,50
ahmet@example.com,05552345678,Mehmet Kaya,L,100
ayse@example.com,+905551234567,Ayşe Demir,S,25
```

**Hata**:
- Satır 3: Email duplicate (satır 2'de de var)
- Satır 4: Telefon duplicate (satır 2'de de var)

---

#### Örnek 4: Email/Phone Zaten Sistemde Kayıtlı
```csv
Email,Phone,DealerName,PackageTier,CodeCount
existing@dealer.com,+905551234567,Ahmet Yılmaz,M,50
newdealer@example.com,05559999999,Mehmet Kaya,L,100
```

**Hata** (eğer sistemde kayıtlıysa):
- Satır 2: Email zaten kullanımda (existing@dealer.com sistemde var)
- Satır 3: Telefon numarası zaten kullanımda (05559999999 sistemde var)

---

#### Örnek 5: Geçersiz PackageTier
```csv
Email,Phone,DealerName,PackageTier,CodeCount
ahmet@example.com,+905551234567,Ahmet Yılmaz,MEDIUM,50
mehmet@example.com,05552345678,Mehmet Kaya,Large,100
```

**Hata**:
- Satır 2: PackageTier "MEDIUM" geçersiz (S/M/L/XL olmalı)
- Satır 3: PackageTier "Large" geçersiz (büyük/küçük harf önemli, "L" olmalı)

---

#### Örnek 6: Geçersiz CodeCount
```csv
Email,Phone,DealerName,PackageTier,CodeCount
ahmet@example.com,+905551234567,Ahmet Yılmaz,M,0
mehmet@example.com,05552345678,Mehmet Kaya,L,-50
ayse@example.com,5553456789,Ayşe Demir,S,abc
```

**Hata**:
- Satır 2: CodeCount 0 (minimum 1 olmalı)
- Satır 3: CodeCount negatif (-50)
- Satır 4: CodeCount sayı değil (abc)

---

## Dosya Boyutu ve Satır Limitleri

| Kısıt | Değer | Açıklama |
|-------|-------|----------|
| **Maksimum Dosya Boyutu** | 5 MB | Daha büyük dosyalar reddedilir |
| **Maksimum Satır Sayısı** | 2000 bayi | Tek seferde max 2000 bayi davet edilebilir |
| **Minimum Satır Sayısı** | 1 bayi | En az 1 bayi olmalı |
| **Desteklenen Formatlar** | .xlsx, .xls | Sadece Excel dosyaları |

**Not**: CSV dosyaları doğrudan desteklenmez, önce Excel'e dönüştürülmeli.

---

## Sık Sorulan Sorular (SSS)

### ❓ Eğer PackageTier boşsa ne olur?

API request'teki `defaultTier` değeri kullanılır.

**Örnek**:
```json
{
  "defaultTier": "M"  // Boş PackageTier için M kullanılacak
}
```

---

### ❓ DealerName boşsa ne olur?

Email'in @ işaretinden önceki kısmı otomatik olarak isim olarak kullanılır.

**Örnek**:
- Email: `ahmet.yilmaz@example.com`
- DealerName boş → "ahmet.yilmaz" kullanılır

---

### ❓ Telefon numarası hangi formatta girilmeli?

4 format desteklenir, en güvenlisi `+905551234567` formatıdır.

---

### ❓ Excel'de boş satırlar olabilir mi?

Hayır, boş satırlar hata verir. Tüm satırlarda en az Email ve Phone dolu olmalı.

---

### ❓ Sütun sırası önemli mi?

Hayır, önemli olan header (ilk satır) sütun isimlerinin doğru olması. Sütunlar farklı sırada olabilir.

**Geçerli Örnek**:
```csv
Phone,Email,CodeCount,PackageTier,DealerName
+905551234567,ahmet@example.com,50,M,Ahmet Yılmaz
```

---

### ❓ Excel'de formül kullanabilir miyim?

Hayır, sadece düz metin değerler desteklenir. Formüller çalıştırılmaz.

---

### ❓ Türkçe karakter kullanabilir miyim?

Evet, DealerName alanında Türkçe karakterler (ç, ğ, ı, ö, ş, ü) kullanılabilir.

---

### ❓ 2000'den fazla bayi eklemek istersem?

Excel'i birden fazla dosyaya böl ve her birini ayrı ayrı yükle. Sistem her dosyayı ayrı bir job olarak işler.

---

## Test için Hazır Excel Dosyaları

Bu klasördeki CSV dosyalarını kullanarak test edebilirsin:

1. **1_basic_example.csv** → Tüm alanlar dolu, gerçek senaryo testi
2. **2_minimal_example.csv** → Varsayılan değer testi
3. **3_mixed_example.csv** → Karışık senaryo testi

**Test Adımları**:
1. CSV dosyasını Excel'de aç
2. `.xlsx` olarak kaydet
3. API'ye yükle
4. SignalR ile progress takip et
5. Sonuçları kontrol et

---

## Hata Mesajları ve Çözümleri

### Hata: "Dosya boyutu 5MB'dan küçük olmalıdır"

**Çözüm**: Excel dosyasını küçült (satır sayısını azalt) veya birden fazla dosyaya böl.

---

### Hata: "Satır X: Email formatı geçersiz"

**Çözüm**: X. satırdaki email'i kontrol et, geçerli format: `user@domain.com`

---

### Hata: "Satır X: Telefon numarası formatı geçersiz"

**Çözüm**: X. satırdaki telefonu kontrol et, desteklenen formatlar:
- `+905551234567`
- `905551234567`
- `05551234567`
- `5551234567`

---

### Hata: "Satır X: Email zaten kullanımda"

**Çözüm**: Bu email sistemde kayıtlı, farklı bir email kullan.

---

### Hata: "Yetersiz kod. Sponsor'ün X kodu var ancak Y kod gerekiyor"

**Çözüm**: 
1. Excel'deki satır sayısını azalt, veya
2. Excel'deki CodeCount değerlerini azalt, veya
3. Sponsor'e daha fazla kod satın al

---

## Sonuç

Bu örnekler ve açıklamalarla:

✅ Excel dosyasını doğru formatta oluşturabilirsin  
✅ Zorunlu ve opsiyonel alanları anlayabilirsin  
✅ Telefon numarası formatlarını öğrenebilirsin  
✅ Validasyon hatalarını önleyebilirsin  
✅ Farklı senaryolar için örnekleri kullanabilirsin  

**Önerilen Başlangıç**: `1_basic_example.csv` dosyasını Excel'e dönüştür ve test et!
