using System.ComponentModel.DataAnnotations;

namespace SClinic.Validation;

/// <summary>
/// Ensures a DateOnly or DateTime value is today or in the future.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class FutureDateOnlyAttribute : ValidationAttribute
{
    public FutureDateOnlyAttribute()
        : base("The {0} must be today or a future date.") { }

    protected override ValidationResult? IsValid(object? value, ValidationContext ctx)
    {
        if (value is null) return ValidationResult.Success;

        var today = DateOnly.FromDateTime(DateTime.Today);

        var date = value switch
        {
            DateOnly d => d,
            DateTime dt => DateOnly.FromDateTime(dt),
            _ => (DateOnly?)null
        };

        if (date is null)
            return new ValidationResult($"The {ctx.DisplayName} must be a valid date.");

        return date >= today
            ? ValidationResult.Success
            : new ValidationResult(FormatErrorMessage(ctx.DisplayName));
    }
}
