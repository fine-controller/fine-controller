using Common.Utils;
using k8s;
using k8s.Models;
using Newtonsoft.Json;
using System;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Common.Models
{
	public class ResourceObject : IKubernetesObject, IMetadata<V1ObjectMeta>
	{
		protected static readonly JsonSerializerSettings JSON_SERIALIZER_SETTINGS = new() { ReferenceLoopHandling = ReferenceLoopHandling.Ignore, NullValueHandling = NullValueHandling.Ignore, Formatting = Formatting.None };

		private readonly JsonObject _data;

		public string Kind
		{
			get
			{
				return _data[Constants.KindCamelCase]?.Deserialize<string>();
			}
			set
			{
				_data[Constants.KindCamelCase] = value;
			}
		}

		public string ApiVersion
		{
			get
			{
				return _data[Constants.ApiVersionCamelCase]?.Deserialize<string>();
			}
			set
			{
				_data[Constants.ApiVersionCamelCase] = value;
			}
		}

		public V1ObjectMeta Metadata
		{
			get
			{
				return _data[Constants.MetadataCamelCase]?.Deserialize<V1ObjectMeta>();
			}
			set
			{
				_data[Constants.MetadataCamelCase] = JsonNode.Parse(JsonConvert.SerializeObject(value, JSON_SERIALIZER_SETTINGS));
			}
		}

		public JsonObject Data
		{
			get
			{
				return _data;
			}
		}

		public string LongName
		{
			get
			{
				return NameUtil.GetLongName(this.ApiGroup(), this.ApiGroupVersion(), Kind, this.Namespace(), this.Name());
			}
		}

		public WatchEventType EventType
		{
			get
			{
				var value = _data[Constants.MetadataCamelCase]?[Constants.LabelsCamelCase]?[Constants.EventTypeDashCase]?.GetValue<string>();

				if (string.IsNullOrWhiteSpace(value))
				{
					return default;
				}

				return Enum.Parse<WatchEventType>(value);
			}
		}

		public ResourceObject(JsonObject data, WatchEventType eventType)
		{
			_data = data ?? throw new ArgumentNullException(nameof(data));
			
			_data[Constants.MetadataCamelCase] ??= new JsonObject();
			_data[Constants.MetadataCamelCase][Constants.LabelsCamelCase] ??= new JsonObject();
			_data[Constants.MetadataCamelCase][Constants.LabelsCamelCase][Constants.EventTypeDashCase] = eventType.ToString();
		}

		public bool IsNewerThan(ResourceObject otherResourceObject)
		{
			return string.Compare(this.ResourceVersion(), otherResourceObject.ResourceVersion()) > 0;
		}
	}
}
