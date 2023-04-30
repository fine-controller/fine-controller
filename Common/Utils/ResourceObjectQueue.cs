using Common.Models;
using k8s.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Common.Utils
{
	public class ResourceObjectQueue
	{
		private readonly SemaphoreSlim _lock = new(1, 1);
		private readonly SemaphoreSlim _semaphore = new(0);
		private readonly Queue<ResourceObject> _resourceObjects = new();
		
		public async Task AddAsync(ResourceObject resourceObject, CancellationToken cancellationToken)
		{
			await _lock.WaitAsync(cancellationToken);

			try
			{
				var existing = _resourceObjects.FirstOrDefault(item => item.LongName == resourceObject.LongName);

				if (existing is not null)
				{
					// - we only care about the latest version
					// - an older version still has not been processed, update it
					
					if (string.Compare(resourceObject.ResourceVersion(), existing.ResourceVersion(), StringComparison.Ordinal) > 0)
					{
						existing.Update(resourceObject.EventType, resourceObject.Data);
					}

					return;
				}

				_resourceObjects.Enqueue(resourceObject);
				_semaphore.Release();
			}
			finally
			{
				_lock.Release();
			}
		}

		public async Task<ResourceObject> GetNextAsync(CancellationToken cancellationToken)
		{
			await _semaphore.WaitAsync(cancellationToken);
			await _lock.WaitAsync(cancellationToken);

			try
			{
				return _resourceObjects.Dequeue();
			}
			finally
			{
				_lock.Release();
			}
		}
	}
}
