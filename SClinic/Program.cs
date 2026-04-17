using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SClinic.Data;
using SClinic.Hubs;
using SClinic.Services;
using SClinic.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// ── Kestrel: tăng giới hạn body cho upload ảnh AI ──────────────────────────
builder.WebHost.ConfigureKestrel(opts =>
    opts.Limits.MaxRequestBodySize = 20 * 1024 * 1024); // 20 MB

// ── Database ───────────────────────────────────────────────────────────────
builder.Services.AddDbContext<ApplicationDbContext>(opts =>
    opts.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── HttpClient (for Gemini AI) ─────────────────────────────────────────────
builder.Services.AddHttpClient();

// ── SignalR (real-time notifications) ─────────────────────────────────────
builder.Services.AddSignalR();

// ── JWT + Cookie Authentication ────────────────────────────────────────────
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is missing from configuration.");

builder.Services.AddAuthentication(options =>
{
    // MVC default: Cookie auth (for Razor views with [Authorize])
    options.DefaultAuthenticateScheme = Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme       = Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme    = Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.LoginPath       = "/Account/Login";
    options.LogoutPath      = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan  = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
})
.AddJwtBearer(Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    options.SaveToken = true;
    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        ValidateIssuer           = true,
        ValidateAudience         = true,
        ValidateLifetime         = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer              = builder.Configuration["Jwt:Issuer"],
        ValidAudience            = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey         = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew                = TimeSpan.Zero
    };
    options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
    {
        OnMessageReceived = ctx =>
        {
            var token = ctx.Request.Cookies["sc_token"];
            if (!string.IsNullOrEmpty(token)) ctx.Token = token;
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// ── Business Services ──────────────────────────────────────────────────────
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<ITreatmentService, TreatmentService>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();

// ── OTP & Caching ───────────────────────────────────────────────────────────
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<OtpService>();
builder.Services.AddSingleton<EmailService>();
builder.Services.AddHttpContextAccessor();

// ── Swagger (Bug #15: API testing for testers) ────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title       = "S-Clinic API",
        Version     = "v1",
        Description = "API cho hệ thống quản lý phòng khám S-Clinic"
    });
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        In          = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Nhập JWT token: Bearer {token}",
        Name        = "Authorization",
        Type        = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme      = "Bearer"
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {{
        new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Reference = new Microsoft.OpenApi.Models.OpenApiReference
            { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" }
        }, []
    }});
});

// ── MVC + JSON ─────────────────────────────────────────────────────────────
builder.Services.AddControllersWithViews()
    .AddJsonOptions(opts =>
    {
        // Escape ALL non-ASCII as \uXXXX → fixes garbled Vietnamese in any browser
        // regardless of whether Browser honours Content-Type charset header.
        opts.JsonSerializerOptions.Encoder =
            System.Text.Encodings.Web.JavaScriptEncoder.Create(
                System.Text.Unicode.UnicodeRanges.BasicLatin);

        opts.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
        opts.JsonSerializerOptions.ReferenceHandler =
            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

var app = builder.Build();

// ── Middleware Pipeline ────────────────────────────────────────────────────
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

// Bug #15: Swagger UI only in Development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "S-Clinic API v1");
        c.RoutePrefix = "swagger";
    });
}

// ── Ensure UTF-8 charset on all JSON API responses (fixes garbled Vietnamese) ─
app.Use(async (ctx, next) =>
{
    ctx.Response.OnStarting(() =>
    {
        if (ctx.Response.ContentType?.Contains("application/json") == true
            && !ctx.Response.ContentType.Contains("charset"))
        {
            ctx.Response.ContentType += "; charset=utf-8";
        }
        return Task.CompletedTask;
    });
    await next();
});

// Serve static files — includes CSS, JS, and .glb 3D models from wwwroot
app.UseStaticFiles(new StaticFileOptions
{
    ServeUnknownFileTypes = true,
    DefaultContentType = "application/octet-stream"
});

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// ── SignalR Hub ───────────────────────────────────────────────────────────
app.MapHub<ClinicHub>("/clinicHub");

// ── Routes ─────────────────────────────────────────────────────────────────
app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Auto-apply EF Core migrations on startup (development only)
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();

    // Fix existing invoice dates so they aren't all crammed into one day
    await db.Database.ExecuteSqlRawAsync(@"
        UPDATE Invoices 
        SET CreatedDate = DATEADD(minute, 30, MedicalRecords.RecordDate)
        FROM Invoices 
        INNER JOIN MedicalRecords ON Invoices.RecordId = MedicalRecords.RecordId;
    ");
    
    // Seed realistic demo data for presentation
    await DbSeeder.SeedRealisticDemoData(db);

    // Seed treatment packages (idempotent — skips if already seeded)
    await DbSeeder.SeedTreatmentPackages(db);

    // Seed future schedules (today + 30 days) so there are always bookable slots
    await DbSeeder.SeedFutureSchedules(db);

    // Seed demo PatientTreatment records for test patients (idempotent)
    await DbSeeder.SeedDemoPatientTreatments(db);
}

// ── Dev Utility: Reset all passwords (Development only) ───────────────────
// GET /dev/reset-passwords  → rehashes all accounts with BCrypt("Sclinic@123")
if (app.Environment.IsDevelopment())
{
    app.MapGet("/dev/reset-passwords", async (ApplicationDbContext db) =>
    {
        const string newPassword = "Sclinic@123";
        var accounts = await db.Accounts.ToListAsync();
        foreach (var acc in accounts)
            acc.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword, workFactor: 11);
        await db.SaveChangesAsync();
        return Results.Ok(new { updated = accounts.Count, password = newPassword, message = "All passwords reset successfully." });
    });
}

// ── ONE-TIME SETUP (any environment, chỉ chạy khi DB trống) ───────────────
// GET /setup  → tạo Roles + Admin account lần đầu tiên
// ⚠️  Tự vô hiệu hoá ngay khi đã có account trong hệ thống
app.MapGet("/setup", async (ApplicationDbContext db) =>
{
    if (await db.Accounts.AnyAsync())
        return Results.Ok(new { done = false, message = "Đã khởi tạo rồi. Endpoint này đã bị vô hiệu hoá." });

    // Seed Roles
    var roleNames = new[] { "Admin", "Doctor", "Receptionist", "Cashier", "Patient" };
    foreach (var rn in roleNames)
        if (!await db.Roles.AnyAsync(r => r.RoleName == rn))
            db.Roles.Add(new SClinic.Models.Role { RoleName = rn });
    await db.SaveChangesAsync();

    // Tạo Admin account
    var adminRole = await db.Roles.FirstAsync(r => r.RoleName == "Admin");
    const string defaultPass = "Sclinic@2026!";
    db.Accounts.Add(new SClinic.Models.Account
    {
        Email        = "admin@sclinic.vn",
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(defaultPass, workFactor: 11),
        RoleId       = adminRole.RoleId,
        IsActive     = true
    });
    await db.SaveChangesAsync();

    return Results.Ok(new
    {
        done    = true,
        message = "✅ Khởi tạo thành công!",
        login   = new { email = "admin@sclinic.vn", password = defaultPass, warning = "ĐỔI MẬT KHẨU NGAY!" }
    });
});

await app.RunAsync();


