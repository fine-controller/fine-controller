using Common.Utils;
using k8s.Models;

namespace Common.Models
{
	public class ControllerResourceObject : V1Service
	{
		public string LongName => NameUtil.GetResourceObjectLongName(ApiVersion, Kind, Metadata?.Namespace(), Metadata?.Name);
	}
}
