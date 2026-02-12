using BookwormsOnline.Model;
using BookwormsOnline.Services;
using BookwormsOnline.Middleware;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// --------------------------------------------------
// Configuration
// --------------------------------------------------

builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

// --------------------------------------------------
// Database
// --------------------------------------------------

builder.Services.AddDbContext<BookwormsDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// --------------------------------------------------
// Razor Pages + Global CSRF Protection
// --------------------------------------------------

builder.Services.AddRazorPages(options =>
{
    // Automatically validate antiforgery tokens on POST/PUT/DELETE
    options.Conventions.ConfigureFilter(
        new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute());
});

// Add response caching
builder.Services.AddResponseCaching();

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

// --------------------------------------------------
// Identity Configuration
// --------------------------------------------------

builder.Services.AddIdentity<Member, IdentityRole>(options =>
{
    // Lockout settings
    options.Lockout.AllowedForNewUsers = true;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(1);
    options.Lockout.MaxFailedAccessAttempts = 2;

    // Password policy
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 12;

    // User settings
    options.User.RequireUniqueEmail = true;

    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<BookwormsDbContext>()
.AddDefaultTokenProviders();

// Validate security stamp frequently
builder.Services.Configure<SecurityStampValidatorOptions>(options =>
{
    options.ValidationInterval = TimeSpan.FromMinutes(1);
});

// Configure Identity cookie
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "MyCookieAuth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;

    options.LoginPath = "/Login";
    options.LogoutPath = "/Logout";
    options.AccessDeniedPath = "/Error";

    options.ExpireTimeSpan = TimeSpan.FromMinutes(1);
    options.SlidingExpiration = true; // Reset timeout on each request
});

// --------------------------------------------------
// Session
// --------------------------------------------------

builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(1);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

// --------------------------------------------------
// Additional Services
// --------------------------------------------------

builder.Services.AddDataProtection();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IEmailService, EmailService>();

// --------------------------------------------------
// Build App
// --------------------------------------------------

var app = builder.Build();

// --------------------------------------------------
// Middleware Pipeline
// --------------------------------------------------

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseStatusCodePagesWithRedirects("/errors/{0}");

// Disable caching for dynamic pages
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
    context.Response.Headers.Append("Pragma", "no-cache");
    context.Response.Headers.Append("Expires", "0");
    await next();
});

app.UseRouting();

app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

// Custom middleware for 2FA enforcement
app.UseMiddleware<Require2FAMiddleware>();
// Custom middleware for session timeout and password expiration
app.UseMiddleware<SessionTimeoutMiddleware>();
app.UseMiddleware<PasswordExpirationMiddleware>();

app.MapRazorPages();

app.Run();
