# FullStackHero .NET 9 Starter Kit - Simplified Architecture 🚀

[![Code Quality & Coverage](https://github.com/FurkanHaydari/dotnet-starter-kit/actions/workflows/analyze.yml/badge.svg)](https://github.com/FurkanHaydari/dotnet-starter-kit/actions/workflows/analyze.yml)
[![Test Coverage](https://img.shields.io/badge/coverage-74%25-green)](https://github.com/FurkanHaydari/dotnet-starter-kit/actions/workflows/test-coverage.yml)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

> Clean Architecture Solution with ASP.NET Core Web API, Dapper ORM & Role-Based Authentication

A simplified .NET 9 starter kit focused on essential features with a clean, maintainable architecture. This project has been streamlined from the original FullStackHero template to provide a focused foundation for building scalable web APIs.

## 🏗️ Architecture Overview

This starter kit implements a **simplified clean architecture** with the following key design decisions:

- **Single Database**: PostgreSQL with Dapper for data access
- **No Multi-Tenancy**: Simplified for single-tenant applications  
- **Role-Based Auth**: JWT authentication with 4 predefined roles
- **Minimal Dependencies**: Focused on essential packages only
- **Direct Repository Pattern**: Simple data access without complex abstractions

## 🚀 Quick Start Guide

### Prerequisites

- .NET 9 SDK
- PostgreSQL (local or Docker)
- Visual Studio Code or JetBrains Rider

### Setup Instructions

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd dotnet-starter-kit
   ```

2. **Configure Database**
   Update connection string in `src/api/server/appsettings.Development.json`:
   ```json
   {
     "DatabaseOptions": {
       "Provider": "postgresql",
       "ConnectionString": "Server=localhost;Port=5433;Database=fsh;User Id=pgadmin;Password=pgadmin"
     }
   }
   ```

3. **Run Database Migrations**
   ```bash
   cd src/api/server
   # Migrations will run automatically on startup
   dotnet run
   ```

4. **Access the API**
   - HTTP: `http://localhost:5000`
   - HTTPS: `https://localhost:7000`
   - Swagger: `http://localhost:5000/swagger` or `https://localhost:7000/swagger`

## 🔐 Authentication System

### SMS OTP Registration Flow

The system implements a secure two-step registration process with SMS OTP verification:

1. **Registration Request** (`POST /api/v1/auth/register-request`)
   - User submits registration form with required fields
   - System validates data and stores temporarily in cache (15 minutes)
   - SMS OTP (4-digit code) is sent to provided phone number
   - No database storage until phone verification

2. **OTP Verification** (`POST /api/v1/auth/verify-registration`)
   - User submits phone number and OTP code
   - System validates OTP (3 attempts max)
   - Upon successful verification, user is created in database
   - Cache is cleared and registration is complete

#### Registration Fields
- **Ad** (First Name) - Required
- **Soyad** (Last Name) - Required
- **TC Kimlik No** (Turkish ID Number) - Required, 11 digits
- **Doğum Tarihi** (Birth Date) - Required
- **E-posta** (Email) - Required, unique
- **Telefon** (Phone) - Required, unique, Turkish format
- **Meslek** (Profession) - Required, profession ID
- **Şifre** (Password) - Required, strong password
- **Şifre Tekrar** (Confirm Password) - Required, must match
- **Contract Acceptance** - Required, must be true

#### Security Features
- Cache-based temporary storage (no DB until verified)
- 15-minute expiration for pending registrations
- 3 OTP attempt limit with lockout
- IP address and device info collection
- Phone number uniqueness validation

### Roles & Permissions

| Role | Description | Permissions |
|------|-------------|-------------|
| `admin` | System Administrator | Full access to all endpoints |
| `customer_admin` | Customer Administrator | User management within organization |
| `customer_support` | Customer Support | Read access to user data |
| `base_user` | Standard User | Profile management, password change |

### Default Admin Account

- **Email**: `admin@system.com`
- **Password**: `Admin123!`

## 📬 API Endpoints

### Public Endpoints (No Authentication Required)
```
GET  /api/v1/auth/test             - Health check
POST /api/v1/auth/register         - User registration (legacy)
POST /api/v1/auth/register-request - SMS OTP registration (step 1)
POST /api/v1/auth/verify-registration - Verify SMS OTP and complete registration (step 2)
POST /api/v1/auth/login            - User authentication
POST /api/v1/auth/token            - Generate JWT token
POST /api/v1/auth/refresh          - Refresh JWT token
POST /api/v1/auth/forgot-password  - Password reset
```

### Base User Endpoints (Any authenticated user)
```
GET  /api/v1/auth/profile          - Get current user profile
PUT  /api/v1/auth/profile          - Update profile
POST /api/v1/auth/change-password  - Change password
GET  /api/v1/auth/permissions      - Get user permissions
GET  /api/v1/auth/roles            - Get all roles
```

### Admin & Customer Admin Endpoints (admin, customer_admin roles)
```
GET    /api/v1/auth/users                    - List all users
POST   /api/v1/auth/users/register           - Register new user
PUT    /api/v1/auth/users/{id}               - Update user
DELETE /api/v1/auth/users/{id}               - Soft delete user
POST   /api/v1/auth/users/{id}/roles         - Assign role to user
DELETE /api/v1/auth/users/{id}/roles/{roleId} - Remove role from user
GET    /api/v1/auth/users/by-role/{roleId}   - List users by role
```

### Admin, Customer Admin & Support Endpoints
```
GET /api/v1/auth/users/{id}          - Get user by ID
GET /api/v1/auth/users/{id}/roles    - Get user roles
```

### Admin Only Endpoints
```
DELETE /api/v1/auth/users/{id}/hard  - Hard delete user
```

### Bootstrap Endpoint (Temporary, Anonymous Access)
```
POST /api/v1/auth/bootstrap/assign-admin/{userId} - Assign admin role
```

## 🛠️ Technology Stack

- **.NET 9** - Latest .NET framework
- **ASP.NET Core** - Web API framework
- **Dapper** - Lightweight ORM for data access
- **PostgreSQL** - Primary database
- **JWT Bearer** - Authentication tokens
- **BCrypt** - Password hashing
- **FluentValidation** - Input validation
- **Serilog** - Structured logging
- **Swagger/OpenAPI** - API documentation
- **Carter** - Minimal API endpoints
- **Memory Cache** - In-memory caching for pending registrations
- **SMS Service** - SMS OTP delivery (development mode logs to console)

## 🗄️ Database Schema

### Core Tables
- `users` - User accounts and profile information (includes Turkish fields: TC Kimlik No, address, IBAN)
- `roles` - System roles (admin, customer_admin, customer_support, base_user)
- `user_roles` - Many-to-many relationship between users and roles
- `professions` - Reference table for user professions
- **Cache Storage** - Temporary registration data (15-minute expiration)

### Default Roles
The system includes 4 predefined roles created during migration:
- `admin` - Full system access
- `customer_admin` - Customer management
- `customer_support` - Customer support functions
- `base_user` - Basic user access

## 🧪 Testing

### Using Server.http File
Use the provided `server.http` file for quick API testing:

1. **Test the API**:
   ```http
   GET http://localhost:5000/api/v1/auth/test
   ```

2. **Test SMS OTP Registration Flow**:
   ```http
   # Step 1: Request registration with SMS OTP
   POST http://localhost:5000/api/v1/auth/register-request
   Content-Type: application/json

   {
     "firstName": "Ahmet",
     "lastName": "Yılmaz",
     "tcKimlikNo": "12345678901",
     "birthDate": "1990-01-01",
     "email": "ahmet@example.com",
     "phoneNumber": "+905551234567",
     "professionId": 1,
     "password": "SecurePass123!",
     "confirmPassword": "SecurePass123!",
     "acceptContract": true
   }

   # Step 2: Verify SMS OTP (check console for OTP code)
   POST http://localhost:5000/api/v1/auth/verify-registration
   Content-Type: application/json

   {
     "phoneNumber": "+905551234567",
     "otpCode": "1234"
   }
   ```

3. **Login as admin**:
   ```http
   POST http://localhost:5000/api/v1/auth/login
   Content-Type: application/json

   {
     "email": "admin@system.com",
     "password": "Admin123!"
   }
   ```

4. **Copy the JWT token from response**

5. **Test protected endpoints**:
   ```http
   GET http://localhost:5000/api/v1/auth/profile
   Authorization: Bearer YOUR_JWT_TOKEN_HERE
   ```

### Using Swagger UI
Navigate to `http://localhost:5000/swagger` for interactive API documentation.

### Using cURL
```bash
# Test endpoint
curl -X GET "http://localhost:5000/api/v1/auth/test"

# Login
curl -X POST "http://localhost:5000/api/v1/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@system.com","password":"Admin123!"}'
```

## 🔧 Configuration

### JWT Settings
```json
{
  "JwtOptions": {
    "Key": "QsJbczCNysv/5SGh+U7sxedX8C07TPQPBdsnSDKZ/aE=",
    "TokenExpirationInMinutes": 60,
    "RefreshTokenExpirationInDays": 7
  }
}
```

### Database Options
```json
{
  "DatabaseOptions": {
    "Provider": "postgresql",
    "ConnectionString": "Server=localhost;Port=5433;Database=fsh;User Id=pgadmin;Password=pgadmin"
  }
}
```

### CORS Configuration
```json
{
  "CorsOptions": {
    "AllowedOrigins": [
      "https://localhost:7100",
      "http://localhost:7100",
      "http://localhost:5010"
    ]
  }
}
```

## 📁 Project Structure

```
src/
├── api/
│   ├── server/
│   │   ├── AuthController.cs           # Authentication endpoints
│   │   ├── DapperUserRepository.cs     # Data access layer
│   │   ├── JwtHelper.cs                # JWT token management
│   │   ├── RoleConstants.cs            # Role definitions
│   │   ├── Models/                     # Data models
│   │   └── Scripts/                    # Database migrations
│   └── framework/
│       └── Infrastructure/             # Framework components
└── aspire/                             # Aspire service defaults
```

## 🚧 Development Notes

### Adding New Endpoints
1. Add method to `AuthController.cs`
2. Implement authorization with `[Authorize(Roles = "admin,customer_admin")]`
3. Add data access methods to `DapperUserRepository.cs` if needed
4. Update `server.http` with new endpoint examples

### Database Migrations
1. Create SQL scripts in `Scripts/` folder
2. Follow naming convention: `XXX_DescriptiveName.sql`
3. Migrations run automatically on application startup
4. Current migrations:
   - `001_CreateUsersTable.sql` - User table schema
   - `002_CreateRolesAndUserRoles.sql` - Roles and user-role relationships
   - `003_CreateDefaultAdmin.sql` - Default admin user
   - `004_CreateProfessionsTable.sql` - Professions reference table
   - `005_UpdateUsersTableForProfessions.sql` - Add profession_id to users
   - `006_AddProfessionData.sql` - Insert profession data
   - `007_RemoveVerificationColumns.sql` - Remove old verification columns
   - `008_RemoveVerificationColumns.sql` - Clean up verification columns
   - `009_AddAdditionalUserFields.sql` - Add address, IBAN, IP tracking fields

### Role-Based Security
- Roles are defined in `RoleConstants.cs`
- Use `[Authorize(Roles = RoleConstants.Admin)]` for single role
- Use `[Authorize(Roles = "admin,customer_admin")]` for multiple roles
- JWT tokens include role claims for authorization
- Roles are assigned through the user management endpoints

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests for new endpoints in `server.http`
5. Submit a pull request

## 📝 Migration from Original FullStackHero

This version has been simplified from the original FullStackHero template:

### ✅ Completed Simplifications
- **Removed**: Entity Framework Core (replaced with Dapper)
- **Removed**: Multi-tenancy support
- **Removed**: Complex module system (ToDo/Catalog modules)
- **Removed**: Unnecessary packages and dependencies
- **Added**: Role-based authentication with 4 predefined roles
- **Added**: Comprehensive user management API
- **Simplified**: Database schema to essential tables only
- **Simplified**: Architecture to focus on core features

### 🎯 Current Status
- ✅ PostgreSQL with Dapper integration
- ✅ JWT authentication system
- ✅ Role-based authorization (admin, customer_admin, customer_support, base_user)
- ✅ Complete user management CRUD operations
- ✅ Database migrations system
- ✅ API documentation with Swagger
- ✅ Ready for development and testing

## 📞 Support

If you encounter any issues or have questions, please create an issue in the repository or refer to the comprehensive API documentation available at `/swagger` when the application is running.

## ⚖️ License

MIT © [fullstackhero](LICENSE)

## 🚀 Enterprise Architecture Roadmap

Bu proje şu anda **Clean Architecture** ve **DDD** prensipleriyle solid bir foundation sunarken, gelecekte enterprise-level distributed architecture'a geçiş planlanmaktadır.

### 🎯 Target Enterprise Architecture

```
┌─────────────────┐    ┌─────────────────┐
│ Client & CDN    │    │ Web App         │
│ (Browser/Mobile)│    │ (Vue/Nuxt)      │
└─────────────────┘    └─────────────────┘
           │                      │
           └──────────┬───────────┘
                      │
        ┌─────────────▼─────────────┐
        │      API Gateway          │
        │  (Auth, Rate Limit,       │
        │   Request Routing)        │
        └─────────────┬─────────────┘
                      │
        ┌─────────────▼─────────────┐
        │    Load Balancer          │
        └─────────────┬─────────────┘
                      │
    ┌─────────────────┼─────────────────┐
    │                 │                 │
┌───▼────┐    ┌──────▼──────┐    ┌────▼─────┐
│BFF/API │    │   Consumer  │    │Scheduled │
│ Layer  │    │  Services   │    │   Jobs   │
└───┬────┘    └──────┬──────┘    └────┬─────┘
    │                │                │
    └────────────────┼────────────────┘
                     │
        ┌────────────▼─────────────┐
        │     Message Queue        │
        │      (RabbitMQ)          │
        └──────────────────────────┘
                     │
    ┌────────────────┼────────────────┐
    │                │                │
┌───▼────┐    ┌──────▼──────┐    ┌────▼─────┐
│PostgreSQL    │    Redis     │    │  Consul  │
│Master/Replica│  (Cache/Pub) │    │(Discovery)│
└─────────────┘    └─────────┘    └──────────┘
```

### 🏗️ Architecture Components

#### **Frontend Layer**
- **Client & CDN (Cloudflare)**: Statik dosyaları global olarak cache'ler
- **Web App (Vue/Nuxt)**: SSR/SSG ile SEO-friendly UI

#### **Gateway & Load Balancing**
- **API Gateway**: Authentication, authorization, rate limiting, request routing
- **Load Balancer**: Traffic distribution, health checks, auto-scaling

#### **Backend Services**
- **BFF/Web API**: Anticorruption layer, service discovery, CQRS pattern
- **Consumer Services**: RabbitMQ message processing, external API integration
- **Scheduled Jobs**: Hangfire ile background job processing

#### **Data & Infrastructure**
- **PostgreSQL**: Master-replica setup (write/read separation)
- **Redis**: Hybrid cache (get/set) + Pub/Sub messaging
- **RabbitMQ**: Asynchronous message processing
- **Consul**: Service discovery ve configuration management

#### **Monitoring & Logging**
- **ELK Stack**: Centralized logging (Elasticsearch, Logstash, Kibana)
- **Health Checks**: Service durumu izleme
- **Metrics**: Performance ve usage metrics

### ⚠️ **Mevcut Auth Yapısında Güncellenecekler**

#### **🔐 1. Distributed Authentication**
```csharp
// Şu an: Monolithic JWT
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)

// Hedef: API Gateway + Service-to-Service Auth
public interface IApiGatewayAuthService 
{
    Task<AuthResult> ValidateTokenAsync(string token);
    Task<ClaimsPrincipal> ParseTokenAsync(string token);
    Task InvalidateTokenAsync(string token);
}
```

#### **🏪 2. Redis Token Store**
```csharp
// Şu an: InMemoryRefreshTokenRepository
public class InMemoryRefreshTokenRepository : IRefreshTokenRepository

// Hedef: Distributed Token Management
public interface IDistributedTokenStore
{
    Task StoreTokenAsync(string userId, TokenInfo token);
    Task<TokenInfo?> GetTokenAsync(string userId);
    Task RevokeTokenAsync(string userId);
    Task<bool> IsTokenValidAsync(string token);
}
```

#### **📡 3. Service Discovery Integration**
```csharp
// Hedef: Consul entegrasyonu
public interface IServiceDiscoveryAuthProvider
{
    Task<ServiceAuthConfig> GetAuthConfigAsync(string serviceName);
    Task RegisterServiceAsync(string serviceName, AuthRequirements auth);
}
```

#### **🔔 4. Event-Driven Authentication**
```csharp
// Hedef: Auth events için RabbitMQ
public interface IAuthEventPublisher
{
    Task PublishUserLoggedInAsync(UserLoggedInEvent @event);
    Task PublishTokenRevokedAsync(TokenRevokedEvent @event);
    Task PublishUserRoleChangedAsync(UserRoleChangedEvent @event);
}
```

### 📋 **Implementation Roadmap**

#### **Phase 1: Core Enhancements (2-3 hafta)**
- ✅ Redis-based distributed token store
- ✅ Token blacklisting mechanism
- ✅ Enhanced JWT validation
- ✅ Basic service-to-service authentication

#### **Phase 2: Gateway Integration (2-3 hafta)**
- ✅ API Gateway auth middleware
- ✅ Centralized rate limiting
- ✅ Request validation pipeline
- ✅ Load balancer health checks

#### **Phase 3: Event-Driven Architecture (2-3 hafta)**
- ✅ RabbitMQ integration
- ✅ Auth event publishing/consuming
- ✅ Audit trail implementation
- ✅ Real-time auth status updates

#### **Phase 4: Service Discovery (2-3 hafta)**
- ✅ Consul service registration
- ✅ Dynamic configuration management
- ✅ Service health monitoring
- ✅ Auto-scaling integration

#### **Phase 5: Monitoring & Security (1-2 hafta)**
- ✅ ELK Stack integration
- ✅ Authentication metrics
- ✅ Security audit logs
- ✅ Performance monitoring

### 🛠️ **Required Technology Updates**

#### **New Dependencies**
```xml
<!-- Message Queue -->
<PackageReference Include="RabbitMQ.Client" Version="6.8.1" />
<PackageReference Include="MassTransit.RabbitMQ" Version="8.1.3" />

<!-- Service Discovery -->
<PackageReference Include="Consul" Version="1.6.10.9" />
<PackageReference Include="Consul.AspNetCore" Version="1.6.10.9" />

<!-- API Gateway -->
<PackageReference Include="Ocelot" Version="23.0.0" />
<PackageReference Include="Ocelot.Provider.Consul" Version="23.0.0" />

<!-- Monitoring -->
<PackageReference Include="Serilog.Sinks.Elasticsearch" Version="10.0.0" />
<PackageReference Include="Prometheus.AspNetCore" Version="8.2.1" />
```

#### **Infrastructure Requirements**
- **Docker Compose**: Multi-service orchestration
- **PostgreSQL**: Master-replica setup
- **Redis Cluster**: High availability caching
- **RabbitMQ**: Message queue clustering
- **Consul**: Service mesh
- **ELK Stack**: Centralized logging

### 📊 **Migration Strategy**

#### **🔄 Backward Compatibility**
- Mevcut JWT token'lar migrate edilecek
- Legacy endpoint'ler deprecated olacak
- Rolling deployment strategy

#### **⚡ Performance Considerations**
- Redis connection pooling
- Database read-replica utilization
- Cache warming strategies
- Circuit breaker patterns

#### **🔒 Security Enhancements**
- Token encryption at rest
- API Gateway security headers
- Rate limiting per user/service
- Audit trail compliance

### 🎯 **Expected Benefits**

#### **Scalability**
- **Horizontal scaling**: Load balancer ile multiple instances
- **Database performance**: Read-replica separation
- **Cache efficiency**: Redis distributed caching

#### **Reliability**
- **High availability**: Service redundancy
- **Fault tolerance**: Circuit breaker patterns
- **Monitoring**: Real-time health checks

#### **Developer Experience**
- **Service discovery**: Otomatik service registration
- **Configuration management**: Centralized config via Consul
- **Debugging**: Centralized logging with correlation IDs

### 📈 **Success Metrics**

- **Response Time**: < 200ms for auth endpoints
- **Throughput**: > 10,000 requests/second
- **Availability**: 99.9% uptime
- **Scalability**: Auto-scale based on load

---

**Estimated Timeline**: **8-12 hafta** (team size'a bağlı)  
**Risk Level**: **Medium** (good planning ile manageable)  
**Investment**: **Medium** (infrastructure + development)

Bu roadmap ile mevcut solid foundation enterprise-grade distributed architecture'a dönüştürülebilir! 🚀