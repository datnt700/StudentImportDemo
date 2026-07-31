using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentImportDemo.Data;
using StudentImportDemo.Entity;
using StudentImportDemo.Services.Background;

namespace StudentImportDemo.Controllers;

public class ImportController : Controller
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _environment;
    private readonly IStudentImportJobQueue _queue;

    public ImportController(
        AppDbContext db,
        IWebHostEnvironment environment,
        IStudentImportJobQueue queue)
    {
        _db = db;
        _environment = environment;
        _queue = queue;
    }

    [HttpGet("")]
    [HttpGet("import")]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet("api/import-jobs/{importId}")]
    public async Task<IActionResult> GetImportJob(string importId)
    {
        var job = await _db.ImportJobs
            .Include(x => x.RowResults)
            .FirstOrDefaultAsync(x => x.Id == importId);

        if (job == null)
        {
            return NotFound();
        }

        return Ok(new
        {
            job.Id,
            job.FileName,
            job.Status,
            job.SuccessCount,
            job.FailedCount,
            rows = job.RowResults
                .OrderBy(x => x.RowNumber)
                .Select(x => new
                {
                    x.RowNumber,
                    x.StudentCode,
                    x.FullName,
                    x.Status,
                    x.Message
                })
        });
    }

    [HttpPost("api/students/import")]
    public async Task<IActionResult> Import([FromForm] IFormFile file, [FromForm] string importId)
    {
        var existingJob = await _db.ImportJobs.AnyAsync(job => job.Id == importId);
        if (existingJob)
        {
            return Conflict(new
            {
                message = "An import job with this importId already exists. Create a new importId for a new import."
            });
        }

        var importDirectory = Path.Combine(_environment.ContentRootPath, "uploads", "imports");
        Directory.CreateDirectory(importDirectory);

        var storedFileName = importId + Path.GetExtension(file.FileName);
        var absoluteStoredPath = Path.Combine(importDirectory, storedFileName);
        var relativeStoredPath = Path.Combine("uploads", "imports", storedFileName);

        await using (var stream = System.IO.File.Create(absoluteStoredPath))
        {
            await file.CopyToAsync(stream);
        }

        _db.ImportJobs.Add(new ImportJob
        {
            Id = importId,
            FileName = file.FileName,
            StoredFilePath = relativeStoredPath,
            Status = "Pending",
            SuccessCount = 0,
            FailedCount = 0,
            CreatedAt = DateTime.UtcNow
        });
        try
        {
           await _db.SaveChangesAsync();
        }
        catch(DbUpdateException ex)
        {
            Console.WriteLine("Error", ex);
        }
        _queue.Enqueue(importId);

        return Ok(new
        {
            importId,
            status = "Pending",
            fileName = file.FileName,
            message = "Import job created. Processing will continue in the background."
        });
    }
}