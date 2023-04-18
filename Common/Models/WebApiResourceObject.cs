using Common.Utils;
using k8s.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Common.Models
{
	public class WebApiResourceObject : V1Service
	{
		public List<V1CustomResourceDefinition> CustomResourceDefinitions { get; } = new();

		public string FineControllerSpecPath => this.GetAnnotation(Constants.FineControllerSpecPath);

		public string LongName => NameUtil.GetResourceObjectLongName(ApiVersion, Kind, Metadata?.Namespace(), Metadata?.Name);

		public bool FineControllerEnable
		{
			get
			{
				var stringValue = this.GetAnnotation(Constants.FineControllerEnable);

				if (string.IsNullOrWhiteSpace(stringValue))
				{
					return default;
				}

				if (!bool.TryParse(stringValue, out var boolValue))
				{
					throw new ApplicationException($"Invalid {Constants.FineControllerEnable} '{stringValue}' (is not true|false)");
				}

				return boolValue;
			}
		}

		public bool FineControllerHttps
		{
			get
			{
				var stringValue = this.GetAnnotation(Constants.FineControllerHttps);

				if (string.IsNullOrWhiteSpace(stringValue))
				{
					return default;
				}

				if (!bool.TryParse(stringValue, out var boolValue))
				{
					throw new ApplicationException($"Invalid {Constants.FineControllerHttps} '{stringValue}' (is not true|false)");
				}

				return boolValue;
			}
		}

		public int? FineControllerPort
		{
			get
			{
				var stringValue = this.GetAnnotation(Constants.FineControllerPort);

				if (string.IsNullOrWhiteSpace(stringValue))
				{
					return default;
				}

				if (!int.TryParse(stringValue.Trim(), out var intValue))
				{
					throw new ApplicationException($"Invalid {Constants.FineControllerPort} '{stringValue}' (is not an integer)");
				}

				if (intValue < 0 || intValue > 65535)
				{
					throw new ApplicationException($"Invalid {Constants.FineControllerPort} '{stringValue}' (is not 1 - 65535)");
				}

				return intValue;
			}
		}

		public SpecFormat? FineControllerSpecFormat
		{
			get
			{
				var stringValue = this.GetAnnotation(Constants.FineControllerSpecFormat);

				if (string.IsNullOrWhiteSpace(stringValue))
				{
					return default;
				}

				if (!Enum.TryParse<SpecFormat>(stringValue, true, out var enumValue))
				{
					throw new ApplicationException($"Invalid {Constants.FineControllerSpecFormat} '{stringValue}' (is not {string.Join('|', Enum.GetNames<SpecFormat>().Select(x => x.ToLower()))})");
				}

				return enumValue;
			}
		}

		public string FineControllerWebApiUrl
		{
			get
			{
				return this.GetAnnotation(Constants.FineControllerWebApiUrl);
			}
			set
			{
				this.SetAnnotation(Constants.FineControllerWebApiUrl, value);
			}
		}

		public string FineControllerApiGroup
		{
			get
			{
				return this.GetAnnotation(Constants.FineControllerApiGroup);
			}
			set
			{
				this.SetAnnotation(Constants.FineControllerApiGroup, value);
			}
		}
	}
}
