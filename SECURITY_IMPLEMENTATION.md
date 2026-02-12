# Security Implementation Summary

## Overview
This document outlines all security features implemented in the BookwormsOnline Razor Pages application.

## ? Implemented Security Features

### 1. Multiple Login Detection & Session Management
**Location:** `Login.cshtml.cs`, `Member.cs`, `Program.cs`

- **Single Session Enforcement**: When a user logs in from a new device/browser, previous sessions are automatically invalidated
- **Session ID Tracking**: Each login generates a unique session ID stored in cookies and database
- **Security Stamp Validation**: Validates security stamp on every request to detect concurrent logins
- **Implementation Details:**
  - `Member.CurrentSessionId`: Stores current active session
  - `Member.LastLoginDate`: Tracks last login timestamp
  - `SecurityStampValidatorOptions.ValidationInterval = TimeSpan.Zero`: Validates on every request
  - Previous sessions invalidated via `UpdateSecurityStampAsync()`

### 2. Proper & Safe Logout
**Location:** `Logout.cshtml.cs`

- **Complete Session Clearing**: Clears all session data
- **Cookie Deletion**: Removes all authentication cookies (.AspNetCore.Identity.Application, MyCookieAuth, SessionId)
- **Cache Control**: Prevents browser caching with proper headers
- **Database Update**: Clears session ID from database
- **Audit Logging**: Logs logout action with timestamp
- **Redirect**: Safely redirects to login page

### 3. Audit Logging System
**Location:** `AuditLog.cs`, `AuditLogService.cs`, `IAuditLogService.cs`

**Tracked Events:**
- LOGIN_SUCCESS
- LOGIN_ATTEMPT (failed)
- LOGOUT
- REGISTRATION_SUCCESS
- REGISTRATION_ATTEMPT (failed)
- SESSION_INVALIDATED
- SESSION_INVALID
- PAGE_ACCESS

**Logged Information:**
- User ID and Email
- Action performed
- Detailed description
- IP Address
- User Agent (browser/device info)
- Timestamp (UTC)
- Success/Failure status

### 4. User Homepage with Encrypted Data Display
**Location:** `Index.cshtml`, `Index.cshtml.cs`

**Features:**
- Displays all user profile information
- Decrypts and masks credit card (shows only last 4 digits)
- Shows last login date
- Session validation on page load
- Profile photo display
- Secure HTML encoding with `@Html.DisplayFor()`

### 5. Input Validation & Sanitization

#### Server-Side Validation
**Location:** `SecurityValidationAttributes.cs`, ViewModels

**Custom Validation Attributes:**
- `[NoSqlInjection]`: Blocks SQL injection keywords (SELECT, INSERT, DROP, UNION, etc.)
- `[NoXss]`: Blocks XSS patterns (script tags, javascript:, onerror, etc.)
- `[SafeName]`: Only allows letters, spaces, hyphens, apostrophes (2-50 chars)
- `[SafeAddress]`: Only allows alphanumeric + common punctuation
- `[ValidPhoneNumber]`: 8-15 digits, optional + prefix

**Applied To:**
- Email: Required, EmailAddress, StringLength(100), NoXss, NoSqlInjection
- Password: Required, MinLength(12), Complex regex pattern
- Names: Required, SafeName, NoXss, NoSqlInjection
- Addresses: Required, SafeAddress, StringLength(200), NoXss, NoSqlInjection
- Phone: Required, Phone, ValidPhoneNumber
- Credit Card: Required, CreditCard, Regex(13-19 digits)

#### Client-Side Validation
**Location:** `Register.cshtml`, `Login.cshtml`

**Features:**
- Real-time password strength indicator
- jQuery unobtrusive validation
- Inline error messages
- Visual feedback for invalid inputs
- Required field validation before form submission

#### Input Sanitization
**Location:** `Login.cshtml.cs`, `Register.cshtml.cs`

**Method:** `SanitizeInput(string input)`
- HTML encodes all input using `HttpUtility.HtmlEncode()`
- Trims whitespace
- Applied to all user inputs before processing

### 6. Injection Attack Prevention

#### SQL Injection Prevention
**Methods:**
1. **Entity Framework Core**: Parameterized queries by default
2. **NoSqlInjection Validation**: Blocks SQL keywords at input level
3. **Input Sanitization**: HTML encoding prevents malicious code

#### XSS (Cross-Site Scripting) Prevention
**Methods:**
1. **NoXss Validation Attribute**: Blocks XSS patterns at input level
2. **HTML Encoding**: All outputs use `@Html.DisplayFor()` or automatic Razor encoding
3. **Security Headers**:
   ```
   X-Content-Type-Options: nosniff
   X-Frame-Options: DENY
   X-XSS-Protection: 1; mode=block
   Content-Security-Policy: default-src 'self'; script-src 'self' 'unsafe-inline' https://www.google.com https://www.gstatic.com; ...
   ```
4. **Input Sanitization**: `HttpUtility.HtmlEncode()` before saving to database

#### CSRF (Cross-Site Request Forgery) Prevention
**Location:** `Program.cs`, All Razor Pages

**Implementation:**
- Automatic anti-forgery tokens on all forms with `asp-antiforgery="true"`
- Configured in `builder.Services.AddAntiforgery()`:
  - HeaderName: "X-CSRF-TOKEN"
  - Secure cookies only (HTTPS)
  - SameSite: Strict
- Razor Pages automatically validates tokens on POST requests

### 7. Proper Encoding
**Location:** All Razor views, Service classes

**Implementation:**
- **Display Encoding**: `@Html.DisplayFor()` automatically encodes
- **Input Encoding**: `HttpUtility.HtmlEncode()` before database save
- **Credit Card Encryption**: `IDataProtector.Protect()` for sensitive data
- **URL Encoding**: Automatic in Razor Pages routing

### 8. Rate Limiting & Account Lockout
**Location:** `Program.cs`

**Configuration:**
```csharp
options.Lockout.AllowedForNewUsers = true;
options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(1);
options.Lockout.MaxFailedAccessAttempts = 2;
```

**Features:**
- Locks account after 2 failed login attempts
- Lockout duration: 1 minute (configurable)
- Displays remaining lockout time to user
- Audit logs all lockout events

### 9. Session Timeout
**Location:** `Program.cs`

**Configuration:**
```csharp
// Application Cookie
options.ExpireTimeSpan = TimeSpan.FromMinutes(1);
options.SlidingExpiration = false;

// Session
options.IdleTimeout = TimeSpan.FromMinutes(1);
```

**Features:**
- Automatic logout after 1 minute of inactivity
- No sliding expiration (hard timeout)
- Secure session cookies (HttpOnly, Secure, SameSite: Strict)

### 10. Password Policy
**Location:** `Program.cs`, `Register.cs`

**Requirements:**
- Minimum 12 characters (configurable to 8 in Identity options)
- Must contain uppercase letter
- Must contain lowercase letter
- Must contain digit
- Must contain special character
- Validated by regex: `^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{12,}$`

### 11. Data Protection & Encryption
**Location:** `Register.cshtml.cs`, `Index.cshtml.cs`

**Implementation:**
- Uses ASP.NET Core Data Protection API
- Encrypts credit card numbers before storage
- Decrypts only for masked display (last 4 digits)
- Secret key stored in configuration
- Protection provider: `DataProtectionProvider.Create("EncryptData")`

### 12. File Upload Security
**Location:** `Register.cshtml.cs`

**Validation:**
- File extension check (.jpg/.jpeg only)
- File size limit (5MB max)
- Unique filename generation (GUID)
- Safe directory creation
- Proper file permissions

### 13. Email Validation & Alias Prevention
**Location:** `Login.cshtml.cs`, `Register.cshtml.cs`

**Features:**
- Removes email aliases (+ sign trick): `Regex.Replace(email, @"\+.*?(?=@)", "")`
- Prevents duplicate accounts via email aliases
- Validates email format
- Checks for existing email before registration

### 14. reCAPTCHA Integration
**Location:** `Login.cshtml`, `Register.cshtml`, corresponding .cs files

**Implementation:**
- Google reCAPTCHA v3
- Validates on both login and registration
- Server-side verification
- Prevents automated attacks

## ?? Additional Security Measures

### HTTPS Enforcement
- `app.UseHttpsRedirection()` redirects HTTP to HTTPS
- Secure cookies require HTTPS

### Security Headers
```csharp
X-Content-Type-Options: nosniff
X-Frame-Options: DENY
X-XSS-Protection: 1; mode=block
Content-Security-Policy: (restrictive policy)
Cache-Control: no-store, no-cache, must-revalidate (on logout)
```

### Authentication & Authorization
- `[Authorize]` attribute on protected pages
- Automatic redirect to login for unauthorized access
- Role-based access control ready (via ASP.NET Core Identity)

## ?? Database Schema

### AuditLog Table
```csharp
- Id (int, PK)
- UserId (string, required)
- UserEmail (string, required, max 100)
- Action (string, required, max 50)
- Details (string, max 500)
- IpAddress (string, required, max 45)
- UserAgent (string, max 500)
- Timestamp (DateTime, required)
- IsSuccessful (bool)
```

### Member Extensions
```csharp
- CurrentSessionId (string, nullable)
- LastLoginDate (DateTime, nullable)
```

## ?? How to Apply Database Changes

Run the following command to update the database:
```bash
dotnet ef database update -p BookwormsOnline\BookwormsOnline.csproj
```

## ?? Configuration Required

Ensure these settings are in `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "BookwormsConnectionString": "your-connection-string"
  },
  "SecretKey": "your-secret-key-for-encryption",
  "RecaptchaSettings": {
    "SiteKey": "your-recaptcha-site-key",
    "SecretKey": "your-recaptcha-secret-key"
  }
}
```

## ??? Security Best Practices Followed

1. ? Defense in Depth (multiple layers of security)
2. ? Least Privilege (users only access what they need)
3. ? Fail Secure (errors don't expose sensitive info)
4. ? Separation of Concerns (validation, logging, business logic separated)
5. ? Input Validation (both client and server side)
6. ? Output Encoding (prevents XSS)
7. ? Parameterized Queries (prevents SQL injection)
8. ? Secure Session Management
9. ? Audit Logging (accountability)
10. ? Data Encryption (at rest)
11. ? HTTPS Only (data in transit)
12. ? Security Headers (browser-level protection)

## ?? Testing Recommendations

### Manual Testing
1. Test lockout after 2 failed login attempts
2. Test session expiry after 1 minute inactivity
3. Test concurrent login detection (login from 2 browsers)
4. Test proper logout (verify all cookies cleared)
5. Test SQL injection attempts (should be blocked)
6. Test XSS attempts (should be blocked)
7. Test file upload validation (try non-JPG files)
8. Check audit log entries in database

### Automated Testing
- Unit tests for validation attributes
- Integration tests for login/logout flows
- Security scanning with OWASP ZAP or similar tools

## ?? References

- OWASP Top 10: https://owasp.org/www-project-top-ten/
- ASP.NET Core Security: https://docs.microsoft.com/en-us/aspnet/core/security/
- Data Protection API: https://docs.microsoft.com/en-us/aspnet/core/security/data-protection/
