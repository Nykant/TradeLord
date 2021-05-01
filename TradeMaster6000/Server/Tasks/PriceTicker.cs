using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace TradeMaster6000.Server.Tasks
{
    //public class PriceTicker : IHostedService
    //{
    //    private readonly ILogger<PriceTicker> logger;
    //    private readonly IWorker worker;

    //    public PriceTicker(ILogger<PriceTicker> logger,
    //        IWorker worker)
    //    {
    //        this.logger = logger;
    //        this.worker = worker;
    //    }

    //    public async Task StartAsync(CancellationToken cancellationToken)
    //    {
    //        await worker.StartTicker(cancellationToken);
    //    }

    //    public Task StopAsync(CancellationToken cancellationToken)
    //    {
    //        return Task.CompletedTask;
    //    }
    //}
}
