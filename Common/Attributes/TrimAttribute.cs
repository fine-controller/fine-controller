using System.ComponentModel.DataAnnotations;

namespace Common.Attributes
{
	public class TrimAttribute : ValidationAttribute
	{
		protected override ValidationResult IsValid(object value, ValidationContext validationContext)
		{
			if (value is null)
			{
				return ValidationResult.Success;
			}

			if (validationContext is null)
			{
				return ValidationResult.Success;
			}

			if (validationContext.ObjectType is null)
			{
				return ValidationResult.Success;
			}

			if (validationContext.ObjectInstance is null)
			{
				return ValidationResult.Success;
			}

			if (string.IsNullOrWhiteSpace(validationContext?.MemberName))
			{
				return ValidationResult.Success;
			}

			if (value is not string valueStr)
			{
				return ValidationResult.Success;
			}

			if (string.IsNullOrWhiteSpace(valueStr))
			{
				return ValidationResult.Success;
			}

			var property = validationContext.ObjectType.GetProperty(validationContext.MemberName);

			if (property is null)
			{
				return ValidationResult.Success;
			}

			if (!property.CanWrite)
			{
				return ValidationResult.Success;
			}

			property.SetValue(validationContext.ObjectInstance, valueStr.Trim());

			return ValidationResult.Success;
		}
	}
}
