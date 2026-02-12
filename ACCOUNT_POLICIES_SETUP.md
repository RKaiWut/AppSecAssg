# Account Policies & Recovery - Setup Guide

## ? What Was Implemented

### 1. Account Lockout Recovery
- **Automatic recovery after 1 minute** ?
- Configured in `Program.cs`:
  ```csharp
  options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(1);
  options.Lockout.MaxFailedAccessAttempts = 2;
  ```

### 2. Password History (No Reuse)
- **Prevents reusing last 2 passwords** ?
- Implemented in: `ChangePassword.cshtml.cs`, `ResetPassword.cshtml.cs`
- Stores password hashes in `PasswordHistory` table

### 3. Change Password
- **Page**: `/ChangePassword`
- Features:
  - Validates current password
  - Checks minimum password age (1 minute)
  - Checks password history (last 2 passwords)
  - Updates password expiry date

### 4. Reset Password (Email Link)
- **Pages**: `/ForgotPassword`, `/ResetPassword`
- Features:
  - Sends email with reset link
  - Token expires after default time (1 hour)
  - Checks password history
  - Clears account lockout on successful reset

### 5. Password Age Policies
- **Minimum Password Age**: 1 minute (cannot change too soon)
- **Maximum Password Age**: 90 days (must change after expiry)
- Tracked in `Member` model:
  - `LastPasswordChangedDate`
  - `PasswordExpiryDate`

---

## ?? Required NuGet Packages

Add these packages to your project:

```bash
# For Email (MailKit - recommended by Microsoft)
dotnet add package MailKit

# For SMS (Twilio - most popular)
dotnet add package Twilio
```

Or add manually to `BookwormsOnline.csproj`:

```xml
<ItemGroup>
  <PackageReference Include="MailKit" Version="4.3.0" />
  <PackageReference Include="Twilio" Version="7.0.0" />
</ItemGroup>
```

---

## ?? Configuration Required

### 1. Email Setup (Gmail Example)

Add to `appsettings.json`:

```json
{
  "EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": 587,
    "SenderEmail": "your-email@gmail.com",
    "SenderName": "Bookworms Online",
    "Username": "your-email@gmail.com",
    "Password": "your-app-specific-password"
  }
}
```

#### How to Get Gmail App Password:

1. **Enable 2-Step Verification** on your Google Account
2. Go to: https://myaccount.google.com/apppasswords
3. Select "Mail" and your device
4. Copy the 16-character password
5. Use this password in `appsettings.json` (NOT your regular Gmail password)

#### Alternative Email Providers:

**Outlook/Office 365:**
```json
{
  "SmtpServer": "smtp-mail.outlook.com",
  "SmtpPort": 587,
  "SenderEmail": "your-email@outlook.com",
  "Username": "your-email@outlook.com",
  "Password": "your-password"
}
```

**SendGrid (Recommended for Production):**
```json
{
  "SmtpServer": "smtp.sendgrid.net",
  "SmtpPort": 587,
  "SenderEmail": "noreply@yourdomain.com",
  "Username": "apikey",
  "Password": "YOUR_SENDGRID_API_KEY"
}
```

---

### 2. SMS Setup (Twilio - Optional)

Add to `appsettings.json`:

```json
{
  "TwilioSettings": {
    "AccountSid": "YOUR_TWILIO_ACCOUNT_SID",
    "AuthToken": "YOUR_TWILIO_AUTH_TOKEN",
    "FromNumber": "+1234567890"
  }
}
```

#### How to Get Twilio Credentials:

1. Sign up at: https://www.twilio.com/
2. Get free trial credits ($15)
3. Go to Console Dashboard
4. Copy:
   - **Account SID**
   - **Auth Token**
5. Get a phone number from Twilio
6. Use that as `FromNumber`

**Note:** SMS is optional. You can implement it later when needed.

---

## ??? Database Migration

Run this to create the `PasswordHistory` table:

```bash
dotnet ef migrations add AddPasswordPolicies -p BookwormsOnline/BookwormsOnline.csproj

dotnet ef database update -p BookwormsOnline/BookwormsOnline.csproj
```

---

## ?? Security Features Summary

| Feature | Status | Details |
|---------|--------|---------|
| **Automatic Lockout Recovery** | ? | 1 minute after lockout |
| **Password History** | ? | Prevents reusing last 2 passwords |
| **Change Password** | ? | With minimum age check (1 min) |
| **Reset Password (Email)** | ? | Token-based, 1 hour expiry |
| **Reset Password (SMS)** | ?? | Service ready, configure Twilio |
| **Min Password Age** | ? | 1 minute |
| **Max Password Age** | ? | 90 days |
| **Password Complexity** | ? | 12+ chars, upper, lower, digit, special |

---

## ?? Password Policy Constants

You can adjust these in the respective files:

### Change Password (`ChangePassword.cshtml.cs`):
```csharp
private const int MinPasswordAgeMinutes = 1;  // Change to 1440 for 1 day
private const int MaxPasswordAgeDays = 90;     // Change to 60 for 60 days
private const int PasswordHistoryCount = 2;    // Change to 5 for last 5 passwords
```

### Reset Password (`ResetPassword.cshtml.cs`):
```csharp
private const int PasswordHistoryCount = 2;
private const int MaxPasswordAgeDays = 90;
```

---

## ?? Testing Guide

### Test Change Password:
1. Login to application
2. Go to `/ChangePassword`
3. Try changing password immediately ? Should fail (min age)
4. Try using old password ? Should fail (password history)
5. Use a new password ? Should succeed

### Test Forgot Password:
1. Go to `/Login`
2. Click "Forgot your password?"
3. Enter email address
4. Check email inbox for reset link
5. Click link, enter new password
6. Login with new password

### Test Account Recovery:
1. Fail login 2 times ? Account locked
2. Wait 1 minute
3. Try logging in again ? Should work (automatic recovery)

### Test Password Expiry:
1. Manually set `PasswordExpiryDate` in database to past date:
   ```sql
   UPDATE AspNetUsers 
   SET PasswordExpiryDate = DATEADD(day, -1, GETDATE())
   WHERE Email = 'test@example.com'
   ```
2. Go to `/ChangePassword`
3. Should see expiry warning

---

## ?? Usage Examples

### In Your Code - Send Custom Emails:

```csharp
// Inject IEmailService
public MyPageModel(IEmailService emailService)
{
    _emailService = emailService;
}

// Send custom email
await _emailService.SendEmailAsync(
    "user@example.com",
    "Welcome to Bookworms",
    "<h1>Welcome!</h1><p>Thanks for joining us.</p>"
);
```

### In Your Code - Send SMS:

```csharp
// Inject ISmsService
public MyPageModel(ISmsService smsService)
{
    _smsService = smsService;
}

// Send custom SMS
await _smsService.SendSmsAsync(
    "+1234567890",
    "Your verification code is: 123456"
);
```

---

## ?? Files Created

### Models:
- `PasswordHistory.cs` - Stores password history

### Services:
- `IEmailService.cs` - Email service interface
- `EmailService.cs` - Email implementation (MailKit)
- `ISmsService.cs` - SMS service interface
- `SmsService.cs` - SMS implementation (Twilio)

### Pages:
- `ChangePassword.cshtml` + `.cs` - Change password page
- `ForgotPassword.cshtml` + `.cs` - Request password reset
- `ForgotPasswordConfirmation.cshtml` + `.cs` - Confirmation page
- `ResetPassword.cshtml` + `.cs` - Reset password with token
- `ResetPasswordConfirmation.cshtml` + `.cs` - Success page

### ViewModels:
- `ChangePassword.cs` - Change password model
- `ForgotPassword.cs` - Forgot password model
- `ResetPassword.cs` - Reset password model

---

## ?? Production Deployment Checklist

- [ ] Install MailKit package
- [ ] Install Twilio package (if using SMS)
- [ ] Configure email settings in `appsettings.json`
- [ ] Get Gmail app password or use SendGrid
- [ ] Run database migration for PasswordHistory table
- [ ] Test email sending
- [ ] Adjust password age constants (90 days is recommended)
- [ ] Consider increasing min password age to 24 hours
- [ ] Consider increasing password history to 5-10 passwords
- [ ] Set up email monitoring/logging
- [ ] Configure Twilio for SMS (optional)

---

## ?? For Production - Recommended Changes

```csharp
// In ChangePassword.cshtml.cs and ResetPassword.cshtml.cs

// Change from 1 minute to 24 hours
private const int MinPasswordAgeMinutes = 1440;  // 24 hours

// Change from 90 days to 60-90 days (adjust as needed)
private const int MaxPasswordAgeDays = 90;

// Change from 2 to 5-10 passwords
private const int PasswordHistoryCount = 5;
```

---

## ?? You're Done!

All account policies and recovery features are implemented. Configure email settings and you're ready to go!

**Next Steps:**
1. Add email configuration to `appsettings.json`
2. Get Gmail app password
3. Run database migration
4. Test the features
5. Deploy! ??
