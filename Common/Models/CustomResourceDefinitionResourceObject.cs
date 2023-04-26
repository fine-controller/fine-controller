using Common.Utils;
using k8s.Models;
using Newtonsoft.Json;
using System;

namespace Common.Models
{
	public class CustomResourceDefinitionResourceObject : V1CustomResourceDefinition
	{
		protected static readonly JsonSerializerSettings JSON_SERIALIZER_SETTINGS = new() { ReferenceLoopHandling = ReferenceLoopHandling.Ignore, NullValueHandling = NullValueHandling.Ignore, Formatting = Formatting.None };

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
