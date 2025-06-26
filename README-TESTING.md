# 🧪 FSH Starter Kit - Test Guide

Bu rehber, FSH Starter Kit'in test altyapısını nasıl kullanacağınızı gösterir.

## 📋 Test Stratejisi

Bu projede 3 katmanlı test stratejisi kullanılmaktadır:

### 1. **Unit Tests** 🔬
- **Amaç**: Business logic ve domain katmanı testleri
- **Teknoloji**: xUnit, FluentAssertions
- **Konum**: `tests/FSH.Starter.Tests.Unit/`

### 2. **Integration Tests** 🔗  
- **Amaç**: Database bağlantıları ve component integration testleri
- **Teknoloji**: xUnit, TestContainers, Respawn
- **Konum**: `tests/FSH.Starter.Tests.Integration/`

### 3. **API Tests** 🌐
- **Amaç**: End-to-end API testleri gerçek verilerle
- **Teknoloji**: xUnit, HttpClient, Bogus
- **Konum**: `tests/FSH.Starter.Tests.Api/`

## 🚀 Hızlı Başlangıç

### Ön Gereksinimler
- .NET 9.0 SDK
- Docker (test database için)
- Make (opsiyonel - komut kolaylığı için)

### Test Verilerini Hazırlama

1. **Gerçek test verilerini ayarlayın:**
```bash
# Test ayarları dosyasını kopyalayın
cp tests/FSH.Starter.Tests.Api/testsettings.example.json tests/FSH.Starter.Tests.Api/testsettings.json

# testsettings.json dosyasını düzenleyin - gerçek kişi bilgilerini ekleyin
```

2. **testsettings.json örneği:**
```json
{
  "TestData": {
    "RealPersons": [
      {
        "Tckn": "12345678901",
        "FirstName": "AHMET", 
        "LastName": "YILMAZ",
        "BirthDate": "1990-01-15",
        "PhoneNumber": "5551234567",
        "Email": "test@example.com",
        "Password": "TestPass123!",
        "ProfessionId": 1,
        "ExpectedMernisResult": true
      }
    ]
  }
}
```

## 🛠️ Test Komutları

### Make ile (Önerilen)

```bash
# Tüm testleri çalıştır (test DB ile)
make test

# Sadece unit testler
make test-unit

# Sadece integration testler  
make test-integration

# API testleri (gerçek verilerle)
make test-api

# Test veritabanını başlat
make test-db-up

# Test veritabanını durdur
make test-db-down

# Test sonuçlarını temizle
make test-clean
```

### Manuel Komutlar

```bash
# Test veritabanını başlat
docker run -d --name fsh-test-db \
  -e POSTGRES_USER=testuser \
  -e POSTGRES_PASSWORD=testpass \
  -e POSTGRES_DB=fsh_test \
  -p 5434:5432 \
  postgres:15-alpine

# Unit testleri çalıştır
dotnet test tests/FSH.Starter.Tests.Unit

# Integration testleri çalıştır  
dotnet test tests/FSH.Starter.Tests.Integration

# API testleri çalıştır
dotnet test tests/FSH.Starter.Tests.Api

# Test veritabanını durdur
docker stop fsh-test-db && docker rm fsh-test-db
```

## 📊 Test Senaryoları

### API Tests - Kapsanan Senaryolar

#### ✅ Pozitif Testler
- [x] **Health Check**: API'nin çalışır durumda olması
- [x] **MERNIS Doğrulama**: Gerçek TC kimlik doğrulama
- [x] **Kayıt İşlemi**: Geçerli verilerle kullanıcı kaydı
- [x] **SMS Doğrulama**: OTP kod doğrulama
- [x] **Login**: Kayıtlı kullanıcı girişi
- [x] **Token Üretimi**: JWT token üretimi ve doğrulama

#### ❌ Negatif Testler
- [x] **Geçersiz TC Kimlik**: Hatalı format (10 hane, alfabetik karakter)
- [x] **Geçersiz Telefon**: Hatalı format (5 ile başlamayan)
- [x] **Zayıf Şifre**: Güvenlik kurallarına uymayan şifreler
- [x] **Yaş Sınırı**: 18 yaş altı kullanıcı kaydı
- [x] **Geçersiz Email**: Hatalı email formatı
- [x] **MERNIS Başarısız**: MERNİS'te bulunamayan kimlik

### Integration Tests - Kapsanan Senaryolar

#### 🗄️ Database Tests
- [x] **Bağlantı Testi**: PostgreSQL bağlantısı
- [x] **Tablo Varlığı**: Gerekli tabloların oluşturulması
- [x] **CRUD İşlemleri**: Create, Read, Update, Delete
- [x] **Foreign Key**: İlişkisel veri bütünlüğü
- [x] **Unique Constraint**: Benzersizlik kısıtlamaları

## 🎯 Test Verileri

### Sahte Veri Üretimi
Testler, `Bogus` kütüphanesi ile Türkçe sahte veriler üretir:

```csharp
// Geçerli TC kimlik üretimi
var tckn = TestDataManager.GenerateValidTCKN();

// Geçerli telefon numarası
var phone = TestDataManager.GenerateValidPhoneNumber();

// Sahte kişi verisi
var person = TestDataManager.GenerateFakePerson();
```

### Gerçek Test Verileri
- `testsettings.json` dosyasında tanımlanır
- Git'e dahil edilmez (`.gitignore`)
- Sadece development ortamında kullanılır
- MERNİS doğrulama testleri için gerekli

## 🔧 Test Konfigürasyonu

### Environment Variables
```bash
# Test ortamı ayarı
ASPNETCORE_ENVIRONMENT=Testing

# Test veritabanı bağlantısı
TEST_DATABASE_CONNECTION_STRING="Server=localhost;Port=5434;Database=fsh_test;User Id=testuser;Password=testpass;"

# MERNİS test modu
MERNIS_DEVELOPMENT_MODE=true
```

### Test Database
- **Image**: `postgres:15-alpine`
- **Port**: `5434` (5432 çakışma olmaması için)
- **Database**: `fsh_test`
- **User**: `testuser`
- **Password**: `testpass`

## 📈 Test Coverage

Kod kapsamı raporları için:

```bash
# Coverage ile test çalıştır
dotnet test --collect:"XPlat Code Coverage"

# Coverage raporu oluştur (reportgenerator gerekli)
reportgenerator -reports:"*/TestResults/*/coverage.cobertura.xml" -targetdir:"coverage" -reporttypes:Html
```

## 🚨 Test Güvenliği

### Dikkat Edilmesi Gerekenler

1. **Gerçek Veriler**: `testsettings.json` dosyasını Git'e commit etmeyin
2. **Test Ortamı**: Production veritabanında test çalıştırmayın  
3. **API Keys**: Test ortamında gerçek API anahtarları kullanmayın
4. **Temizlik**: Test sonrası veritabanını temizleyin

### Güvenli Test Praktikleri

```json
{
  "MernisService": {
    "UseDevelopmentMode": true,
    "SimulationDelayMs": 100,
    "TestTcknFailures": ["11111111110", "00000000000"]
  }
}
```

## 🐛 Troubleshooting

### Yaygın Sorunlar

**Problem**: Test veritabanı başlamıyor
```bash
# Çözüm: Port kullanımını kontrol edin
docker ps | grep 5434
sudo lsof -i :5434
```

**Problem**: MERNIS testleri başarısız
```bash
# Çözüm: Development mode'un açık olduğunu kontrol edin
# testsettings.json dosyasında:
"MernisService": {
  "UseDevelopmentMode": true
}
```

**Problem**: Test verileri bulunamıyor
```bash
# Çözüm: testsettings.json dosyasının varlığını kontrol edin
ls tests/FSH.Starter.Tests.Api/testsettings.json
```

## 📝 Test Yazma Rehberi

### Yeni API Test Ekleme

```csharp
[Fact]
public async Task YourApiEndpoint_Should_Work()
{
    LogTestStart(Output, "Your Test Name");

    // Arrange
    var testData = TestDataManager.GenerateFakePerson();

    // Act  
    var response = await PostJsonAsync("api/v1/your-endpoint", testData);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);

    LogTestEnd(Output, "Your Test Name", true);
}
```

### Test Data Yönetimi

```csharp
// Sahte veri üretimi
var fakePerson = TestDataManager.GenerateFakePerson();

// Gerçek veri kullanımı
var realPersons = TestDataManager.GetRealPersonsFromConfig();

// Özel invalid veri
var invalidPerson = TestDataManager.GenerateInvalidTcknPerson();
```

## 🤝 Katkıda Bulunma

Test yazarken aşağıdaki kurallara uyun:

1. **Açıklayıcı Test İsimleri**: `When_Given_Should` formatı
2. **Arrange-Act-Assert**: AAA pattern kullanın
3. **Test İzolasyonu**: Testler birbirinden bağımsız olmalı
4. **Cleanup**: Test sonrası temizlik yapın
5. **Logging**: Test adımlarını kaydedin

## 📚 İlgili Dökümanlar

- [xUnit Documentation](https://xunit.net/)
- [FluentAssertions](https://fluentassertions.com/)
- [TestContainers](https://testcontainers.org/)
- [Bogus](https://github.com/bchavez/Bogus)

---

**💡 İpucu**: Test yazımı hakkında sorularınız için issue açın veya [discussions](../../discussions) bölümünü kullanın. 