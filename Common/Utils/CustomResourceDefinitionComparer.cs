using Common.Interfaces;
using k8s.Models;
using System;
using System.Collections.Generic;

namespace Common.Utils
{
	internal class CustomResourceDefinitionComparer : ICustomResourceDefinitionComparer
	{
		public (IEnumerable<V1CustomResourceDefinition> New, IEnumerable<V1CustomResourceDefinition> Updated, IEnumerable<V1CustomResourceDefinition> Removed, IEnumerable<V1CustomResourceDefinition> Unchanged) Compare(IEnumerable<V1CustomResourceDefinition> existing, IEnumerable<V1CustomResourceDefinition> incoming)
		{
			throw new NotImplementedException();
		}
	}
}
