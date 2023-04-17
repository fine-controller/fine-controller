using Common.Models;
using System;
using System.Collections.Generic;

namespace Common.Utils
{
	public class ControllerResourceObjectComparer : IComparer<ControllerResourceObject>
	{
		public static ControllerResourceObjectComparer Instance { get; } = new();

		public int Compare(ControllerResourceObject first, ControllerResourceObject second)
		{
			if (first is null)
			{
				throw new ArgumentNullException(nameof(first));
			}

			if (second is null)
			{
				throw new ArgumentNullException(nameof(second));
			}

			return (int)first.LongName?.CompareTo(second.LongName);
		}
	}
}
