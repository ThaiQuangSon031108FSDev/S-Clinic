using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace SClinic.Validation;

/// <summary>
/// Validates Vietnamese mobile numbers: starts with 03x/05x/07x/08x/09x, exactly 10 digits.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class ValidPhoneFormatAttribute : ValidationAttribute
{
    // Vietnamese carrier prefixes (Viettel, Mobifone, Vinaphone, Gmobile, Vietnamobile)
    private static readonly Regex PhoneRegex = new(
        @"^(0[3|5|7|8|9])\d{8}$",
        RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(250)
    );

    public ValidPhoneFormatAttribute()
        : base("The {0} must be a valid Vietnamese phone number (e.g. 0912345678).") { }

    protected override ValidationResult? IsValid(object? value, ValidationContext ctx)
    {
        if (value is null or "") return ValidationResult.Success;

        var phone = value.ToString()!.Trim().Replace(" ", "").Replace("-", "");

        return PhoneRegex.IsMatch(phone)
            ? ValidationResult.Success
            : new ValidationResult(FormatErrorMessage(ctx.DisplayName));
    }
}
