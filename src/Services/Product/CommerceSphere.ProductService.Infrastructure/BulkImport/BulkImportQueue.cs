using System.Threading.Channels;
using CommerceSphere.ProductService.Application.Interfaces;

namespace CommerceSphere.ProductService.Infrastructure.BulkImport;

// Singleton, in-process work queue handing job ids from the upload endpoint to the background
// worker. Unbounded because the producer (an admin clicking upload) is naturally low-volume.
//
// Durability note: this queue is in-memory, so a process restart loses any job that was queued
// but not yet picked up. The job row stays 'Pending' in the DB — recovery would re-enqueue
// Pending jobs on startup. (Kept simple here; ProcessJobAsync already guards re-entry.)
public class BulkImportQueue : IBulkImportQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>(
        new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });

    public ValueTask EnqueueAsync(Guid jobId, CancellationToken ct = default) =>
        _channel.Writer.WriteAsync(jobId, ct);

    public ValueTask<Guid> DequeueAsync(CancellationToken ct = default) =>
        _channel.Reader.ReadAsync(ct);
}
