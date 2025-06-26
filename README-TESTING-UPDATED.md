# 🧪 Test Infrastructure - Güncellenmiş Yapı

FSH Starter Kit için merkezi test konfigürasyonu ve kapsamlı test altyapısı.

## 📋 Proje Yapısı

```
dotnet-starter-kit/
├── testsettings.shared.json          # 🔧 Merkezi test konfigürasyonu
├── testsettings.json                 # 🔒 Hassas veriler (git'e dahil değil)
└── tests/
    ├── Shared/                       # 🔗 Ortak test utilities
    │   ├── TestDataManager.cs        # Merkezi veri yöneticisi
    │   └── FSH.Starter.Tests.Shared.csproj
    ├── FSH.Starter.Tests.Unit/       # 🔬 Unit testler
    ├── FSH.Starter.Tests.Integration/ # 🔗 Integration testler  
    └── FSH.Starter.Tests.Api/        # 🌐 API testleri
```

## 🎯 Yeni Özellikler

### ✅ Merkezi Konfigürasyon
- **Tek kaynak**: Tüm test projeleri `testsettings.shared.json` kullanır
- **Güvenli**: Hassas veriler ayrı `testsettings.json` dosyasında
- **Esnek**: Environment variables ile override mümkün

### ✅ Shared TestDataManager
- **Organize**: İşlevsellik sınıflara bölünmüş
  - `TestDataManager.Persons`: Kişi verileri
  - `TestDataManager.Users`: Test kullanıcıları
  - `TestDataManager.Api`: API request'ler
  - `TestDataManager.Validation`: Doğrulama helpers
  - `TestDataManager.Configuration`: Ayarlar

### ✅ Güvenlik
- Gerçek veriler Git'e dahil edilmez
- Test data classification
- Environment-based configuration

## 🚀 Hızlı Başlangıç

### 1. Test Ortamını Hazırlama

```bash
# Merkezi test konfigürasyonu zaten mevcut
cp testsettings.shared.json testsettings.json

# Gerçek verilerinizi ekleyin (opsiyonel)
nano testsettings.json
```

### 2. Test Çalıştırma

```bash
# Tüm testler (önerilen)
make test

# Kategorik testler
make test-unit           # Unit testler
make test-integration    # Integration testler  
make test-api           # API testleri
```

## 📊 Test Konfigürasyonu

### `testsettings.shared.json` - Genel Ayarlar

```json
{
  "TestConfiguration": {
    "Environment": "Testing",
    "UseRealData": false,        // Gerçek veri kullanılsın mı?
    "LogLevel": "Warning"
  },
  "DatabaseSettings": {
    "UseTestContainers": true,   // Docker PostgreSQL kullan
    "ConnectionString": "...",
    "Provider": "postgresql"
  },
  "MernisService": {
    "UseDevelopmentMode": true,  // MERNIS simulation
    "SimulationDelayMs": 100,
    "TestTcknFailures": ["11111111110", "00000000000"]
  }
}
```

### `testsettings.json` - Hassas Veriler (Git'e dahil değil)

```json
{
  "TestData": {
    "RealPersons": [
      {
        "Tckn": "12345678901",      // ⚠️ Gerçek TC kimlik
        "FirstName": "AHMET", 
        "LastName": "YILMAZ",
        "BirthDate": "1990-01-15",
        "PhoneNumber": "5551234567", // ⚠️ Gerçek telefon
        "Email": "test@example.com",
        "Password": "TestPass123!",
        "ExpectedMernisResult": true
      }
    ]
  }
}
```

## 🔧 TestDataManager Kullanımı

### Fake Data Üretimi

```csharp
// Türkçe sahte kişi
var person = TestDataManager.Persons.GenerateFakePerson();

// Geçerli TC kimlik
var tckn = TestDataManager.Validation.GenerateValidTCKN();

// Geçerli telefon numarası  
var phone = TestDataManager.Validation.GenerateValidPhoneNumber();

// 18 yaş altı kişi
var underAge = TestDataManager.Persons.GenerateUnder18Person();
```

### Gerçek Data (Konfigürasyondan)

```csharp
// Gerçek kişiler (MERNIS doğrulaması için)
var realPersons = TestDataManager.Persons.GetRealPersonsFromConfig();

// Test kullanıcıları
var testUsers = TestDataManager.Users.GetTestUsersFromConfig();

// Geçersiz test durumları
var invalidCases = TestDataManager.Persons.GetInvalidPersonsFromConfig();
```

### API Requests

```csharp
// Kayıt isteği
var registerRequest = TestDataManager.Api.CreateRegisterRequest(person);

// Giriş isteği
var loginRequest = TestDataManager.Api.CreateLoginRequest(email, password);

// SMS doğrulama
var verifyRequest = TestDataManager.Api.CreateVerifyRequest(phone, otpCode);
```

### Konfigürasyon Erişimi

```csharp
// Database bağlantısı
var connStr = TestDataManager.Configuration.GetConnectionString();

// Development mode kontrolü
var isDev = TestDataManager.Configuration.UseDevelopmentMode();

// Mock OTP kodu
var otpCode = TestDataManager.Configuration.GetMockOtpCode();
```

## 🧪 Test Senaryoları

### 📱 API Tests - Kapsanan Durumlar

#### ✅ Pozitif Testler
```csharp
[Fact] HealthCheck_Should_Return_Success()
[Fact] MernisTest_With_ValidData_Should_Return_Success()  
[Fact] RegisterRequest_With_ValidRealData_Should_Succeed()
[Fact] FullRegistrationFlow_With_FakeData_Should_Complete()
```

#### ❌ Negatif Testler
```csharp
[Theory] RegisterRequest_With_InvalidTckn_Should_Return_ValidationError()
[Theory] RegisterRequest_With_InvalidPhone_Should_Return_ValidationError()
[Theory] RegisterRequest_With_WeakPassword_Should_Return_ValidationError()
[Fact] RegisterRequest_With_Under18_Should_Return_ValidationError()
```

### 🔗 Integration Tests - Database

```csharp
[Fact] Database_Should_Be_Accessible()
[Fact] Database_Should_Have_Required_Tables()
[Fact] User_CRUD_Operations_Should_Work()
[Fact] Foreign_Key_Constraints_Should_Work()
[Fact] Unique_Constraints_Should_Work()
```

## 🛡️ Güvenlik ve En İyi Uygulamalar

### ⚠️ Dikkat Edilmesi Gerekenler

1. **Gerçek Veriler**: `testsettings.json` dosyasını **asla** Git'e commit etmeyin
2. **Test Ortamı**: Production veritabanında test çalıştırmayın
3. **API Keys**: Test ortamında gerçek API anahtarları kullanmayın
4. **Temizlik**: Test sonrası veritabanını temizleyin

### 🔒 Güvenli Test Practices

```json
{
  "MernisService": {
    "UseDevelopmentMode": true,       // ✅ Simulation kullan
    "RealApiKey": "ASLA_GIT_E_KOYMA" // ❌ Gerçek API key'i git'e koyma
  }
}
```

### 📋 Environment Variables Override

```bash
# CI/CD ortamında
export FSH_TEST_TestConfiguration__UseRealData=false
export FSH_TEST_MernisService__UseDevelopmentMode=true
export FSH_TEST_DatabaseSettings__ConnectionString="test_db_connection"
```

## 🎛️ Test Komutları

```bash
# Ana komutlar
make test                    # Tüm testler + test DB
make test-clean             # Test artifacts temizle
make test-db-up             # Test veritabanı başlat
make test-db-down           # Test veritabanı durdur

# Detaylı test çıktıları
make test-unit              # 🔬 Unit tests (detaylı çıktı)
make test-integration       # 🔗 Integration tests (test DB ile)
make test-api              # 🌐 API tests (gerçek senaryolar)

# Geliştirme
make test-watch            # Değişiklikleri izle ve test çalıştır
make test-db-reset         # Test DB'yi sıfırla
```

## 📈 Test Coverage ve Raporlama

```bash
# Coverage ile test çalıştır
dotnet test --collect:"XPlat Code Coverage"

# HTML rapor oluştur (reportgenerator gerekli)
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coverage" -reporttypes:Html

# Coverage sonuçları
open coverage/index.html
```

## 🐛 Troubleshooting

### Yaygın Sorunlar

**Problem**: Test konfigürasyonu bulunamıyor
```bash
# Çözüm: Dosya varlığını kontrol edin
ls testsettings.shared.json testsettings.json
```

**Problem**: Gerçek MERNIS testleri başarısız
```bash
# Çözüm: Development mode'un açık olduğunu kontrol edin
grep -r "UseDevelopmentMode" testsettings*.json
```

**Problem**: Test veritabanı bağlantı hatası
```bash
# Çözüm: Docker container'ın çalıştığını kontrol edin
docker ps | grep fsh-test-db
make test-db-up
```

**Problem**: Shared TestDataManager compile hatası
```bash
# Çözüm: Shared projesini build edin
dotnet build tests/Shared/FSH.Starter.Tests.Shared.csproj
```

## 📝 Yeni Test Ekleme

### 1. API Test Ekleme

```csharp
using FSH.Starter.Tests.Shared;

[Fact]
public async Task NewFeature_Should_Work()
{
    // Arrange
    var testData = TestDataManager.Persons.GenerateFakePerson();
    var request = TestDataManager.Api.CreateSomeRequest(testData);
    
    // Act
    var response = await PostJsonAsync("api/v1/new-feature", request);
    
    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
}
```

### 2. Integration Test Ekleme

```csharp
using FSH.Starter.Tests.Shared;

[Fact]
public async Task Database_NewTable_Should_Work()
{
    // Arrange
    var connection = TestDataManager.Configuration.GetConnectionString();
    
    // Test implementation...
}
```

### 3. Yeni Test Verisi Ekleme

`testsettings.shared.json` dosyasına ekleyin:

```json
{
  "TestData": {
    "NewTestCategory": [
      {
        "Property1": "Value1",
        "Property2": "Value2"
      }
    ]
  }
}
```

## 📊 Özet

- ✅ **Merkezi**: Tek `testsettings.shared.json` dosyası
- ✅ **Güvenli**: Hassas veriler ayrı dosyada, Git'e dahil değil  
- ✅ **Organize**: TestDataManager sınıfları ile düzenli
- ✅ **Detaylı**: Test sonuçları hangi API'yi neyle test ettiğini gösterir
- ✅ **Flexible**: Environment variables ile override
- ✅ **Scalable**: Yeni test kategorileri kolayca eklenebilir

Bu yapı sayesinde:
- Test verileri tek yerden yönetilir
- Gerçek veriler güvenli şekilde saklanır  
- Test sonuçları net ve anlaşılır
- Yeni test senaryoları kolayca eklenir
- CI/CD pipeline'ında güvenle çalışır 