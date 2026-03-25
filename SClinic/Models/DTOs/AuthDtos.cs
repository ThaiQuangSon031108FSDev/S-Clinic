namespace SClinic.Models.DTOs;

public record LoginRequest(string Email, string Password);

public record LoginResponse(
    string Token,
    string Email,
    string Role,
    int AccountId,
    DateTime ExpiresAt
);

public record RegisterPatientRequest(
    string FullName,
    string Phone,
    string Email,
    string Password,
    DateOnly? DateOfBirth
);
