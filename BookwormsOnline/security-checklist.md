# Web Application Security Checklist

## Registration and User Data Management
- [Y] Implement successful saving of member info into the database - Register.cshtml.cs
- [Y] Check for duplicate email addresses and handle appropriately - UserManager in Register.cshtml.cs
- [Y] Implement strong password requirements:
  - [Y] Minimum 12 characters - MinimumLength(12)
  - [Y] Combination of lowercase, uppercase, numbers, and special characters - RegularExpression + Javascript function
  - [Y] Provide feedback on password strength - Javascript function in Register.cshtml
  - [Y] Implement both client-side and server-side password checks - RegularExpression
- [Y] Encrypt sensitive user data in the database (e.g., NRIC, credit card numbers) - ProtectedData
- [Y] Implement proper password hashing and storage - ASP.NET Identity
- [Y] Implement file upload restrictions (e.g., .docx, .pdf, or .jpg only) - jpg only

To-do:
Register correctly, save. Show db
Register duplicate email, short password, save, try xss. Show db
Code - no sql injection since no sql.

## Session Management
- [Y] Create a secure session upon successful login
- [Y] Implement session timeout
- [Partial] Route to homepage/login page after session timeout - Session timeout handling in Program.cs
- [Y] Detect and handle multiple logins from different devices/browser tabs - Session ID tracking in Member.cs

To-do:
Show cookie, show after 1 min timeout
Login, login with new incognito tab logout old

## Login/Logout Security
- [Yes] Implement proper login functionality - Login.cshtml.cs
- [Yes] Implement rate limiting (e.g., account lockout after 3 failed login attempts) - Program.cs
- [Yes] Perform proper and safe logout (clear session and redirect to login page) - Logout.cshtml.cs
- [Yes] Implement audit logging (save user activities in the database) - Custom LoggingService.cs
- [Yes] Redirect to homepage after successful login, displaying user info - Redirects to index.cshtml

Logout
Show account lockout
Show audit logs

## Anti-Bot Protection
- [Yes] Implement Google reCAPTCHA v3 service

Show recaptcha bottom right

## Input Validation and Sanitization
- [maybe] Prevent injection attacks (e.g., SQL injection) - there's no sql queries in the first place
- [yes] Implement Cross-Site Request Forgery (CSRF) protection - automatic in Razor Pages
- [yes] Prevent Cross-Site Scripting (XSS) attacks - Encoded input & Builtin Razor encoding
- [prob] Perform proper input sanitization, validation, and verification for all user inputs - HtmlEncoded + Builtin Razor encoding
- [Yes] Implement both client-side and server-side input validation - DataAnnotations + form check
- [Yes] Display error or warning messages for improper input - Yes
- [Yes] Perform proper encoding before saving data into the database - HtmlEncoded

No sql btw, csrf token btw, htmleconded btw, show code?

## Error Handling
- [Maybe] Implement graceful error handling on all pages - Should work
- [Yes] Create and display custom error pages (e.g., 404, 403) - Errors folder

try error page, work

## Software Testing and Security Analysis
- [Yes] Perform source code analysis using external tools (e.g., GitHub) - JQuery vulnerabilities, no idea how to fix just validate input
- [Dont Know] Address security vulnerabilities identified in the source code

show github, is jquery??

## Advanced Security Features
- [ ] Implement automatic account recovery after lockout period - tba
- [Yes] Enforce password history (avoid password reuse, max 2 password history)
- [Yes] Implement change password functionality
- [Yes] Implement reset password functionality (using email link or SMS)
- [Not Sure] Enforce minimum and maximum password age policies
- [] Implement Two-Factor Authentication (2FA) - tba

 forget password, try change again, wait
 change to old password, no work good

## General Security Best Practices
- [Yes] Use HTTPS for all communications - Redirection in Program.cs
- [Yes] Implement proper access controls and authorization - [Authorize] in Index.cshtml.cs
- [Yea] Keep all software and dependencies up to date
- [Maybe] Follow secure coding practices
- [No] Regularly backup and securely store user data - no thanks
- [Yes] Implement logging and monitoring for security events

show https, show program.cs
show index redirect
show auditing

## Documentation and Reporting
- [ ] Prepare a report on implemented security features
- [ ] Complete and submit the security checklist

Remember to test each security feature thoroughly and ensure they work as expected in your web application.

### Fixes
## Redirect to Index.html when session times out
## Require password change after login when password expires 