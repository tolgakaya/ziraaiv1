# Frontend API Değişiklikleri - Bulk Dealer Invitation

## 📋 Özet
Bulk dealer invitation API'sinde önemli değişiklikler yapıldı. **DefaultTier, DefaultCodeCount ve UseRowSpecificCounts parametreleri KALDIRILDI**. Artık tüm konfigürasyon Excel dosyasında yapılıyor.

## ⚠️ Breaking Changes

### ❌ Kaldırılan API Parametreleri

**Eski API (ARTIK GEÇERSİZ):**
```typescript
// ❌ ARTIK KULLANILMAMALI
interface BulkDealerInvitationRequest {
  sponsorId: number;
  excelFile: File;
  invitationType: 'Invite' | 'AutoCreate';
  defaultTier?: 'S' | 'M' | 'L' | 'XL';     // ❌ KALDIRILDI
  defaultCodeCount?: number;                  // ❌ KALDIRILDI
  useRowSpecificCounts?: boolean;            // ❌ KALDIRILDI
  sendSms: boolean;
}
```

**Yeni API (GEÇERLİ):**
```typescript
// ✅ YENİ KULLANIM
interface BulkDealerInvitationRequest {
  sponsorId: number;
  excelFile: File;
  invitationType: 'Invite' | 'AutoCreate';
  sendSms: boolean;
}
```

### 📝 API Endpoint

**Endpoint:** `POST /api/v1/sponsorship/dealer/bulk-invite`

**Content-Type:** `multipart/form-data`

### ⚠️ CRITICAL: Form Field Names

**Excel dosyası field name'i MUTLAKA `ExcelFile` olmalı!**

| Field Name | Type | Example Value | Required |
|------------|------|---------------|----------|
| `SponsorId` | number (form field) | `159` | ✅ Yes |
| `ExcelFile` | file | [Excel file] | ✅ Yes |
| `InvitationType` | string | `"Invite"` or `"AutoCreate"` | ✅ Yes |
| `SendSms` | boolean (string) | `"true"` or `"false"` | ✅ Yes |

**Common Mistakes:**
- ❌ `file` → **Wrong field name**
- ❌ `excelFile` → **Case sensitive, wrong!**
- ❌ `excel` → **Wrong field name**
- ✅ `ExcelFile` → **Correct (case-sensitive)**

**Request Example (TypeScript/Axios):**
```typescript
const formData = new FormData();
// ⚠️ IMPORTANT: Field name must be 'ExcelFile' (case-sensitive)
formData.append('SponsorId', sponsorId.toString());
formData.append('ExcelFile', excelFile);  // ⚠️ Must be 'ExcelFile' exactly!
formData.append('InvitationType', 'Invite'); // or 'AutoCreate'
formData.append('SendSms', 'true');

// ❌ ARTIK GÖNDERİLMEMELİ:
// formData.append('DefaultTier', 'M');
// formData.append('DefaultCodeCount', '10');
// formData.append('UseRowSpecificCounts', 'true');

const response = await axios.post('/api/v1/sponsorship/dealer/bulk-invite', formData, {
  headers: {
    'Content-Type': 'multipart/form-data',
    'Authorization': `Bearer ${token}`,
    'x-dev-arch-version': '1.0'
  }
});
```

**React Example:**
```tsx
const handleBulkInvite = async (file: File) => {
  const formData = new FormData();
  formData.append('SponsorId', user.userId.toString());
  formData.append('ExcelFile', file);
  formData.append('InvitationType', invitationType); // 'Invite' or 'AutoCreate'
  formData.append('SendSms', sendSms ? 'true' : 'false');

  try {
    const response = await api.post('/api/v1/sponsorship/dealer/bulk-invite', formData);
    console.log('Bulk invitation started:', response.data);
  } catch (error) {
    console.error('Bulk invitation failed:', error);
  }
};
```

## 🆕 Yeni Excel Formatları

### Mod 1: Otomatik Dağıtım (Önerilen)

**Kullanım:** Tier bilgisi belirtilmeden sadece adet ile davet

**Excel Yapısı:**
```csv
Email,Phone,DealerName,CodeCount
dealer1@test.com,905551234567,Dealer 1,10
dealer2@test.com,905551234568,Dealer 2,15
dealer3@test.com,905551234569,Dealer 3,20
```

**Özellikler:**
- ✅ **PackageTier sütunu OLMAMALI**
- ✅ Sistem otomatik olarak mevcut tier'lardan kod dağıtır
- ✅ Süresine yakın kodlar önce kullanılır
- ✅ Tekli dealer davetleri ile aynı davranış

**UI'da Gösterilecek Mesaj:**
```
"Tier belirtmediniz. Sistem otomatik olarak mevcut kodlarınızdan 
dağıtım yapacak. Süresine yakın kodlar öncelikli olarak kullanılacak."
```

### Mod 2: Tier Bazlı (Gelişmiş)

**Kullanım:** Her dealer için spesifik tier belirtilmek istendiğinde

**Excel Yapısı:**
```csv
Email,Phone,DealerName,PackageTier,CodeCount
dealer1@test.com,905551234567,Dealer 1,M,10
dealer2@test.com,905551234568,Dealer 2,L,15
dealer3@test.com,905551234569,Dealer 3,S,5
```

**Özellikler:**
- ✅ **PackageTier sütunu TÜM satırlarda OLMALI**
- ✅ Her dealer için farklı tier belirtilebilir
- ✅ Sadece belirtilen tier'dan kod kullanılır
- ⚠️ Karma mod desteklenmiyor (bazı satırlarda tier, bazılarında yok)

**UI'da Gösterilecek Mesaj:**
```
"Tier bilgisi belirttiniz. Her dealer için sadece belirttiğiniz 
tier'dan kod kullanılacak."
```

## 🎨 Frontend UI Değişiklikleri

### ❌ Kaldırılması Gereken UI Elemanları

1. **Default Tier Seçimi**
```tsx
// ❌ KALDIRILMALI
<FormControl>
  <FormLabel>Default Tier</FormLabel>
  <Select value={defaultTier} onChange={setDefaultTier}>
    <option value="S">Small (S)</option>
    <option value="M">Medium (M)</option>
    <option value="L">Large (L)</option>
    <option value="XL">Extra Large (XL)</option>
  </Select>
</FormControl>
```

2. **Default Code Count Input**
```tsx
// ❌ KALDIRILMALI
<FormControl>
  <FormLabel>Default Kod Sayısı</FormLabel>
  <Input 
    type="number" 
    value={defaultCodeCount} 
    onChange={(e) => setDefaultCodeCount(e.target.value)} 
  />
</FormControl>
```

3. **Use Row Specific Counts Checkbox**
```tsx
// ❌ KALDIRILMALI
<Checkbox 
  checked={useRowSpecificCounts}
  onChange={(e) => setUseRowSpecificCounts(e.target.checked)}
>
  Excel'deki adet bilgilerini kullan
</Checkbox>
```

### ✅ Yeni/Güncellenen UI Elemanları

**1. Basitleştirilmiş Form:**
```tsx
const BulkInviteForm = () => {
  const [excelFile, setExcelFile] = useState<File | null>(null);
  const [invitationType, setInvitationType] = useState<'Invite' | 'AutoCreate'>('Invite');
  const [sendSms, setSendSms] = useState(true);

  return (
    <form onSubmit={handleSubmit}>
      {/* Excel File Upload */}
      <FormControl isRequired>
        <FormLabel>Excel Dosyası</FormLabel>
        <Input 
          type="file" 
          accept=".xlsx,.xls"
          onChange={(e) => setExcelFile(e.target.files?.[0] || null)}
        />
        <FormHelperText>
          Maksimum 5 MB, maksimum 2000 dealer
        </FormHelperText>
      </FormControl>

      {/* Invitation Type */}
      <FormControl isRequired>
        <FormLabel>Davet Tipi</FormLabel>
        <RadioGroup value={invitationType} onChange={setInvitationType}>
          <Radio value="Invite">Davet Gönder</Radio>
          <Radio value="AutoCreate">Otomatik Oluştur</Radio>
        </RadioGroup>
      </FormControl>

      {/* SMS Option */}
      <FormControl>
        <Checkbox checked={sendSms} onChange={(e) => setSendSms(e.target.checked)}>
          SMS Bildirimi Gönder
        </Checkbox>
      </FormControl>

      <Button type="submit" colorScheme="blue">
        Toplu Davet Başlat
      </Button>
    </form>
  );
};
```

**2. Excel Template Download:**
```tsx
const ExcelTemplateDownload = () => {
  const downloadTemplate = (mode: 'auto' | 'tier-specific') => {
    if (mode === 'auto') {
      // Otomatik dağıtım template
      const csvContent = `Email,Phone,DealerName,CodeCount
dealer1@example.com,905551234567,Dealer 1,10
dealer2@example.com,905551234568,Dealer 2,15`;
      
      const blob = new Blob([csvContent], { type: 'text/csv' });
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = 'bulk_invite_template_auto.csv';
      a.click();
    } else {
      // Tier-specific template
      const csvContent = `Email,Phone,DealerName,PackageTier,CodeCount
dealer1@example.com,905551234567,Dealer 1,M,10
dealer2@example.com,905551234568,Dealer 2,L,15`;
      
      const blob = new Blob([csvContent], { type: 'text/csv' });
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = 'bulk_invite_template_tier.csv';
      a.click();
    }
  };

  return (
    <Box>
      <Text fontWeight="bold" mb={2}>Excel Şablonu İndir:</Text>
      <HStack spacing={2}>
        <Button 
          size="sm" 
          leftIcon={<DownloadIcon />}
          onClick={() => downloadTemplate('auto')}
        >
          Otomatik Dağıtım
        </Button>
        <Button 
          size="sm" 
          leftIcon={<DownloadIcon />}
          onClick={() => downloadTemplate('tier-specific')}
        >
          Tier Bazlı
        </Button>
      </HStack>
    </Box>
  );
};
```

**3. Info Card (Kullanıcıları Bilgilendirme):**
```tsx
const BulkInviteInfoCard = () => {
  return (
    <Alert status="info" variant="left-accent">
      <AlertIcon />
      <Box>
        <AlertTitle>Excel Dosyası Hakkında</AlertTitle>
        <AlertDescription>
          <UnorderedList spacing={1} mt={2}>
            <ListItem>
              <strong>Otomatik Dağıtım:</strong> PackageTier sütunu olmadan yükleyin. 
              Sistem mevcut kodlarınızdan otomatik dağıtım yapar.
            </ListItem>
            <ListItem>
              <strong>Tier Bazlı:</strong> PackageTier sütunu ekleyin ve her dealer için 
              tier belirtin (S, M, L, XL).
            </ListItem>
            <ListItem>
              <strong>Gerekli Sütunlar:</strong> Email, Phone, CodeCount
            </ListItem>
            <ListItem>
              <strong>İsteğe Bağlı:</strong> DealerName, PackageTier
            </ListItem>
          </UnorderedList>
        </AlertDescription>
      </Box>
    </Alert>
  );
};
```

## 📊 Hata Mesajları (Frontend Handling)

### Yeni Hata Mesajları

**1. Otomatik Dağıtımda Yetersiz Kod:**
```json
{
  "success": false,
  "message": "Yetersiz kod. Gerekli: 100, Mevcut: 50 (tüm tier'lar)"
}
```

**Frontend Gösterimi:**
```tsx
<Alert status="error">
  <AlertIcon />
  <Box>
    <AlertTitle>Yetersiz Kod</AlertTitle>
    <AlertDescription>
      Toplamda {required} kod gerekiyor ancak tüm tier'larınızda 
      toplam {available} kod mevcut. Lütfen kod satın alın veya 
      dealer sayısını azaltın.
    </AlertDescription>
  </Box>
</Alert>
```

**2. Tier Bazlı Yetersiz Kod:**
```json
{
  "success": false,
  "message": "Yetersiz kod:\nM tier: 10 kod mevcut, 20 kod gerekli (Eksik: 10)\nL tier: 5 kod mevcut, 15 kod gerekli (Eksik: 10)"
}
```

**Frontend Gösterimi:**
```tsx
<Alert status="error">
  <AlertIcon />
  <Box>
    <AlertTitle>Tier Bazında Yetersiz Kod</AlertTitle>
    <AlertDescription>
      <UnorderedList>
        <ListItem>M Tier: 10 kod mevcut, 20 gerekli (10 eksik)</ListItem>
        <ListItem>L Tier: 5 kod mevcut, 15 gerekli (10 eksik)</ListItem>
      </UnorderedList>
    </AlertDescription>
  </Box>
</Alert>
```

**3. Karma Mod Hatası:**
```json
{
  "success": false,
  "message": "Karma mod desteklenmiyor. Tüm satırlar tier belirtmeli veya hiçbiri belirtmemeli. 5 satırda tier eksik."
}
```

**Frontend Gösterimi:**
```tsx
<Alert status="warning">
  <AlertIcon />
  <Box>
    <AlertTitle>Karma Mod Desteklenmiyor</AlertTitle>
    <AlertDescription>
      Excel dosyanızda bazı satırlarda PackageTier belirtilmiş, 
      bazılarında belirtilmemiş. Ya tüm satırlarda tier belirtin 
      ya da hiçbirinde belirtmeyin.
      <br />
      <strong>5 satırda tier bilgisi eksik.</strong>
    </AlertDescription>
  </Box>
</Alert>
```

## 🔄 Migration Guide (Mevcut Kod Güncellemesi)

### Adım 1: API Request Güncellemesi

**Eski Kod:**
```typescript
// ❌ ESKİ
const submitBulkInvite = async () => {
  const formData = new FormData();
  formData.append('SponsorId', sponsorId);
  formData.append('ExcelFile', excelFile);
  formData.append('InvitationType', invitationType);
  formData.append('DefaultTier', defaultTier);           // KALDIR
  formData.append('DefaultCodeCount', defaultCodeCount); // KALDIR
  formData.append('UseRowSpecificCounts', useRowSpecificCounts); // KALDIR
  formData.append('SendSms', sendSms);
  
  const response = await api.post('/api/v1/sponsorship/dealer/bulk-invite', formData);
};
```

**Yeni Kod:**
```typescript
// ✅ YENİ
const submitBulkInvite = async () => {
  const formData = new FormData();
  formData.append('SponsorId', sponsorId);
  formData.append('ExcelFile', excelFile);
  formData.append('InvitationType', invitationType);
  formData.append('SendSms', sendSms);
  
  const response = await api.post('/api/v1/sponsorship/dealer/bulk-invite', formData);
};
```

### Adım 2: State Temizleme

```typescript
// ❌ KALDIRILACAK state'ler
const [defaultTier, setDefaultTier] = useState('M');
const [defaultCodeCount, setDefaultCodeCount] = useState(10);
const [useRowSpecificCounts, setUseRowSpecificCounts] = useState(false);

// ✅ KALAN state'ler
const [excelFile, setExcelFile] = useState<File | null>(null);
const [invitationType, setInvitationType] = useState<'Invite' | 'AutoCreate'>('Invite');
const [sendSms, setSendSms] = useState(true);
```

### Adım 3: UI Component Güncellemesi

**Kaldırılacak bileşenler:**
```tsx
// ❌ Bunları KALDIR
<TierSelector value={defaultTier} onChange={setDefaultTier} />
<CodeCountInput value={defaultCodeCount} onChange={setDefaultCodeCount} />
<UseRowSpecificCheckbox checked={useRowSpecificCounts} onChange={setUseRowSpecificCounts} />
```

**Eklenecek bileşenler:**
```tsx
// ✅ Bunları EKLE
<ExcelTemplateDownload />
<BulkInviteInfoCard />
```

## 📱 Örnek Tam Sayfa Kodu (React + TypeScript)

```tsx
import React, { useState } from 'react';
import {
  Box, Button, FormControl, FormLabel, Input, Radio, RadioGroup,
  Checkbox, Alert, AlertIcon, AlertTitle, AlertDescription,
  VStack, HStack, Text, UnorderedList, ListItem
} from '@chakra-ui/react';
import { DownloadIcon } from '@chakra-ui/icons';
import axios from 'axios';

interface BulkInvitePageProps {
  sponsorId: number;
  authToken: string;
}

const BulkInvitePage: React.FC<BulkInvitePageProps> = ({ sponsorId, authToken }) => {
  const [excelFile, setExcelFile] = useState<File | null>(null);
  const [invitationType, setInvitationType] = useState<'Invite' | 'AutoCreate'>('Invite');
  const [sendSms, setSendSms] = useState(true);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<any>(null);

  const downloadTemplate = (mode: 'auto' | 'tier') => {
    const csvContent = mode === 'auto'
      ? `Email,Phone,DealerName,CodeCount\ndealer1@example.com,905551234567,Dealer 1,10`
      : `Email,Phone,DealerName,PackageTier,CodeCount\ndealer1@example.com,905551234567,Dealer 1,M,10`;
    
    const blob = new Blob([csvContent], { type: 'text/csv' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `bulk_invite_template_${mode}.csv`;
    a.click();
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!excelFile) {
      setError('Lütfen bir Excel dosyası seçin');
      return;
    }

    setLoading(true);
    setError(null);
    setSuccess(null);

    try {
      const formData = new FormData();
      formData.append('SponsorId', sponsorId.toString());
      formData.append('ExcelFile', excelFile);
      formData.append('InvitationType', invitationType);
      formData.append('SendSms', sendSms.toString());

      const response = await axios.post(
        '/api/v1/sponsorship/dealer/bulk-invite',
        formData,
        {
          headers: {
            'Content-Type': 'multipart/form-data',
            'Authorization': `Bearer ${authToken}`,
            'x-dev-arch-version': '1.0'
          }
        }
      );

      setSuccess(response.data);
    } catch (err: any) {
      setError(err.response?.data?.message || 'Toplu davet başlatılamadı');
    } finally {
      setLoading(false);
    }
  };

  return (
    <Box maxW="800px" mx="auto" p={6}>
      <Text fontSize="2xl" fontWeight="bold" mb={4}>
        Toplu Dealer Daveti
      </Text>

      {/* Info Card */}
      <Alert status="info" variant="left-accent" mb={6}>
        <AlertIcon />
        <Box>
          <AlertTitle>Excel Dosyası Hakkında</AlertTitle>
          <AlertDescription>
            <UnorderedList spacing={1} mt={2}>
              <ListItem>
                <strong>Otomatik Dağıtım:</strong> PackageTier olmadan yükleyin
              </ListItem>
              <ListItem>
                <strong>Tier Bazlı:</strong> Her satırda PackageTier belirtin
              </ListItem>
              <ListItem>
                <strong>Gerekli:</strong> Email, Phone, CodeCount
              </ListItem>
            </UnorderedList>
          </AlertDescription>
        </Box>
      </Alert>

      {/* Template Download */}
      <Box mb={6}>
        <Text fontWeight="bold" mb={2}>Excel Şablonu İndir:</Text>
        <HStack spacing={2}>
          <Button 
            size="sm" 
            leftIcon={<DownloadIcon />}
            onClick={() => downloadTemplate('auto')}
          >
            Otomatik Dağıtım
          </Button>
          <Button 
            size="sm" 
            leftIcon={<DownloadIcon />}
            onClick={() => downloadTemplate('tier')}
          >
            Tier Bazlı
          </Button>
        </HStack>
      </Box>

      {/* Form */}
      <form onSubmit={handleSubmit}>
        <VStack spacing={4} align="stretch">
          {/* Excel File */}
          <FormControl isRequired>
            <FormLabel>Excel Dosyası</FormLabel>
            <Input 
              type="file" 
              accept=".xlsx,.xls"
              onChange={(e) => setExcelFile(e.target.files?.[0] || null)}
            />
          </FormControl>

          {/* Invitation Type */}
          <FormControl isRequired>
            <FormLabel>Davet Tipi</FormLabel>
            <RadioGroup value={invitationType} onChange={(v) => setInvitationType(v as any)}>
              <VStack align="start">
                <Radio value="Invite">Davet Gönder</Radio>
                <Radio value="AutoCreate">Otomatik Oluştur</Radio>
              </VStack>
            </RadioGroup>
          </FormControl>

          {/* SMS */}
          <FormControl>
            <Checkbox 
              checked={sendSms} 
              onChange={(e) => setSendSms(e.target.checked)}
            >
              SMS Bildirimi Gönder
            </Checkbox>
          </FormControl>

          {/* Submit */}
          <Button 
            type="submit" 
            colorScheme="blue" 
            isLoading={loading}
            loadingText="Gönderiliyor..."
          >
            Toplu Davet Başlat
          </Button>
        </VStack>
      </form>

      {/* Error */}
      {error && (
        <Alert status="error" mt={4}>
          <AlertIcon />
          {error}
        </Alert>
      )}

      {/* Success */}
      {success && (
        <Alert status="success" mt={4}>
          <AlertIcon />
          <Box>
            <AlertTitle>Başarılı!</AlertTitle>
            <AlertDescription>
              {success.data.totalDealers} dealer için davet işlemi başlatıldı.
              <br />
              Job ID: {success.data.jobId}
            </AlertDescription>
          </Box>
        </Alert>
      )}
    </Box>
  );
};

export default BulkInvitePage;
```

## ✅ Checklist (Frontend Team)

### Backend API Değişiklikleri
- [ ] DefaultTier parametresi kaldırıldı (API'ye gönderilmemeli)
- [ ] DefaultCodeCount parametresi kaldırıldı (API'ye gönderilmemeli)
- [ ] UseRowSpecificCounts parametresi kaldırıldı (API'ye gönderilmemeli)
- [ ] API artık sadece 4 parametre alıyor: SponsorId, ExcelFile, InvitationType, SendSms

### UI Değişiklikleri
- [ ] DefaultTier seçim dropdown'u kaldırıldı
- [ ] DefaultCodeCount input alanı kaldırıldı
- [ ] UseRowSpecificCounts checkbox kaldırıldı
- [ ] Excel template download butonları eklendi (2 adet: auto, tier-specific)
- [ ] Bilgilendirme card'ı eklendi (Excel format açıklamaları)

### Excel Template
- [ ] Otomatik dağıtım template hazırlandı (PackageTier sütunu YOK)
- [ ] Tier-bazlı template hazırlandı (PackageTier sütunu VAR)
- [ ] Template'ler download edilebilir hale getirildi

### Error Handling
- [ ] Yeni hata mesajları handle ediliyor (otomatik dağıtım yetersiz kod)
- [ ] Tier-bazlı yetersiz kod hataları gösteriliyor
- [ ] Karma mod hatası gösteriliyor

### Test
- [ ] Otomatik dağıtım modu test edildi (PackageTier olmayan Excel)
- [ ] Tier-bazlı mod test edildi (PackageTier olan Excel)
- [ ] Karma mod reddi test edildi (bazı satırlarda tier var, bazılarında yok)
- [ ] API request'lerde eski parametreler gönderilmediği doğrulandı

## 🐛 Troubleshooting

### Error: "Dosya yüklenmedi"

**Symptom**: API returns error "Dosya yüklenmedi" (File not uploaded)

**Cause**: Incorrect form field name for Excel file

**Solution**: 
1. Check FormData field name is EXACTLY `ExcelFile` (case-sensitive)
2. Verify file object is not null/undefined
3. Check Content-Type header is `multipart/form-data`

**Debug Steps:**
```typescript
const formData = new FormData();
formData.append('ExcelFile', file); // ⚠️ Must be 'ExcelFile'

// Debug: Log FormData contents
for (let [key, value] of formData.entries()) {
  console.log('FormData:', key, value);
}
// Expected output: "FormData: ExcelFile [object File]"
```

**Common Mistakes:**
```typescript
// ❌ WRONG - lowercase 'e'
formData.append('excelFile', file);

// ❌ WRONG - different name
formData.append('file', file);

// ✅ CORRECT
formData.append('ExcelFile', file);
```

### Error: "AuthorizationsDenied"

**Cause**: Missing `BulkDealerInvitationCommand` operation claim

**Solution**: 
1. Run SQL migration: `claudedocs/Dealers/migrations/005_bulk_invitation_authorization.sql`
2. User must logout/login to refresh claim cache
3. Verify user is in Sponsor or Admin group

## 📞 Destek

Sorularınız için:
- Backend Lead: Tolga Kaya
- Documentation: `claudedocs/Dealers/BULK_INVITATION_EXCEL_FORMATS.md`
- Implementation Details: `claudedocs/Dealers/AUTO_ALLOCATION_IMPLEMENTATION.md`
