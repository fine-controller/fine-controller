using RestSharp;
using System.Text.Json;

namespace Common.Utils
{
	public static class IRestResponseExtensions
	{
		public static T Deserialize<T>(this IRestResponse response)
		{
			return JsonSerializer.Deserialize<T>(response.Content);
		}
	}
}
