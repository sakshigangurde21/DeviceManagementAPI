using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.RegularExpressions;

namespace DeviceManagementAPI.Validations
{
    public class DeviceNameValidationAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            var name = value?.ToString()?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(name) || name.Equals("string", StringComparison.OrdinalIgnoreCase))
                return new ValidationResult("Device name is required.");

            if (name.Length > 100)
                return new ValidationResult("Device name cannot exceed 100 characters.");

            if (name.All(char.IsDigit))
                return new ValidationResult("Device name cannot be only numbers.");

            if (!Regex.IsMatch(name, @"^[a-zA-Z0-9 _-]+$"))
                return new ValidationResult("Only letters, numbers, spaces, hyphens, and underscores allowed.");

            return ValidationResult.Success;
        }
    }
}
