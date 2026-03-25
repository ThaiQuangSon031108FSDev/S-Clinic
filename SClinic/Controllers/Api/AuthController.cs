using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SClinic.Data;
using SClinic.Helpers;
using SClinic.Models;
using SClinic.Models.DTOs;

namespace SClinic.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class AuthController(ApplicationDbContext db, IConfiguration config) : ControllerBase
{
    /// <summary>
    /// POST api/auth/login — validates credentials, returns JWT token.
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var account = await db.Accounts
            .Include(a => a.Role)
            .FirstOrDefaultAsync(a => a.Email == request.Email && a.IsActive);

        if (account is null || !BCrypt.Net.BCrypt.Verify(request.Password, account.PasswordHash))
            return Unauthorized(new { message = "Invalid email or password." });

        var token = JwtHelper.GenerateToken(account, config);
        var expiresAt = DateTime.UtcNow.AddHours(double.Parse(config["Jwt:ExpiryHours"] ?? "8"));

        return Ok(new LoginResponse(
            Token: token,
            Email: account.Email,
            Role: account.Role.RoleName,
            AccountId: account.AccountId,
            ExpiresAt: expiresAt
        ));
    }

    /// <summary>
    /// POST api/auth/register — self-registration for patients only.
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterPatientRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var emailExists = await db.Accounts.AnyAsync(a => a.Email == request.Email);
        if (emailExists) return Conflict(new { message = "Email is already registered." });

        var phoneExists = await db.Patients.AnyAsync(p => p.Phone == request.Phone);
        if (phoneExists) return Conflict(new { message = "Phone number is already registered." });

        var account = new Account
        {
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            RoleId = 5, // Patient role
            IsActive = true
        };
        db.Accounts.Add(account);
        await db.SaveChangesAsync();

        var patient = new Patient
        {
            AccountId = account.AccountId,
            FullName = request.FullName,
            Phone = request.Phone,
            DateOfBirth = request.DateOfBirth
        };
        db.Patients.Add(patient);
        await db.SaveChangesAsync();

        return Ok(new { message = "Registration successful.", accountId = account.AccountId });
    }
}
