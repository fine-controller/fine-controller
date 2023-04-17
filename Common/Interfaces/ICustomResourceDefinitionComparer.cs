using k8s.Models;
using System.Collections.Generic;

namespace Common.Interfaces
{
	public interface ICustomResourceDefinitionComparer
	{
		public (IEnumerable<V1CustomResourceDefinition> New, IEnumerable<V1CustomResourceDefinition> Updated, IEnumerable<V1CustomResourceDefinition> Removed, IEnumerable<V1CustomResourceDefinition> Unchanged) Compare(IEnumerable<V1CustomResourceDefinition> existingCRDs, IEnumerable<V1CustomResourceDefinition> incomingCRDs);
	}
}
