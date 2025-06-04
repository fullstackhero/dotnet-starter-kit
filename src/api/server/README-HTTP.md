# 🌐 API Testing Suite - Enterprise HTTP Collection

> Comprehensive HTTP request collection for FSH Starter API testing with role-based organization and enterprise-grade structure.

## 📁 File Organization

### 🔧 Core Files
| File | Purpose | Authentication | Scope |
|------|---------|----------------|-------|
| `_variables.http` | **Global Variables** | None | Shared configuration, tokens, test data |
| `public.http` | **Public Endpoints** | None | Registration, login, password reset |
| `base_user.http` | **User Operations** | JWT Required | Profile management, verification |
| `admin.http` | **Admin Operations** | Admin JWT | User management, system admin |

### 📚 Documentation
| File | Purpose |
|------|---------|
| `README-HTTP.md` | This documentation |
| `Server.http` | API overview and quick start |

## 🚀 Quick Start Guide

### 1. Environment Setup
```bash
# Option A: Use _variables.http (manual token management)
# Edit _variables.http and update tokens after login

# Option B: Environment variables (CI/CD recommended)
export ADMIN_TOKEN="eyJhbGciOiJIUzI1NiIs..."
export BASE_USER_TOKEN="eyJhbGciOiJIUzI1NiIs..."
```

### 2. Authentication Flow
```
Step 1: public.http → Admin Login → Copy accessToken
Step 2: Update _variables.http → @admin_token = copied_token
Step 3: Use admin.http with admin operations
```

### 3. Testing Workflow
```
1. Health Check (public.http)
2. Register Test Users (public.http)  
3. Login as Different Roles (public.http)
4. Role-based Operations (admin.http, base_user.http)
5. Validation Testing (error scenarios)
```

## 🎯 Role-Based Testing

### 👤 Base User Testing (`base_user.http`)
**Required Token**: `base_user` role or higher
```
✅ Profile management (limited fields)
✅ Password changes
✅ Email/Phone verification
✅ Contact information updates
❌ Cannot access admin functions
❌ Cannot modify other users
```

### 👑 Admin Testing (`admin.http`) 
**Required Token**: `admin` or `customer_admin` role
```
✅ Full user management (CRUD)
✅ Role assignment/removal
✅ System administration
✅ User analytics and reporting
✅ Override all restrictions
```

### 🌐 Public Testing (`public.http`)
**No Authentication Required**
```
✅ User registration with MERNİS verification
✅ Authentication (login/token generation)
✅ Password reset workflows
✅ Health checks and API information
✅ Input validation testing
```

## 🎫 Token Management

### Automatic Token Handling
```http
# In public.http - Named requests for token capture
# @name adminLogin
POST {{Base_Auth_URL}}/login

# In admin.http - Automatic token usage
@admin_token = {{adminLogin.response.body.accessToken}}
```

### Manual Token Management
```http
# Copy from login response and paste
@admin_token = eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...

# Use in requests
Authorization: Bearer {{admin_token}}
```

### Environment Variables (Production/CI)
```bash
# Set environment variables
export ADMIN_TOKEN="your_admin_token"
export BASE_USER_TOKEN="your_user_token"

# Use in .http files
@admin_token = {{$dotenv ADMIN_TOKEN}}
```

## 🧪 Test Scenarios

### ✅ Positive Testing
- User registration with valid data
- Successful authentication flows
- Profile updates within role permissions
- Role-based access validation

### ⚠️ Negative Testing
- Invalid credentials
- Malformed input data
- Unauthorized access attempts
- Role escalation prevention

### 🔐 Security Testing
- Token expiration handling
- Permission boundary testing
- Input validation bypass attempts
- SQL injection prevention

## 📊 Advanced Features

### Variable Inheritance
```http
# Global variables in _variables.http
@Server_HostAddress = https://localhost:7000
@Base_Auth_URL = {{Server_HostAddress}}/api/v1/auth

# Used across all files
POST {{Base_Auth_URL}}/login
```

### Response Chaining
```http
# Capture response in public.http
# @name createUser
POST {{Base_Auth_URL}}/register

# Use response in admin.http
GET {{Base_Auth_URL}}/users/{{createUser.response.body.userId}}
```

### Dynamic Test Data
```http
# Parameterized requests with test data
POST {{Base_Auth_URL}}/register
{
  "email": "test-{{$randomInt}}@example.com",
  "tckn": "{{test_tckn_1}}",
  "phoneNumber": "{{test_phone_1}}"
}
```

## 🔧 Configuration Options

### Development Environment
```http
@Server_HostAddress = https://localhost:7000
# MERNİS verification disabled
# Console logging enabled
# Relaxed validation
```

### Production Environment  
```http
@Server_HostAddress = https://api.yourcompany.com
# MERNİS verification enabled
# Email service configured
# Strict validation
```

### Test Environment
```http
@Server_HostAddress = https://test-api.yourcompany.com
# Mock services
# Seeded test data
# Automated test runners
```

## 📋 Best Practices

### 🎯 Organization
- ✅ Group related requests by functionality
- ✅ Use descriptive request names
- ✅ Include clear comments and documentation
- ✅ Separate test scenarios from functional tests

### 🔐 Security
- ✅ Never commit real tokens to version control
- ✅ Use environment variables for sensitive data
- ✅ Rotate test credentials regularly
- ✅ Validate all security boundaries

### 🧪 Testing
- ✅ Test both positive and negative scenarios
- ✅ Include edge cases and boundary conditions
- ✅ Validate error responses and status codes
- ✅ Document expected behaviors

### 📝 Documentation
- ✅ Comment complex request scenarios
- ✅ Document required permissions
- ✅ Include usage examples
- ✅ Maintain up-to-date endpoint documentation

## 🚨 Troubleshooting

### Common Issues

**401 Unauthorized**
```
• Token expired (check expiration)
• Token malformed (copy-paste errors)
• Wrong token for endpoint role requirement
```

**403 Forbidden**
```
• Insufficient role permissions
• Trying admin endpoint with user token
• Role hierarchy violation
```

**400 Bad Request**
```
• Invalid request format
• Missing required fields
• Validation errors
```

**Connection Issues**
```
• Check server is running on correct port
• Verify base URL configuration
• Check for proxy/firewall issues
```

### Debug Steps
1. Verify server is running (`public.http` health check)
2. Check token validity (login again if needed)
3. Validate request format against API documentation
4. Check server logs for detailed error information

## 📞 Support

- **API Documentation**: `/swagger` when server is running
- **Health Check**: `GET /api/v1/auth/test`
- **Base URL**: `{{Server_HostAddress}}/api/v1/auth`
- **Default Admin**: `admin@system.com` / `Admin123!`

---

Built with ❤️ for enterprise-grade API testing 