using Common.Utils;
using System.Collections.Generic;

namespace Common.Models
{
	public class AppData
	{
		public SortedSet<ControllerResourceObject> ControllerResourceObjects { get; } = new(ControllerResourceObjectComparer.Instance);
	}
}
