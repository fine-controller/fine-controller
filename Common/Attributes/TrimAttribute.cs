using System.ComponentModel.DataAnnotations;

namespace Common.Attributes
{
	public class TrimAttribute : ValidationAttribute
	{
		protected override ValidationResult IsValid(object value, ValidationContext validationContext)
		{
			if (value is string valueStr && valueStr is not null)
			{
				var property = validationContext.ObjectType.GetProperty(validationContext.MemberName);

				if (property.CanWrite)
				{
					property.SetValue(validationContext.ObjectInstance, valueStr.Trim());
				}
			}

			return ValidationResult.Success; // always
		}
	}
}
