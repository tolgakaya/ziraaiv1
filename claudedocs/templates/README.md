# Bulk Farmer Code Distribution - Excel Templates

Bu klasör, toplu farmer kod dağıtımı için Excel/CSV template dosyalarını içerir.

## Template Dosyaları

### 1. Minimal Template
**Dosya:** `bulk-farmer-code-distribution-template-minimal.csv`

**İçerik:**
- Email (Zorunlu)
- Phone (Zorunlu)
- CodeCount (Zorunlu)

**Kullanım:**
```csv
Email,Phone,CodeCount
farmer1@example.com,05321234567,1
farmer2@example.com,+905429876543,2
```

### 2. Full Template
**Dosya:** `bulk-farmer-code-distribution-template-full.csv`

**İçerik:**
- Email (Zorunlu)
- Phone (Zorunlu)
- CodeCount (Zorunlu)
- FarmerName (Opsiyonel)

**Kullanım:**
```csv
Email,Phone,CodeCount,FarmerName
ahmet.yilmaz@gmail.com,05321234567,1,Ahmet Yılmaz
mehmet.demir@hotmail.com,+905429876543,2,Mehmet Demir
```

### 3. Sample Data
**Dosya:** `bulk-farmer-code-distribution-sample-data.csv`

10 örnek farmer verisi içerir. Test ve demo amaçlı kullanılabilir.

## Excel Dosyası Oluşturma

CSV dosyalarını Excel'de açıp `.xlsx` formatında kaydedebilirsiniz:

1. CSV dosyasını Excel'de açın
2. File → Save As → Excel Workbook (.xlsx)
3. UTF-8 encoding'i seçin (Türkçe karakterler için)

## Frontend Entegrasyonu

### API Endpoint (Önerilen)

Template dosyalarını indirmek için API endpoint oluşturabilirsiniz:

```csharp
// WebAPI/Controllers/SponsorshipController.cs

[HttpGet("bulk-code-distribution/templates/{templateType}")]
[AllowAnonymous]
public IActionResult DownloadTemplate(string templateType)
{
    var fileName = templateType.ToLower() switch
    {
        "minimal" => "bulk-farmer-code-distribution-template-minimal.xlsx",
        "full" => "bulk-farmer-code-distribution-template-full.xlsx",
        "sample" => "bulk-farmer-code-distribution-sample-data.xlsx",
        _ => null
    };

    if (fileName == null)
    {
        return BadRequest(new { message = "Invalid template type. Use: minimal, full, sample" });
    }

    var filePath = Path.Combine(_hostingEnvironment.WebRootPath, "templates", fileName);

    if (!System.IO.File.Exists(filePath))
    {
        return NotFound(new { message = "Template file not found" });
    }

    var fileBytes = System.IO.File.ReadAllBytes(filePath);
    var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    return File(fileBytes, contentType, fileName);
}
```

### Frontend Kullanımı

```typescript
// Download template
const downloadTemplate = async (templateType: 'minimal' | 'full' | 'sample') => {
  const response = await fetch(
    `/api/v1/sponsorship/bulk-code-distribution/templates/${templateType}`,
    {
      method: 'GET',
      headers: {
        'x-dev-arch-version': '1.0'
      }
    }
  );

  if (!response.ok) {
    throw new Error('Template download failed');
  }

  const blob = await response.blob();
  const url = window.URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = `bulk-farmer-code-distribution-template-${templateType}.xlsx`;
  document.body.appendChild(a);
  a.click();
  window.URL.revokeObjectURL(url);
  document.body.removeChild(a);
};

// Usage
<button onClick={() => downloadTemplate('minimal')}>
  Download Minimal Template
</button>
<button onClick={() => downloadTemplate('full')}>
  Download Full Template
</button>
<button onClick={() => downloadTemplate('sample')}>
  Download Sample Data
</button>
```

## Alternatif: Statik Dosya Servisi

Template dosyalarını `wwwroot/templates/` klasörüne koyup statik olarak servis edebilirsiniz:

```csharp
// Program.cs veya Startup.cs
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(env.ContentRootPath, "wwwroot/templates")),
    RequestPath = "/templates"
});
```

Frontend'den direkt link:
```html
<a href="/templates/bulk-farmer-code-distribution-template-minimal.xlsx" download>
  Download Minimal Template
</a>
```

## Excel Creation (Server-Side)

.NET'te dinamik Excel oluşturmak için EPPlus kullanabilirsiniz:

```bash
dotnet add package EPPlus
```

```csharp
using OfficeOpenXml;

public class ExcelTemplateGenerator
{
    public byte[] GenerateMinimalTemplate()
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("Farmer Code Distribution");

        // Headers
        worksheet.Cells[1, 1].Value = "Email";
        worksheet.Cells[1, 2].Value = "Phone";
        worksheet.Cells[1, 3].Value = "CodeCount";

        // Style headers
        using (var range = worksheet.Cells[1, 1, 1, 3])
        {
            range.Style.Font.Bold = true;
            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
            range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(68, 114, 196));
            range.Style.Font.Color.SetColor(Color.White);
            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        }

        // Column widths
        worksheet.Column(1).Width = 30;
        worksheet.Column(2).Width = 20;
        worksheet.Column(3).Width = 15;

        return package.GetAsByteArray();
    }
}
```

## Validasyon Kuralları

Template'lere uygun veri girişi için kurallar:

### Email
- Format: `user@domain.com`
- Örnek: `ahmet.yilmaz@gmail.com`
- ❌ Hatalı: `ahmet.yilmaz`, `@gmail.com`

### Phone
- Kabul edilen formatlar:
  - `05321234567`
  - `+905321234567`
  - `905321234567`
  - `5321234567`
  - `0532 123 45 67`
  - `(0532) 123-45-67`
- Hepsi normalize edilir: `05321234567`

### CodeCount
- Minimum: 1
- Maximum: 10
- Tip: Tam sayı
- ❌ Hatalı: `0`, `11`, `1.5`, `abc`

### FarmerName (Opsiyonel)
- Maximum: 200 karakter
- Türkçe karakter desteklenir
- Boş bırakılabilir

## Hata Ayıklama

### Excel Dosyası Açılmıyor
- Dosya uzantısını `.xlsx` olarak değiştirin
- UTF-8 encoding kullanın
- Excel'de "Text to Columns" özelliğini kullanın

### Türkçe Karakterler Bozuk
- UTF-8 BOM encoding kullanın
- Excel'de import ederken "UTF-8" seçin

### Validasyon Hataları
- Header satırının tam olarak eşleştiğinden emin olun
- Boş satır bırakmayın
- Her hücrenin doğru formatta olduğunu kontrol edin

## İletişim

Sorunlar veya öneriler için: [GitHub Issues](https://github.com/tolgakaya/ziraaiv1/issues)
