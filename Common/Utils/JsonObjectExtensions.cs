using Common.Models;
using k8s;
using System;
using System.Text.Json.Nodes;

namespace Common.Utils
{
	public static class JsonObjectExtensions
	{
		public static string GetResourceObjectKind(this JsonObject resourceObject)
		{
			if (resourceObject is null)
			{
				throw new ArgumentNullException(nameof(resourceObject));
			}

			return resourceObject[Constants.KindCamelCase]?.GetValue<string>() ?? string.Empty;
		}

		public static string GetResourceObjectApiVersion(this JsonObject resourceObject)
		{
			if (resourceObject is null)
			{
				throw new ArgumentNullException(nameof(resourceObject));
			}

			return resourceObject[Constants.ApiVersionCamelCase]?.GetValue<string>() ?? string.Empty;
		}

		public static void SetResourceObjectSpecificEventName(this JsonObject resourceObject, string specificEventName)
		{
			if (resourceObject is null)
			{
				throw new ArgumentNullException(nameof(resourceObject));
			}

			if (!Enum.TryParse<WatchEventType>(specificEventName, false, out var _))
			{
				throw new ArgumentException($"Invalid value '{specificEventName}'", nameof(resourceObject));
			}

			if (string.IsNullOrWhiteSpace(specificEventName))
			{
				throw new ArgumentNullException(nameof(specificEventName));
			}

			resourceObject[Constants.MetadataCamelCase] ??= new JsonObject();
			resourceObject[Constants.MetadataCamelCase][Constants.LabelsCamelCase] ??= new JsonObject();
			resourceObject[Constants.MetadataCamelCase][Constants.LabelsCamelCase][Constants.SpecificEventNameDashCase] = specificEventName;
		}

		public static string GetResourceObjectSpecificEventName(this JsonObject resourceObject)
		{
			if (resourceObject is null)
			{
				throw new ArgumentNullException(nameof(resourceObject));
			}

			return resourceObject[Constants.MetadataCamelCase]?[Constants.LabelsCamelCase]?[Constants.SpecificEventNameDashCase]?.GetValue<string>() ?? string.Empty;
		}

		public static WatchEventType GetResourceObjectSpecificEvent(this JsonObject resourceObject)
		{
			if (resourceObject is null)
			{
				throw new ArgumentNullException(nameof(resourceObject));
			}

			var specificEventName = resourceObject.GetResourceObjectSpecificEventName();

			return Enum.Parse<WatchEventType>(specificEventName);
		}

		public static string GetResourceObjectNamespace(this JsonObject resourceObject)
		{
			if (resourceObject is null)
			{
				throw new ArgumentNullException(nameof(resourceObject));
			}

			return resourceObject[Constants.MetadataCamelCase]?[Constants.NamespaceCamelCase]?.GetValue<string>() ?? string.Empty;
		}

		public static string GetResourceObjectShortName(this JsonObject resourceObject)
		{
			if (resourceObject is null)
			{
				throw new ArgumentNullException(nameof(resourceObject));
			}

			return resourceObject[Constants.MetadataCamelCase]?[Constants.NameCamelCase]?.GetValue<string>() ?? string.Empty;
		}

		public static string GetResourceObjectLongName(this JsonObject resourceObject)
		{
			if (resourceObject is null)
			{
				throw new ArgumentNullException(nameof(resourceObject));
			}

			var kind = resourceObject.GetResourceObjectKind();
			var shortName = resourceObject.GetResourceObjectShortName();
			var @namespace = resourceObject.GetResourceObjectNamespace();
			var apiVersion = resourceObject.GetResourceObjectApiVersion();
			
			return NameUtil.GetResourceObjectLongName(apiVersion, kind, @namespace, shortName);
		}
	}
}
