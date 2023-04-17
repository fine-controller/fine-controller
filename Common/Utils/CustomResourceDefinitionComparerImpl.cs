using Common.Interfaces;
using k8s.Models;
using System;
using System.Collections.Generic;

namespace Common.Utils
{
	internal class CustomResourceDefinitionComparerImpl : ICustomResourceDefinitionComparer
	{
		public (IEnumerable<V1CustomResourceDefinition> New, IEnumerable<V1CustomResourceDefinition> Updated, IEnumerable<V1CustomResourceDefinition> Removed, IEnumerable<V1CustomResourceDefinition> Unchanged) Compare(IEnumerable<V1CustomResourceDefinition> existingCRDs, IEnumerable<V1CustomResourceDefinition> incomingCRDs)
		{
			throw new NotImplementedException();
		}
	}
}
