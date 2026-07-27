using StudentImportDemo.Middleware;
using StudentImportDemo.Model;
using StudentImportDemo.Services;
using StudentImportDemo.Services.Excel;
using StudentImportDemo.Services.Impl;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddControllers();
builder.Services.AddScoped<IImport, ImportImpl>();
builder.Services.AddScoped(typeof(IExcelImportReader<>), typeof(ExcelImportReader<>));
builder.Services.AddScoped<IExcelImportDefinition<StudentImportRow>, StudentImportDefinition>();

var app = builder.Build();

app.UseMiddleware<StudentImportFileValidationMiddleware>();

app.MapControllers();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
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
// edge case
// Client gọi API nhưng không gửi file.
// File empty
// File xlsx khong hop le
// xlsx khong dung sheet
// xlsx khong dung header
// xlsx co header nhung khong co value


app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

