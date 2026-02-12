using BookwormsOnline.Model;
using BookwormsOnline.Services;
using BookwormsOnline.ViewModels;
using Ganss.Xss;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;

namespace BookwormsOnline.Pages
{
    public class RegisterModel : PageModel
    {

        // Configure services
        private UserManager<Member> userManager { get; }
        private readonly IAuditLogService _auditLogService;
        private readonly BookwormsDbContext _context;

        // Config Model
        [BindProperty]
        public Register RModel { get; set; }

        // Configure keys and environment
        private readonly IWebHostEnvironment _environment;
        private readonly string _recaptchaSecret;
        public string RecaptchaSiteKey;
        public string RecaptchaErrorMessage { get; set; }
        private readonly string _secretKey;

        public RegisterModel(UserManager<Member> userManager, SignInManager<Member> signInManager, RoleManager<IdentityRole> roleManager, IConfiguration configuration, IWebHostEnvironment environment, IAuditLogService auditLogService, BookwormsDbContext context)
        {
            this.userManager = userManager;
            _environment = environment;
            _recaptchaSecret = configuration["RecaptchaSettings:SecretKey"];
            RecaptchaSiteKey = configuration["RecaptchaSettings:SiteKey"];
            _secretKey = configuration["SecretKey"];
            _auditLogService = auditLogService;
            _context = context;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // 1. Get token
            var recaptchaResponse = Request.Form["g-recaptcha-response"];

            // 2. Verify for key
            if (_recaptchaSecret  == null)
            {
                RecaptchaErrorMessage = "reCAPTCHA secret key is not configured.";
                ModelState.AddModelError("", "reCAPTCHA configuration error");
                await _auditLogService.LogAsync("", RModel?.Email ?? "Unknown", "REGISTRATION_ATTEMPT", "reCAPTCHA configuration error", false);
                return Page();
            }

            // 3. Verify captcha
            if (!await VerifyRecaptcha(recaptchaResponse))
            {
                RecaptchaErrorMessage = "Please verify that you are not a robot.";
                ModelState.AddModelError("", "reCAPTCHA validation failed");
                await _auditLogService.LogAsync("", RModel?.Email ?? "Unknown", "REGISTRATION_ATTEMPT", "reCAPTCHA validation failed", false);
                return Page();
            }

            // Encrypt and sanitise
            var dataProtectionProvider = DataProtectionProvider.Create("EncryptData");
            var protector = dataProtectionProvider.CreateProtector(_secretKey);

            if (ModelState.IsValid)
            {
                // Sanitize inputs to prevent XSS (Razor is safe by default)
                RModel.FirstName = SanitizeInput(RModel.FirstName);
                RModel.LastName = SanitizeInput(RModel.LastName);
                RModel.Email = SanitizeInput(RModel.Email);
                RModel.BillingAddress = SanitizeInput(RModel.BillingAddress);
                RModel.ShippingAddress = SanitizeInput(RModel.ShippingAddress);
                RModel.MobileNo = SanitizeInput(RModel.MobileNo);

                // Check for duplicate email
                var existingUser = await userManager.FindByEmailAsync(RModel.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError("RModel.Email", "Email is already registered.");
                    await _auditLogService.LogAsync("", RModel.Email, "REGISTRATION_ATTEMPT", "Duplicate email", false);
                    return Page();
                }


                // Save uploaded photo to wwwroot/images/profiles
                string photoPath = null;
                if (RModel.Photo != null && RModel.Photo.Length > 0)
                {
                    // Validate file extension
                    var extension = Path.GetExtension(RModel.Photo.FileName).ToLowerInvariant();
                    if (extension != ".jpg" && extension != ".jpeg")
                    {
                        ModelState.AddModelError("RModel.Photo", "Only JPG files are allowed.");
                        return Page();
                    }

                    // Validate file size (max 5MB)
                    if (RModel.Photo.Length > 5 * 1024 * 1024)
                    {
                        ModelState.AddModelError("RModel.Photo", "File size cannot exceed 5MB.");
                        return Page();
                    }

                    // Create unique filename
                    var fileName = $"{Guid.NewGuid()}{extension}";
                    var uploadsFolder = Path.Combine(_environment.WebRootPath, "images", "profiles");
                    
                    // Create directory if it doesn't exist
                    Directory.CreateDirectory(uploadsFolder);

                    var filePath = Path.Combine(uploadsFolder, fileName);

                    // Save file
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await RModel.Photo.CopyToAsync(fileStream);
                    }

                    photoPath = $"/images/profiles/{fileName}";
                }

                // Prevent using same email that uses + sign
                string cleanEmail = Regex.Replace(RModel.Email, @"\+.*?(?=@)", "");

                var user = new Member()
                {
                    UserName = cleanEmail,
                    Email = RModel.Email,
                    FirstName = RModel.FirstName,
                    LastName = RModel.LastName,
                    CreditCardNo = protector.Protect(RModel.CreditCardNo), // Encrypt credit card
                    MobileNo = RModel.MobileNo,
                    BillingAddress = RModel.BillingAddress,
                    ShippingAddress = RModel.ShippingAddress,
                    Photo = photoPath,
                };

                var result = await userManager.CreateAsync(user, RModel.Password);
                if (result.Succeeded)
                {
                    // Store initial password in history to prevent reuse
                    var passwordHistory = new PasswordHistory
                    {
                        UserId = user.Id,
                        PasswordHash = user.PasswordHash,
                        CreatedDate = DateTime.UtcNow
                    };
                    _context.PasswordHistories.Add(passwordHistory);
                    await _context.SaveChangesAsync();

                    await _auditLogService.LogAsync(user.Id, user.Email, "REGISTRATION_SUCCESS", "User registered successfully", true);
                    
                    // Redirect to login page - user will setup 2FA on first login
                    return RedirectToPage("Login");
                }
                
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
                
                await _auditLogService.LogAsync("", RModel.Email, "REGISTRATION_ATTEMPT", $"Registration failed: {string.Join(", ", result.Errors.Select(e => e.Description))}", false);
            }
            return Page();
        }
        private string SanitizeInput(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            // HTML encode to prevent XSS
            return HttpUtility.HtmlEncode(input).Trim();
        }

        // ---------------------------------------
        // VERIFY GOOGLE RECAPTCHA
        // ---------------------------------------
        private async Task<bool> VerifyRecaptcha(string recaptchaResponse)
        {
            if (string.IsNullOrEmpty(recaptchaResponse))
                return false;

            using var client = new HttpClient();

            var result = await client.GetStringAsync(
                $"https://www.google.com/recaptcha/api/siteverify?secret={_recaptchaSecret}&response={recaptchaResponse}");

            var json = JsonSerializer.Deserialize<RecaptchaVerifyResponse>(result);

            return json.success;
        }

        public class RecaptchaVerifyResponse
        {
            public bool success { get; set; }
            public double score { get; set; }
            public string action { get; set; }
            public List<string> error_codes { get; set; }
        }
    }
}
