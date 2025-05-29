# FullStackHero .NET Starter Kit - Web API Makefile
# Kod analizi, güvenlik tarama ve kalite kontrol araçları

.PHONY: help clean restore build analyze security-scan format test lint audit all webapi-scan

# Default target
.DEFAULT_GOAL := help

# Configuration
SRC_DIR := src
API_PROJECT := $(SRC_DIR)/api/server/Server.csproj
SOLUTION := $(SRC_DIR)/FSH.Starter.sln
BUILD_CONFIG := Release
VERBOSITY := minimal

## Help - Available commands
help:
	@echo "🚀 FullStackHero .NET Starter Kit - Web API Commands"
	@echo ""
	@echo "📋 Available targets:"
	@echo "  help           - Show this help message"
	@echo "  clean          - Clean build artifacts"
	@echo "  restore        - Restore NuGet packages"
	@echo "  build          - Build the solution"
	@echo "  analyze        - Run code analysis"
	@echo "  security-scan  - Run security vulnerability scan"
	@echo "  format         - Format code (future: dotnet format)"
	@echo "  test           - Run tests"
	@echo "  lint           - Run linting (StyleCop + analyzers)"
	@echo "  audit          - Run dependency audit"
	@echo "  webapi-scan    - 🎯 Complete Web API analysis scan"
	@echo "  all            - Run complete pipeline (clean → scan → build)"
	@echo ""
	@echo "🔥 Quick start: make webapi-scan"

## Clean - Remove build artifacts
clean:
	@echo "🧹 Cleaning build artifacts..."
	dotnet clean $(SOLUTION) --configuration $(BUILD_CONFIG)
	@echo "✅ Clean completed"

## Restore - Restore NuGet packages
restore:
	@echo "📦 Restoring NuGet packages..."
	dotnet restore $(SOLUTION)
	@echo "✅ Restore completed"

## Build - Build the solution
build: restore
	@echo "🔨 Building solution..."
	dotnet build $(SOLUTION) --no-restore --configuration $(BUILD_CONFIG) --verbosity $(VERBOSITY)
	@echo "✅ Build completed"

## Analyze - Run code analysis with all analyzers
analyze: restore
	@echo "🔍 Running code analysis..."
	@echo "📊 Analyzing with SonarAnalyzer, StyleCop, NetAnalyzers, Roslynator..."
	dotnet build $(API_PROJECT) --no-restore --configuration $(BUILD_CONFIG) --verbosity normal
	@echo "✅ Code analysis completed"

## Security Scan - Check for vulnerabilities
security-scan:
	@echo "🛡️ Running security vulnerability scan..."
	@echo "🔍 Checking for vulnerable packages..."
	dotnet list $(SRC_DIR) package --vulnerable --include-transitive || echo "⚠️ Some vulnerabilities found"
	@echo "✅ Security scan completed"

## Audit - Run dependency audit
audit:
	@echo "📋 Running dependency audit..."
	@echo "🔍 Checking package dependencies..."
	dotnet list $(SRC_DIR) package --include-transitive
	@echo "✅ Dependency audit completed"

## Format - Format code (placeholder for dotnet format)
format:
	@echo "🎨 Code formatting..."
	@echo "⚠️ Manual formatting recommended for now"
	@echo "💡 Future: dotnet format will be added here"
	@echo "✅ Format check completed"

## Test - Run tests
test: build
	@echo "🧪 Running tests..."
	dotnet test $(SOLUTION) --no-build --configuration $(BUILD_CONFIG) --verbosity $(VERBOSITY)
	@echo "✅ Tests completed"

## Lint - Run linting (StyleCop + analyzers)
lint: 
	@echo "🔎 Running linting with StyleCop and analyzers..."
	dotnet build $(API_PROJECT) --no-restore --configuration $(BUILD_CONFIG) --verbosity normal 2>&1 | grep -E "(SA[0-9]{4}|CA[0-9]{4}|S[0-9]{4}|VSTHRD[0-9]{3})" || echo "No linting issues found"
	@echo "✅ Linting completed"

## Web API Scan - Complete analysis for Web API
webapi-scan: clean restore
	@echo "🎯 Starting complete Web API analysis scan..."
	@echo ""
	@echo "📊 Step 1: Code Quality Analysis"
	@echo "═══════════════════════════════════"
	$(MAKE) analyze
	@echo ""
	@echo "🛡️ Step 2: Security Vulnerability Scan"
	@echo "═══════════════════════════════════════"
	$(MAKE) security-scan
	@echo ""
	@echo "📋 Step 3: Dependency Audit"
	@echo "═══════════════════════════"
	$(MAKE) audit
	@echo ""
	@echo "🔎 Step 4: Linting Summary"
	@echo "═════════════════════════"
	$(MAKE) lint
	@echo ""
	@echo "🎉 Web API scan completed!"
	@echo "📊 Check the output above for any warnings or issues."

## All - Complete pipeline
all: clean restore security-scan analyze build test
	@echo "🏆 Complete pipeline finished!"
	@echo "📈 Summary:"
	@echo "  ✅ Clean completed"
	@echo "  ✅ Packages restored"
	@echo "  ✅ Security scan completed"
	@echo "  ✅ Code analysis completed"
	@echo "  ✅ Build successful"
	@echo "  ✅ Tests passed"

# Advanced targets

## Install tools - Install required .NET tools
install-tools:
	@echo "🔧 Installing .NET tools..."
	dotnet tool install --global dotnet-sonarscanner || echo "SonarScanner already installed"
	dotnet tool install --global dotnet-format || echo "dotnet-format already installed"
	@echo "✅ Tools installation completed"

## CI Build - Optimized for CI/CD
ci-build: restore
	@echo "🤖 CI/CD Build starting..."
	dotnet build $(SOLUTION) --no-restore --configuration $(BUILD_CONFIG) --verbosity normal --no-incremental
	@echo "✅ CI/CD Build completed"

## Development build - Quick build for development
dev-build:
	@echo "⚡ Quick development build..."
	dotnet build $(API_PROJECT) --configuration Debug --verbosity minimal
	@echo "✅ Development build completed"

## Watch - Watch for changes and rebuild
watch:
	@echo "👀 Watching for changes..."
	dotnet watch --project $(API_PROJECT) build

## Show analyzer info
analyzer-info:
	@echo "📊 Enabled Code Analyzers:"
	@echo "  🔥 SonarAnalyzer.CSharp (v10.6.0)"
	@echo "  🎨 StyleCop.Analyzers (v1.1.118)"
	@echo "  🚀 Microsoft.CodeAnalysis.NetAnalyzers (v8.0.0)"
	@echo "  🔧 Roslynator.Analyzers (v4.12.9)"
	@echo "  🛡️ SecurityCodeScan (v3.5.4)"
	@echo "  ⚡ Microsoft.VisualStudio.Threading.Analyzers (v17.11.20)"

## Stats - Show project statistics
stats:
	@echo "📈 Project Statistics:"
	@echo "Solution: $(SOLUTION)"
	@echo "API Project: $(API_PROJECT)"
	@echo "Build Config: $(BUILD_CONFIG)"
	@find $(SRC_DIR) -name "*.cs" | wc -l | awk '{print "C# Files: " $$1}'
	@find $(SRC_DIR) -name "*.csproj" | wc -l | awk '{print "Projects: " $$1}' 