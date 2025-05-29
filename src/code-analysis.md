# Kod Analizi Araçları

Bu proje, kod kalitesini, güvenliği ve sürdürülebilirliği sağlamak için çeşitli kod analizi araçları kullanmaktadır.

## 📊 Entegre Edilen Araçlar

### 1. **SonarAnalyzer.CSharp**
- **Amaç**: Kapsamlı kod kalitesi ve güvenlik analizi
- **Özellikler**: 
  - Code smell tespiti
  - Güvenlik açığı analizi
  - Bug tespiti
  - Teknik borç ölçümü

### 2. **StyleCop.Analyzers**
- **Amaç**: Kod stil tutarlılığı
- **Özellikler**:
  - Kod formatlama kuralları
  - İsimlendirme kuralları
  - Dokümantasyon kuralları
- **Yapılandırma**: `stylecop.json`

### 3. **Microsoft.CodeAnalysis.NetAnalyzers**
- **Amaç**: Microsoft'un resmi analiz kuralları
- **Özellikler**:
  - Performance analizi
  - API kullanım kuralları
  - Best practice kontrolü

### 4. **Microsoft.AspNetCore.Analyzers**
- **Amaç**: ASP.NET Core özel analiz
- **Özellikler**:
  - Web API best practices
  - Routing analizi
  - Middleware kullanım kontrolleri

### 5. **Roslynator.Analyzers**
- **Amaç**: Gelişmiş kod analizi ve öneriler
- **Özellikler**:
  - Kod basitleştirme önerileri
  - Performance iyileştirmeleri
  - Modern C# özellik kullanımı

### 6. **Security.CodeScan.VS2019**
- **Amaç**: Güvenlik açığı tespiti
- **Özellikler**:
  - SQL Injection tespiti
  - XSS vulnerability kontrolü
  - Path Traversal analizi
  - Open Redirect kontrolü

## 🚀 Kullanım

### Lokal Geliştirme
```bash
# Kod analizi ile build
dotnet build --configuration Release

# Vulnerability kontrolü
dotnet list package --vulnerable --include-transitive

# Specific analyzer ile build
dotnet build --verbosity diagnostic
```

### CI/CD Pipeline
Proje GitHub Actions ile otomatik kod analizi yapmaktadır:
- Her push ve PR'da kod analizi çalışır
- SonarCloud entegrasyonu
- Güvenlik açığı raporları

## ⚙️ Yapılandırma

### EditorConfig (`.editorconfig`)
- Kod stil kuralları
- Analyzer severity seviyeleri
- Proje bazında özelleştirmeler

### StyleCop (`stylecop.json`)
- Dokümantasyon kuralları
- İsimlendirme kuralları
- Kod organizasyon kuralları

### Global Suppressions (`GlobalSuppressions.cs`)
- False positive suppressions
- Proje bazında istisna kuralları

## 📈 Metrics & Reports

### SonarCloud Dashboard
- Code coverage
- Maintainability rating
- Reliability rating
- Security rating
- Technical debt

### GitHub Actions Reports
- Build status
- Analyzer warnings/errors
- Vulnerability reports

## 🛠️ Özelleştirme

### Analyzer Kurallarını Devre Dışı Bırakma
`.editorconfig` dosyasında:
```ini
dotnet_diagnostic.SA1600.severity = none  # Dokümantasyon kuralını kapat
dotnet_diagnostic.CA1062.severity = suggestion  # Severity seviyesini değiştir
```

### Yeni Analyzer Ekleme
`Directory.Packages.props` dosyasına:
```xml
<PackageVersion Include="YeniAnalyzer" Version="1.0.0" />
```

`Server.csproj` dosyasına:
```xml
<PackageReference Include="YeniAnalyzer">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
</PackageReference>
```

## 🔍 Best Practices

1. **Düzenli Analiz**: Her commit öncesi `dotnet build` çalıştırın
2. **Warning'leri Çözün**: Warning'leri error olarak ele alın
3. **Code Review**: Analyzer önerilerini code review'da tartışın
4. **Metric Takibi**: SonarCloud dashboard'unu düzenli kontrol edin
5. **Güvenlik**: Vulnerability raporlarını hemen çözün

## 📊 Analyzer Severity Seviyeleri

- **Error**: Build'i durdurur
- **Warning**: Build devam eder ama uyarı verir
- **Suggestion**: IDE'de öneri olarak gösterir
- **Silent**: Sadece refactoring sırasında aktif
- **None**: Tamamen devre dışı

## 🚨 Kritik Güvenlik Kuralları

Bu kurallar `warning` seviyesinde tutulmalıdır:
- `SCS0005`: Weak random generator
- `SCS0018`: Potential Path Traversal
- `SCS0026`: SQL Injection
- `SCS0027`: Open Redirect
- `SCS0029`: XSS vulnerability 