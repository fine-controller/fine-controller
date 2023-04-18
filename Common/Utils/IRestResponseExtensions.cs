using Newtonsoft.Json;
using RestSharp;

namespace Common.Utils
{
	public static class IRestResponseExtensions
	{
		public  static T Deserialize<T>(this IRestResponse response)
		{
			return JsonConvert.DeserializeObject<T>(response.Content);
		}
	}
}
