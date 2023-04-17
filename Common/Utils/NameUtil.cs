namespace Common.Utils
{
	public static class NameUtil
	{
		public static string GetApiVersion(string group, string version)
		{
			return $"{group}/{version}";
		}

		public static string GetResourceObjectKindLongName(string apiVersion, string kind)
		{
			return $"{apiVersion}/{kind}";
		}

		public static string GetResourceObjectKindLongName(string group, string version, string kind)
		{
			var apiVersion = GetApiVersion(group, version);
			return $"{apiVersion}/{kind}";
		}

		public static string GetResourceObjectLongName(string apiVersion, string kind, string @namespace, string shortName)
		{
			return $"{apiVersion}/{kind}/{@namespace}/{shortName}";
		}

		public static string GetResourceObjectLongName(string group, string version, string kind, string @namespace, string shortName)
		{
			var apiVersion = GetApiVersion(group, version);
			return $"{apiVersion}/{kind}/{@namespace}/{shortName}";
		}
	}
}
