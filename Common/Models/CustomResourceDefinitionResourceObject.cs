using Common.Utils;
using k8s.Models;
using System;
using System.Text.Json;

namespace Common.Models
{
	public class CustomResourceDefinitionResourceObject : V1CustomResourceDefinition
	{
		public bool IsNamespaced
		{
			get
			{
				return Spec?.Scope?.Equals("Namespaced", StringComparison.OrdinalIgnoreCase) == true;
			}
		}

		public string LongName
		{
			get
			{
				return NameUtil.GetLongName(this.ApiGroup(), this.ApiGroupVersion(), Kind, this.Namespace(), this.Name());
			}
		}

		public static CustomResourceDefinitionResourceObject Convert(V1CustomResourceDefinition v1customResourceDefinition)
		{
			if (v1customResourceDefinition is null)
			{
				throw new ArgumentNullException(nameof(v1customResourceDefinition));
			}

			var v1customResourceDefinitionJson = JsonSerializer.Serialize(v1customResourceDefinition);
			var customResourceDefinitionResourceObject = JsonSerializer.Deserialize<CustomResourceDefinitionResourceObject>(v1customResourceDefinitionJson);

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
