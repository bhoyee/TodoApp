using Microsoft.EntityFrameworkCore;
using TodoApp.Api;
using TodoApp.Api.Endpoints;
using TodoApp.Infrastructure;
using TodoApp.Infrastructure.Persistence;
using TodoApp.Infrastructure.Persistence.Seeding;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks()
    .AddDbContextCheck<TodoAppDbContext>("database");
builder.Services.AddApplicationUseCases();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();
app.UseHttpsRedirection();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    await using var scope = app.Services.CreateAsyncScope();
    var database = scope.ServiceProvider
        .GetRequiredService<TodoAppDbContext>();
    await database.Database.MigrateAsync();
    await DevelopmentDataSeeder.SeedAsync(
        database,
        CancellationToken.None);
}

app.MapGet("/", () => Results.Redirect("/openapi/v1.json"))
    .ExcludeFromDescription();
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");
app.MapProjectEndpoints();

app.Run();

public partial class Program;
