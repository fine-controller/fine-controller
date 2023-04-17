using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace Common.Interfaces
{
    public interface IResourceObjectEventHandler
    {
        public Task HandleAsync(JsonObject resourceObjectEvent, CancellationToken cancellationToken);
    }
}
