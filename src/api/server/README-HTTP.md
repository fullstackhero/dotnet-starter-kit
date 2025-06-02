# HTTP API Test Configuration Guide

## 📁 File Structure

```
src/api/server/
├── Server.http         # Main configuration & quick start
├── public.http         # Public endpoints (no auth required)
├── base_user.http      # Regular user endpoints  
├── admin.http          # Admin endpoints
└── README-HTTP.md      # This guide
```

## 🚀 Quick Start

### 1. Start the API Server
```bash
cd src/api/server
dotnet run
```
Server will start at: `https://localhost:7000`

### 2. Test Health Check
Open `Server.http` and run the health check:
```http
GET https://localhost:7000/api/v1/auth/test
```

### 3. Login and Get Tokens
Open `public.http` and run:
- **Admin Login**: Get admin token for `admin.http`
- **Regular User Login**: Get user token for `base_user.http`

### 4. Update Token Variables
Copy JWT tokens from login responses and update:
- `@admin_token` in `admin.http`
- `@base_user_token` in `base_user.http`

## 🔐 Authentication Setup

### Method 1: Manual Token Update (Recommended for Development)
1. Run login request in `public.http`
2. Copy `token` from response
3. Update variable in respective HTTP file:
```http
@admin_token = eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### Method 2: Environment Variables (Recommended for CI/CD)
Set environment variables:
```bash
export ADMIN_TOKEN="your_admin_jwt_token"
export BASE_USER_TOKEN="your_user_jwt_token"
```

Then uncomment in HTTP files:
```http
@admin_token = {{$dotenv ADMIN_TOKEN}}
```

### Method 3: Response References (Advanced)
Use response from previous requests:
```http
@admin_token = {{adminLogin.response.body.token}}
```

## 👤 Default Users

### Admin User
- **Email**: `admin@system.com`
- **Password**: `Admin123!`
- **Roles**: `admin`
- **Access**: All endpoints

### Test Users (Create via public.http)
Register new users for testing different scenarios.

## 📋 Endpoint Categories

### Public Endpoints (`public.http`)
- ✅ Registration with MERNİS verification
- ✅ Login (admin & user)
- ✅ Password reset flow
- ✅ Token refresh
- ✅ Health check

### Base User Endpoints (`base_user.http`)
- ✅ Profile management (limited fields)
- ✅ Email/phone update (separate secure endpoints)
- ✅ Password change
- ✅ Email/phone verification
- ✅ Permission checking

### Admin Endpoints (`admin.http`)
- ✅ User management (CRUD)
- ✅ Role assignment/removal
- ✅ User filtering by role
- ✅ Soft/hard delete operations
- ✅ Full profile updates (all fields)

## 🔧 Configuration Notes

### Server Configuration
```http
@Server_HostAddress = https://localhost:7000
```
Change this if your server runs on different port/host.

### SSL/HTTPS
- Development uses self-signed certificates
- Accept certificate warnings in your HTTP client
- For production, configure proper SSL certificates

### MERNİS Integration
- Currently uses mock verification for development
- Enable real MERNİS in production via configuration
- Test TCKN format: `11111111110` (development)
- **Turkish Character Support**: Automatic conversion for proper MERNİS verification
  - `i` → `İ` (Turkish capital i with dot)
  - `ı` → `I` (Turkish capital i without dot)  
  - `ç` → `Ç`, `ğ` → `Ğ`, `ö` → `Ö`, `ş` → `Ş`, `ü` → `Ü`
  - Uses Turkish culture (`tr-TR`) for accurate character conversion
  - Example: `ibrahim` becomes `İBRAHİM` for MERNİS API

## 🛡️ Security Features

### Implemented
- ✅ JWT authentication with role-based authorization
- ✅ Password strength validation
- ✅ Rate limiting for sensitive endpoints
- ✅ MERNİS identity verification
- ✅ Email enumeration protection
- ✅ Single-use password reset tokens
- ✅ Separate endpoints for email/phone updates

### Token Management
- **Expiration**: Configurable (default varies by endpoint)
- **Refresh**: Automatic refresh token rotation
- **Security**: HMAC-SHA256 signing with secure secrets

## 📱 Turkish Localization

### TCKN Validation
- Algorithm-based validation
- MERNİS government API integration
- Format: 11-digit Turkish ID number

### Phone Numbers
- Format: `05XXXXXXXXX` (Turkish mobile)
- Validation for Turkish phone number patterns

### Character Support
- Full Turkish character support (ç, ğ, ı, ö, ş, ü)
- UTF-8 encoding throughout

## 🧪 Testing Workflow

### 1. Development Testing
```
public.http → register/login → copy tokens → base_user.http/admin.http
```

### 2. Role Testing
- Test each role's access restrictions
- Verify permission boundaries
- Test role assignment/removal

### 3. Security Testing
- Test with expired tokens
- Test with invalid/malformed requests
- Test rate limiting behavior

## 🚨 Troubleshooting

### Common Issues

**"Unauthorized" Responses**
- Check if token is valid and not expired
- Ensure correct token is used for endpoint
- Verify token includes required claims

**SSL Certificate Errors**
- Accept self-signed certificate in HTTP client
- Or configure proper certificates for production

**MERNİS Verification Failures**
- Use test TCKN: `11111111110` in development
- Check if MERNİS service is enabled in configuration

**Rate Limiting**
- Wait for rate limit window to reset
- Check rate limiting configuration
- Use different user accounts for testing

### Getting Help
- Check server console logs for detailed errors
- Verify database connection and migrations
- Review API documentation in `Server.http`

## 🔄 Updates and Maintenance

### Token Refresh
Tokens expire regularly. When you get 401 responses:
1. Run login requests in `public.http`
2. Update token variables
3. Resume testing

### Version Updates
- Keep HTTP files synchronized with API changes
- Update documentation when adding new endpoints
- Test all endpoints after API updates

## 📞 Support Information

- **API Base URL**: `https://localhost:7000`
- **Database**: PostgreSQL (localhost:5433)
- **Documentation**: Inline comments in HTTP files
- **Admin Contact**: Default admin user credentials above 