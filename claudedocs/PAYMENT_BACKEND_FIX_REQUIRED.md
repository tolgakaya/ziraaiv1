# ÖDEME HATASI - Backend Fix Gerekli

## Hata Durumu
- **Ekran**: iyzico ödeme sayfası "Geçersiz istek" (errorCode: 11) hatası veriyor
- **Sebep**: Backend'den iyzico'ya gönderilen parametreler eksik

## Gerekli Backend Düzeltmesi

`/api/v1/payments/initialize` endpoint'inde iyzico'ya gönderilen request'e şu alanları ekleyin:

### 1. Buyer (Alıcı) Bilgileri - ZORUNLU
```json
{
  "buyer": {
    "id": "134",  // userId
    "name": "Hatice",  // user.firstName
    "surname": "Tarim",  // user.lastName
    "gsmNumber": "+905392027178",  // user.phoneNumber
    "email": "hatice@tarim.com",  // user.email
    "identityNumber": "11111111111",  // TC kimlik no (test için 11111111111)
    "registrationAddress": "Test Address, Istanbul",
    "city": "Istanbul",
    "country": "Turkey",
    "zipCode": "34000"
  }
}
```

### 2. Billing Address - ZORUNLU
```json
{
  "billingAddress": {
    "contactName": "Hatice Tarim",  // companyName veya fullName
    "city": "Istanbul",
    "country": "Turkey",
    "address": "Test Billing Address, Istanbul",
    "zipCode": "34000"
  }
}
```

### 3. Shipping Address - İSTEĞE BAĞLI (ama tavsiye edilir)
```json
{
  "shippingAddress": {
    "contactName": "Hatice Tarim",
    "city": "Istanbul",
    "country": "Turkey",
    "address": "Test Shipping Address, Istanbul",
    "zipCode": "34000"
  }
}
```

### 4. Basket Items - ZORUNLU
```json
{
  "basketItems": [
    {
      "id": "TIER_1_SPONSOR_50",  // unique identifier
      "name": "S Tier Sponsorship - 50 Codes",
      "category1": "Subscription",
      "category2": "Sponsor",
      "itemType": "VIRTUAL",  // PHYSICAL veya VIRTUAL
      "price": "4999.50"  // toplam fiyat string olarak
    }
  ]
}
```

## Tam iyzico Request Örneği

```csharp
var request = new CreateCheckoutFormInitializeRequest
{
    Locale = Locale.TR.ToString(),
    ConversationId = conversationId,
    Price = totalAmount.ToString("F2", CultureInfo.InvariantCulture),
    PaidPrice = totalAmount.ToString("F2", CultureInfo.InvariantCulture),
    Currency = Currency.TRY.ToString(),
    BasketId = $"BASKET_{transactionId}",
    PaymentGroup = PaymentGroup.SUBSCRIPTION.ToString(),
    CallbackUrl = callbackUrl,
    EnabledInstallments = new List<int> { 1 },  // Taksit yok

    // ZORUNLU: Buyer bilgileri
    Buyer = new Buyer
    {
        Id = user.Id.ToString(),
        Name = user.FirstName ?? "User",
        Surname = user.LastName ?? user.Id.ToString(),
        GsmNumber = user.PhoneNumber ?? "+905350000000",
        Email = user.Email ?? "user@example.com",
        IdentityNumber = "11111111111",  // Test için
        RegistrationAddress = sponsorProfile?.CompanyAddress ?? "Istanbul, Turkey",
        City = "Istanbul",
        Country = "Turkey",
        ZipCode = "34000"
    },

    // ZORUNLU: Billing Address
    BillingAddress = new Address
    {
        ContactName = sponsorProfile?.CompanyName ?? user.FullName,
        City = "Istanbul",
        Country = "Turkey",
        Description = sponsorProfile?.CompanyAddress ?? "Istanbul, Turkey",
        ZipCode = "34000"
    },

    // İSTEĞE BAĞLI: Shipping Address
    ShippingAddress = new Address
    {
        ContactName = sponsorProfile?.CompanyName ?? user.FullName,
        City = "Istanbul",
        Country = "Turkey",
        Description = sponsorProfile?.CompanyAddress ?? "Istanbul, Turkey",
        ZipCode = "34000"
    },

    // ZORUNLU: Basket Items
    BasketItems = new List<BasketItem>
    {
        new BasketItem
        {
            Id = $"TIER_{subscriptionTierId}_SPONSOR_{quantity}",
            Name = $"{tierName} Tier Sponsorship - {quantity} Codes",
            Category1 = "Subscription",
            Category2 = "Sponsor",
            ItemType = BasketItemType.VIRTUAL.ToString(),
            Price = totalAmount.ToString("F2", CultureInfo.InvariantCulture)
        }
    }
};

var checkoutFormInitialize = CheckoutFormInitialize.Create(request, options);
```

## Test Bilgileri (Sandbox)

### Test Kredi Kartları
- **Başarılı**: 5528790000000008 | 12/2030 | 123
- **3D Secure**: 5528790000000008 (SMS kodu: 123456)
- **Başarısız**: 5406670000000009

### Test TC Kimlik No
- `11111111111` (11 adet 1)

### Test Telefon
- `+905350000000`

## Kontrol Listesi

- [ ] `buyer` objesi eklenmiş ve tüm zorunlu alanlar dolu
- [ ] `billingAddress` objesi eklenmiş
- [ ] `shippingAddress` objesi eklenmiş (opsiyonel ama tavsiye)
- [ ] `basketItems` array'i eklenmiş ve en az 1 item var
- [ ] `price` ve `paidPrice` aynı değerde (taksit yok)
- [ ] `currency` = "TRY"
- [ ] `callbackUrl` = "ziraai://payment-callback?token={paymentToken}"
- [ ] `locale` = "tr"

## Sonraki Adımlar

1. Backend developer bu düzeltmeleri yapsın
2. Staging ortamında test edin
3. Mobile'dan tekrar ödeme başlatın
4. iyzico sayfası hata vermeden açılmalı
5. Test kartı ile ödeme tamamlayın
6. Callback doğru çalışıyor mu kontrol edin

## İlgili Dosyalar

- Backend: `Business/Services/PaymentService.cs` (muhtemelen)
- Backend: `WebAPI/Controllers/PaymentController.cs`
- Mobile: `lib/features/payment/services/payment_service.dart` (Mobile tarafı doğru)
- Mobile: `lib/features/payment/presentation/screens/payment_webview_screen.dart` (Callback handler hazır)
