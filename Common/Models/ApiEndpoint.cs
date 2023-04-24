using Microsoft.OpenApi.Models;
using System;
using System.Linq;

namespace Common.Models
{
	public class ApiEndpoint
	{
		public string NameLowerCase { get; }
		public string KindLowerCase { get; }
		public string PathLowerCase { get; }
		public string GroupLowerCase { get; }
		public string VersionLowerCase { get; }
		public string NamespaceLowerCase { get; }
		public OpenApiOperation Operation { get; }
		public OperationType OperationType { get; }
		public string KnownKindLowerCase { get; set; }
		public OpenApiPathItem OpenApiPathItem { get; }

		public ApiEndpoint(string path, OpenApiPathItem openApiItem, OperationType? operationType, OpenApiOperation openApiOperation, string defaultGroup)
		{
			path = path?.Trim()?.ToLower();

			if (string.IsNullOrWhiteSpace(path))
			{
				throw new ArgumentNullException(nameof(path));
			}

			if (openApiItem is null)
			{
				throw new ArgumentNullException(nameof(openApiItem));
			}

			if (operationType is null)
			{
				throw new ArgumentNullException(nameof(operationType));
			}

			if (openApiOperation is null)
			{
				throw new ArgumentNullException(nameof(openApiOperation));
			}

			defaultGroup = defaultGroup?.Trim()?.ToLower();

			if (string.IsNullOrWhiteSpace(defaultGroup))
			{
				throw new ArgumentNullException(nameof(defaultGroup));
			}

			if (defaultGroup == "-")
			{
				throw new ArgumentException($"{nameof(defaultGroup)} must not be '-'");
			}

			Operation = openApiOperation;
			OpenApiPathItem = openApiItem;
			PathLowerCase = path.ToLower();
			OperationType = operationType.Value;

			var pathLowerCaseArray = PathLowerCase.Trim('/').Split('/').Select(x => x.Trim()).ToArray();

			if (pathLowerCaseArray.Length != 5)
			{
				throw new ArgumentException("Key must be a path with 5 segments separated by /");
			}

			// group

			GroupLowerCase = pathLowerCaseArray[0];

			if (string.IsNullOrWhiteSpace(GroupLowerCase))
			{
				throw new ArgumentException("Path segment index 0 (group) is required");
			}

			if (GroupLowerCase == ".")
			{
				GroupLowerCase = defaultGroup;
			}
			
			if (GroupLowerCase == "-")
			{
				GroupLowerCase = null;
			}

			// version

			VersionLowerCase = pathLowerCaseArray[1];

			if (string.IsNullOrWhiteSpace(VersionLowerCase))
			{
				throw new ArgumentException("Path segment index 1 (version) is required");
			}

			if (!VersionLowerCase.StartsWith("v"))
			{
				throw new ArgumentException("Path segment index 1 (version) must start with 'v'");
			}
			
			if (VersionLowerCase.Length == 1)
			{
				throw new ArgumentException("Path segment index 1 (version) must have length > 1");
			}

			// kind

			KindLowerCase = pathLowerCaseArray[2];

			if (string.IsNullOrWhiteSpace(KindLowerCase))
			{
				throw new ArgumentException("Path segment index 2 (kind) is required");
			}

			if (!KindLowerCase.StartsWith(VersionLowerCase))
			{
				throw new ArgumentException("Path segment index 2 (kind) must start with the version (segment index 1)");
			}

			if (KindLowerCase.Length == VersionLowerCase.Length)
			{
				throw new ArgumentException("Path segment index 2 (kind) must start have a name after the version prefix");
			}

			// namespace

			NamespaceLowerCase = pathLowerCaseArray[3];

			if (string.IsNullOrWhiteSpace(NamespaceLowerCase))
			{
				throw new ArgumentException("Path segment index 3 (namespace) is required");
			}

			if (NamespaceLowerCase.Equals("-"))
			{
				NamespaceLowerCase = default;
			}
			else if (!NamespaceLowerCase.Equals("{namespace}"))
			{
				throw new ArgumentException("segment index 3 (namespace) must be '{namespace}' or '-'");
			}

			// name

			NameLowerCase = pathLowerCaseArray[4];

			if (string.IsNullOrWhiteSpace(NameLowerCase))
			{
				throw new ArgumentException("Path segment index 4 (name) is required");
			}
		}
	}
}
