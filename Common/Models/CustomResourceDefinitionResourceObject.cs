using Common.Utils;
using k8s.Models;
using Newtonsoft.Json;
using System;

namespace Common.Models
{
	public class CustomResourceDefinitionResourceObject : V1CustomResourceDefinition
	{
		protected static readonly JsonSerializerSettings JSON_SERIALIZER_SETTINGS = new() { ReferenceLoopHandling = ReferenceLoopHandling.Ignore, NullValueHandling = NullValueHandling.Ignore, Formatting = Formatting.None };

		public bool IsNamespaced => Spec?.Scope?.Equals("Namespaced", StringComparison.OrdinalIgnoreCase) == true;

		public string LongName => NameUtil.GetResourceObjectLongName(ApiVersion, Kind, this.Namespace(), this.Name());
		
		public bool FineController
		{
			get
			{
				var stringValue = this.GetLabel(Constants.FineController);

				if (string.IsNullOrWhiteSpace(stringValue))
				{
					return default;
				}

				if (!bool.TryParse(stringValue, out var boolValue))
				{
					throw new ApplicationException($"Invalid {Constants.FineController} '{stringValue}' (is not true|false)");
				}

				return boolValue;
			}
		}

		public string FineControllerHash
		{
			get
			{
				return this.GetAnnotation(Constants.FineControllerHash);
			}
			set
			{
				this.SetAnnotation(Constants.FineControllerHash, value);
			}
		}

		public static CustomResourceDefinitionResourceObject Convert(V1CustomResourceDefinition v1customResourceDefinition)
		{
			if (v1customResourceDefinition is null)
			{
				throw new ArgumentNullException(nameof(v1customResourceDefinition));
			}

			var v1customResourceDefinitionJson = JsonConvert.SerializeObject(v1customResourceDefinition, JSON_SERIALIZER_SETTINGS);
			var customResourceDefinitionResourceObject = JsonConvert.DeserializeObject<CustomResourceDefinitionResourceObject>(v1customResourceDefinitionJson, JSON_SERIALIZER_SETTINGS);

			if (string.IsNullOrWhiteSpace(customResourceDefinitionResourceObject.ApiVersion))
			{
				customResourceDefinitionResourceObject.ApiVersion = Constants.ApiExtensionsK8sIoV1LowerCase;
			}

			if (string.IsNullOrWhiteSpace(customResourceDefinitionResourceObject.Kind))
			{
				customResourceDefinitionResourceObject.Kind = Constants.CustomResourceDefinitionPascalCase;
			}

			return customResourceDefinitionResourceObject;
		}
	}
}
