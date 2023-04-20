using Common.Models;
using System.Collections.Generic;
using System.Linq;

namespace Common.Utils
{
	public static class CustomResourceDefinitionResourceObjectDiffUtil
	{
		public static (IEnumerable<CustomResourceDefinitionResourceObject> New, IEnumerable<CustomResourceDefinitionResourceObject> Updated, IEnumerable<CustomResourceDefinitionResourceObject> Removed, IEnumerable<CustomResourceDefinitionResourceObject> Unchanged) GetDiff(IEnumerable<CustomResourceDefinitionResourceObject> existing, IEnumerable<CustomResourceDefinitionResourceObject> incoming)
		{
			var @new = incoming.ExceptBy(existing.Select(x => x.LongName), x => x.LongName).ToList();
			var removed = existing.ExceptBy(incoming.Select(x => x.LongName), x => x.LongName).ToList();
			var updatedAndUnchanged = incoming.ExceptBy(@new.Select(x => x.LongName), x => x.LongName).ToList();
			var unchanged = updatedAndUnchanged.IntersectBy(existing.Select(x => x.FineControllerHash), x => x.FineControllerHash).ToList();
			var updated = updatedAndUnchanged.ExceptBy(unchanged.Select(x => x.FineControllerHash), x => x.FineControllerHash).ToList();

			return (@new, updated, removed, unchanged);
		}
	}
}
