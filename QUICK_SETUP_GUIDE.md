# ?? Quick Setup Guide - Account Policies & Password Recovery

## ? What's Implemented

All account policies and password recovery features are now ready to use!

### Features:
- ? **Automatic account recovery** after 1 minute of lockout
- ? **Password history** - Cannot reuse last 2 passwords
- ? **Change password** - With minimum/maximum age policies
- ? **Reset password via email** - Secure token-based reset
- ? **Password expiry** - Must change after 90 days

---

## ?? Package Requirements

**Already Installed:**
- ? MailKit (for email sending)

**Installation command (if needed):**
```bash
dotnet add package MailKit
```

---

## ?? Email Configuration (REQUIRED)

### Step 1: Add Email Settings to `appsettings.json`

Add this section to your `appsettings.json` file:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "your-existing-connection-string"
  },
  "EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": 587,
    "SenderEmail": "your-email@gmail.com",
    "SenderName": "Bookworms Online",
    "Username": "your-email@gmail.com",
    "Password": "your-app-password-here"
  }
}
```

### Step 2: Get Gmail App Password

**Important:** You need an **App Password**, NOT your regular Gmail password!

#### How to Get It:

1. **Enable 2-Step Verification** on your Google Account
   - Go to: https://myaccount.google.com/security
   - Click "2-Step Verification"
   - Follow the setup

2. **Generate App Password**
   - Go to: https://myaccount.google.com/apppasswords
   - Select "Mail" and your device
   - Click "Generate"
   - Copy the 16-character password (e.g., `abcd efgh ijkl mnop`)

3. **Use in appsettings.json**
   ```json
   "Password": "abcdefghijklmnop"  // No spaces
   ```

### Alternative: Use Outlook/Office 365

```json
{
  "EmailSettings": {
    "SmtpServer": "smtp-mail.outlook.com",
    "SmtpPort": 587,
    "SenderEmail": "your-email@outlook.com",
    "SenderName": "Bookworms Online",
    "Username": "your-email@outlook.com",
    "Password": "your-outlook-password"
  }
}
```

---

## ??? Database Migration

Run this command to create the `PasswordHistory` table:

```bash
# Create migration
dotnet ef migrations add AddPasswordHistory --project BookwormsOnline

# Apply to database
dotnet ef database update --project BookwormsOnline
```

Or if you prefer PowerShell in Visual Studio:
```powershell
Add-Migration AddPasswordHistory
Update-Database
```

---

## ?? Testing the Features

### Test 1: Automatic Account Recovery
1. Login with wrong password 2 times ? Account locked
2. Wait 1 minute
3. Login with correct password ? ? Should work (auto-recovery)

### Test 2: Change Password
1. Login to your account
2. Click "Change Password" button on home page
3. Enter current password and new password
4. Try changing again immediately ? ? Should fail (min age: 1 min)
5. Try using the same password ? ? Should fail (password history)

### Test 3: Forgot Password (Email Reset)
1. Go to Login page
2. Click "Forgot your password?"
3. Enter your email
4. Check your email inbox
5. Click the reset link
6. Enter new password
7. Login with new password ? ? Should work

### Test 4: Password Expiry
The password will expire after 90 days. To test this:

**Manually trigger expiry** (for testing):
```sql
-- Run in SQL Server Management Studio
UPDATE AspNetUsers 
SET PasswordExpiryDate = DATEADD(day, -1, GETDATE())
WHERE Email = 'your@email.com'
```

Then go to Change Password page ? Should see expiry warning

---

## ?? New Pages Available

| Page | URL | Purpose |
|------|-----|---------|
| **Change Password** | `/ChangePassword` | Users change their own password |
| **Forgot Password** | `/ForgotPassword` | Request password reset link |
| **Reset Password** | `/ResetPassword?userId=...&token=...` | Reset via email link |

---

## ?? Customizing Password Policies

You can adjust these settings in the code:

### In `ChangePassword.cshtml.cs`:

```csharp
// Line ~20-22
private const int MinPasswordAgeMinutes = 1;    // Change to 1440 for 1 day
private const int MaxPasswordAgeDays = 90;       // Change to 60 for 60 days  
private const int PasswordHistoryCount = 2;      // Change to 5 for last 5 passwords
```

### In `ResetPassword.cshtml.cs`:

```csharp
// Line ~18-19
private const int PasswordHistoryCount = 2;      // Change to match ChangePassword
private const int MaxPasswordAgeDays = 90;       // Change to match ChangePassword
```

### Recommended for Production:

```csharp
private const int MinPasswordAgeMinutes = 1440;  // 24 hours
private const int MaxPasswordAgeDays = 60;       // 60 days
private const int PasswordHistoryCount = 5;      // Last 5 passwords
```

---

## ?? Password Policy Summary

| Policy | Setting | Description |
|--------|---------|-------------|
| **Minimum Length** | 12 characters | Set in Program.cs |
| **Complexity** | Upper, lower, digit, special | Set in Program.cs |
| **Minimum Age** | 1 minute | Cannot change too soon |
| **Maximum Age** | 90 days | Must change after expiry |
| **Password History** | 2 passwords | Cannot reuse recent passwords |
| **Account Lockout** | 2 attempts | Auto-recovery after 1 minute |

---

## ?? Email Templates

The system sends these emails:

### Password Reset Email
```
Subject: Password Reset Request - Bookworms Online

You requested to reset your password. Click the link below:
[Reset Password Link]

This link expires in 1 hour.
If you didn't request this, ignore this email.
```

You can customize the template in `EmailService.cs`:

```csharp
public async Task SendPasswordResetEmailAsync(string toEmail, string resetLink)
{
    var subject = "Password Reset Request - Bookworms Online";
    var message = $@"
        <h2>Password Reset Request</h2>
        <p>Click the link: <a href='{resetLink}'>Reset Password</a></p>
        <p>This link expires in 1 hour.</p>
    ";
    await SendEmailAsync(toEmail, subject, message);
}
```

---

## ?? Troubleshooting

### Email Not Sending?

**Check 1: App Password**
- Make sure you're using an App Password, not your regular password
- Verify 2-Step Verification is enabled on Gmail

**Check 2: Less Secure Apps**
- Gmail may block the connection
- Use App Password instead of "Allow less secure apps"

**Check 3: Firewall**
- Make sure port 587 is not blocked

**Check 4: SMTP Settings**
- Verify `smtp.gmail.com` and port `587` are correct
- For Outlook, use `smtp-mail.outlook.com`

**Check Logs:**
```csharp
// Logs are automatically written - check your console/logs
_logger.LogError(ex, "Failed to send email to {Email}", toEmail);
```

### Password History Not Working?

Make sure you ran the database migration:
```bash
dotnet ef database update
```

Verify table exists:
```sql
SELECT * FROM PasswordHistories
```

---

## ? Production Checklist

Before deploying to production:

- [ ] Install MailKit package
- [ ] Configure email settings in `appsettings.json`
- [ ] Get Gmail App Password (or use SendGrid/Mailgun)
- [ ] Run database migration (`dotnet ef database update`)
- [ ] Test email sending
- [ ] Test password reset flow
- [ ] Test account recovery
- [ ] Adjust password age constants (recommended: 24h min, 60-90d max)
- [ ] Consider using a professional email service (SendGrid, Mailgun)
- [ ] Set up email monitoring/logging
- [ ] Add SSL certificate for HTTPS

---

## ?? You're Ready!

All account policies are implemented. Just:
1. Add email configuration to `appsettings.json`
2. Get Gmail app password
3. Run database migration
4. Test!

**Build Status:** ? Successful  
**Features Ready:** ? All Implemented  
**Email Service:** ? Configured (needs credentials)  
**Database:** ? Needs migration  

Deploy with confidence! ??
