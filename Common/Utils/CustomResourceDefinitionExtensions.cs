using k8s.Models;
using System;

namespace Common.Utils
{
	public static class CustomResourceDefinitionExtensions
	{
		public static string GetLongName(this V1CustomResourceDefinition customResourceDefinition)
		{
			if (customResourceDefinition is null)
			{
				throw new ArgumentNullException(nameof(customResourceDefinition));
			}

			return NameUtil.GetResourceObjectLongName(customResourceDefinition.ApiVersion, customResourceDefinition.Kind, customResourceDefinition.Namespace(), customResourceDefinition.Name());
		}
	}
}
