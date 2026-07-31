using Microsoft.EntityFrameworkCore;
using StudentImportDemo;
using StudentImportDemo.Data;
using StudentImportDemo.Middleware;
using StudentImportDemo.Model;
using StudentImportDemo.Services;
using StudentImportDemo.Services.Background;
using StudentImportDemo.Services.Excel;
using StudentImportDemo.Services.Impl;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddScoped<IStudentImport, StudentImportImpl>();
builder.Services.AddScoped(typeof(IExcelImportReader<>), typeof(ExcelImportReader<>));
builder.Services.AddScoped<IExcelImportDefinition<StudentImportRow>, StudentImportDefinition>();
builder.Services.AddSignalR();
builder.Services.AddSingleton<IStudentImportJobQueue, StudentImportJobQueue>();
builder.Services.AddHostedService<StudentImportBackgroundService>();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

app.UseHttpsRedirection();
app.UseMiddleware<StudentImportFileValidationMiddleware>();

app.MapHub<ImportHub>("/hubs/import");
app.MapControllers();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Import}/{action=Index}/{id?}");

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}