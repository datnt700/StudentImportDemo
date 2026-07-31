using Microsoft.EntityFrameworkCore;
using StudentImportDemo.Data;

namespace StudentImportDemo.Services.Background;

public class StudentImportBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IStudentImportJobQueue _queue;
    private readonly ILogger<StudentImportBackgroundService> _logger;

    public StudentImportBackgroundService(
        IServiceScopeFactory scopeFactory,
        IStudentImportJobQueue queue,
        ILogger<StudentImportBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _queue = queue;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await EnqueueExistingPendingJobs(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var importId = await _queue.DequeueAsync(stoppingToken);
                await ProcessJob(importId, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while processing student import jobs.");
            }
        }
    }

    private async Task EnqueueExistingPendingJobs(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var pendingJobIds = await db.ImportJobs
            .Where(job => job.Status == "Pending")
            .OrderBy(job => job.CreatedAt)
            .Select(job => job.Id)
            .ToListAsync(cancellationToken);

        foreach (var jobId in pendingJobIds)
        {
            _queue.Enqueue(jobId);
        }
    }

    private async Task ProcessJob(string importId, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var import = scope.ServiceProvider.GetRequiredService<IStudentImport>();
        await import.ProcessImportJobAsync(importId, cancellationToken);
    }
}