using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace hotel_erp.Api.Services
{
    public class BackupHostedService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly BackupOptions _options;

        public BackupHostedService(IServiceScopeFactory scopeFactory, IOptions<BackupOptions> options)
        {
            _scopeFactory = scopeFactory;
            _options = options.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_options.Enabled) return;

            var nextBackup = GetNextRun(DateTime.Now);
            var nextUploadSync = DateTime.Now.AddHours(_options.PendingUploadIntervalHours);

            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.Now;
                if (now >= nextBackup)
                {
                    await RunScopedAsync(s => s.RunLocalBackupAsync(stoppingToken));
                    nextBackup = GetNextRun(DateTime.Now.AddMinutes(1));
                }

                if (now >= nextUploadSync)
                {
                    await RunScopedAsync(s => s.SyncPendingUploadsAsync(stoppingToken));
                    nextUploadSync = DateTime.Now.AddHours(_options.PendingUploadIntervalHours);
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        private async Task RunScopedAsync(Func<DatabaseBackupService, Task> action)
        {
            using var scope = _scopeFactory.CreateScope();
            await action(scope.ServiceProvider.GetRequiredService<DatabaseBackupService>());
        }

        private DateTime GetNextRun(DateTime from)
        {
            if (!TimeOnly.TryParse(_options.RunAt, out var time)) time = new TimeOnly(2, 0);
            var next = from.Date.Add(time.ToTimeSpan());
            return next <= from ? next.AddDays(1) : next;
        }
    }
}

