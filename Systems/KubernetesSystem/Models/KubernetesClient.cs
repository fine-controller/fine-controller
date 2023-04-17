using k8s;
using System;

namespace Systems.KubernetesSystem.Models
{
	internal class KubernetesClient : IDisposable
	{
		public Kubernetes Client { get; set; }
		public void Dispose() => Client?.Dispose();
	}
}
