using System.ComponentModel.DataAnnotations;
using SClinic.Validation;

namespace SClinic.Models.DTOs;

public record LoginRequest(string Email, string Password);

public record LoginResponse(
    string Token,
    string Email,
    string Role,
    int AccountId,
    DateTime ExpiresAt
);

public class RegisterPatientRequest
{
    [Required] 
    public string FullName { get; set; } = string.Empty;

    [Required, ValidPhoneFormat] 
    public string Phone { get; set; } = string.Empty;

    [Required, EmailAddress] 
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(6)] 
    public string Password { get; set; } = string.Empty;

    public DateOnly? DateOfBirth { get; set; }
}
