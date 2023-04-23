using System;

namespace Common.Utils
{
	public static class NameUtil
	{
		public static string GetApiVersion(string group, string version)
		{
			if (string.IsNullOrWhiteSpace(version))
			{
				throw new ArgumentNullException(nameof(version));
			}

			if (string.IsNullOrWhiteSpace(group))
			{
				group = "-";
			}

			return $"{group.Trim()}/{version.Trim()}";
		}

		public static string GetKindLongName(string group, string version, string kind)
		{
			if (string.IsNullOrWhiteSpace(version))
			{
				throw new ArgumentNullException(nameof(version));
			}

			if (string.IsNullOrWhiteSpace(kind))
			{
				throw new ArgumentNullException(nameof(kind));
			}

			if (string.IsNullOrWhiteSpace(group))
			{
				group = "-";
			}

			return $"{GetApiVersion(group, version)}/{kind.Trim()}";
		}

		public static string GetLongName(string group, string version, string kind, string @namespace, string name)
		{
			if (string.IsNullOrWhiteSpace(version))
			{
				throw new ArgumentNullException(nameof(version));
			}

			if (string.IsNullOrWhiteSpace(kind))
			{
				throw new ArgumentNullException(nameof(kind));
			}

			if (string.IsNullOrWhiteSpace(name))
			{
				throw new ArgumentNullException(nameof(name));
			}

			if (string.IsNullOrWhiteSpace(group))
			{
				group = "-";
			}

			if (string.IsNullOrWhiteSpace(@namespace))
			{
				@namespace = "-";
			}

			return $"{GetKindLongName(group, version, kind)}/{@namespace.Trim()}/{name.Trim()}";
		}
	}
}
