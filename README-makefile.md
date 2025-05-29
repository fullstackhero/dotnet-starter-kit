# 🛠️ Makefile Commands - Web API Kod Analizi

Bu proje, Web API kod kalitesi ve güvenlik analizini kolaylaştırmak için kapsamlı Makefile komutları içerir.

## 🚀 Hızlı Başlangıç

```bash
# Yardım menüsünü görüntüle
make help

# Web API'yi tam tarama yap (önerilen)
make webapi-scan
```

## 📋 Ana Komutlar

### 🎯 Web API Taraması
```bash
make webapi-scan
```
**En kapsamlı komut** - Web API için tüm analiz adımlarını sırasıyla çalıştırır:
- ✅ Kod kalitesi analizi (SonarQube, StyleCop, vs.)
- ✅ Güvenlik açığı taraması
- ✅ Dependency audit
- ✅ Linting özeti

### 🏗️ Build Komutları
```bash
make clean          # Build artifactlarını temizle
make restore         # NuGet paketlerini geri yükle
make build           # Solution'ı build et
make dev-build       # Hızlı development build
make ci-build        # CI/CD optimized build
```

### 🔍 Analiz Komutları
```bash
make analyze         # Detaylı kod analizi
make security-scan   # Güvenlik açığı taraması
make audit           # Dependency audit
make lint            # Linting (StyleCop + analyzers)
```

### 📊 Bilgi Komutları
```bash
make analyzer-info   # Aktif analyzer'ları göster
make stats           # Proje istatistikleri
make help            # Yardım menüsü
```

### 🧪 Test Komutları
```bash
make test            # Tüm testleri çalıştır
make watch           # Dosya değişikliklerini izle
```

### 🎨 Format Komutları
```bash
make format          # Kod formatlama (gelecekte)
```

### 🔄 Pipeline Komutları
```bash
make all             # Tam pipeline (clean → scan → build → test)
```

## 📊 Analiz Sonuçları

### 🎯 Web API Scan Çıktısı
Çalıştırıldığında şu adımları gerçekleştirir:

```
🎯 Starting complete Web API analysis scan...

📊 Step 1: Code Quality Analysis
═══════════════════════════════════
✅ SonarAnalyzer.CSharp: Code smell tespiti
✅ StyleCop.Analyzers: Stil tutarlılığı
✅ Microsoft.CodeAnalysis.NetAnalyzers: Performance analizi
✅ Roslynator.Analyzers: Modern C# önerileri

🛡️ Step 2: Security Vulnerability Scan
═══════════════════════════════════════
✅ Vulnerable package kontrolü
✅ Dependency güvenlik analizi

📋 Step 3: Dependency Audit
═══════════════════════════
✅ Paket bağımlılıkları listesi
✅ Transitive dependency analizi

🔎 Step 4: Linting Summary
═════════════════════════
✅ StyleCop kuralları
✅ Code Analyzer kuralları
```

## 🛡️ Güvenlik Analizi

### Aktif Güvenlik Kuralları
- **SCS0005**: Weak random generator
- **SCS0018**: Potential Path Traversal
- **SCS0026**: SQL Injection
- **SCS0027**: Open Redirect
- **SCS0029**: XSS vulnerability
- **SCS0031**: Potential LDAP injection

### Vulnerability Scan
```bash
make security-scan
```
Güvenlik açığı tespit edilmesi durumunda:
```
⚠️ Some vulnerabilities found
```

## 📈 Kod Kalitesi Metrikleri

### Aktif Analyzer'lar
```bash
make analyzer-info
```
```
📊 Enabled Code Analyzers:
  🔥 SonarAnalyzer.CSharp (v10.6.0)
  🎨 StyleCop.Analyzers (v1.1.118)
  🚀 Microsoft.CodeAnalysis.NetAnalyzers (v8.0.0)
  🔧 Roslynator.Analyzers (v4.12.9)
  🛡️ SecurityCodeScan (v3.5.4)
  ⚡ Microsoft.VisualStudio.Threading.Analyzers (v17.11.20)
```

### Proje İstatistikleri
```bash
make stats
```
```
📈 Project Statistics:
Solution: src/FSH.Starter.sln
API Project: src/api/server/Server.csproj
Build Config: Release
C# Files: 237
Projects: 10
```

## 🔄 CI/CD Integration

### GitHub Actions Kullanımı
```yaml
- name: Run Web API Analysis
  run: make webapi-scan

- name: Run Security Scan
  run: make security-scan
```

### Local Development
```bash
# Her kod değişikliğinden sonra
make webapi-scan

# Sadece build kontrolü
make build

# Güvenlik kontrolü
make security-scan
```

## ⚙️ Konfigürasyon

### Analyzer Ayarları
- **StyleCop**: `src/api/server/stylecop.json`
- **EditorConfig**: `src/.editorconfig`
- **Global Suppressions**: `src/api/server/Properties/GlobalSuppressions.cs`

### Severity Seviyeleri
```ini
# .editorconfig'de tanımlı
dotnet_diagnostic.SA1633.severity = none
dotnet_diagnostic.SCS0026.severity = warning
dotnet_diagnostic.VSTHRD200.severity = warning
```

## 🐛 Troubleshooting

### Yaygın Sorunlar

**1. Build Hatası**
```bash
make clean
make restore
make build
```

**2. Vulnerability Uyarıları**
```bash
# Detaylı bilgi için
dotnet list src package --vulnerable --include-transitive
```

**3. Analyzer Çakışması**
```bash
# Temiz build
make clean && make build
```

### Debug Modları
```bash
# Detaylı build çıktısı
dotnet build --verbosity diagnostic

# Specific analyzer
dotnet build --verbosity normal 2>&1 | grep "SA1"
```

## 🎯 Best Practices

### Development Workflow
1. **Önceki Analiz**: `make webapi-scan`
2. **Kod Geliştirme**: Kendi kodunuzu yazın
3. **Hızlı Kontrol**: `make dev-build`
4. **Son Kontrol**: `make webapi-scan`
5. **Commit**: Git commit

### Pre-commit Hook
`.git/hooks/pre-commit`:
```bash
#!/bin/sh
make webapi-scan || exit 1
```

### Regular Maintenance
```bash
# Haftalık güvenlik kontrolü
make security-scan

# Aylık dependency audit
make audit
```

## 📚 Kaynaklar

- [SonarQube Rules](https://rules.sonarsource.com/csharp/)
- [StyleCop Documentation](https://github.com/DotNetAnalyzers/StyleCopAnalyzers)
- [.NET Code Analysis](https://docs.microsoft.com/en-us/dotnet/fundamentals/code-analysis/)
- [Security Code Scan](https://security-code-scan.github.io/)

## 🎉 Sonuç

Bu Makefile sistemi ile:
- ✅ **1-click analysis**: Tek komutla tam analiz
- ✅ **Consistent quality**: Tutarlı kod kalitesi
- ✅ **Security first**: Güvenlik odaklı yaklaşım
- ✅ **CI/CD ready**: Otomatik entegrasyon
- ✅ **Developer friendly**: Geliştirici dostu komutlar

**Recommended command**: `make webapi-scan` 🚀 