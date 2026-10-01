using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using RequestsManager.Api.Infrastructure;
using RequestsManager.Application;
using RequestsManager.Application.Abstractions;
using RequestsManager.Infrastructure;
using RequestsManager.Infrastructure.Persistence;
using RequestsManager.Infrastructure.Seeding;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false));
        o.AllowInputFormatterExceptionMessages = false; // don't echo CLR type names back in 400s
    });

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HeaderCurrentUser>();

builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);

var corsOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [];
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

// `dotnet run -- seed [count]` re-creates the test data and exits.
if (args.Length > 0 && args[0] == "seed")
{
    var count = args.Length > 1 && int.TryParse(args[1], out var c) ? c : 100_000;
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
    await scope.ServiceProvider.GetRequiredService<DataSeeder>().SeedAsync(count, reset: true);
    return;
}

if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
    var seedCount = app.Configuration.GetValue<int>("Database:SeedIfEmptyCount");
    if (seedCount > 0)
        await scope.ServiceProvider.GetRequiredService<DataSeeder>().SeedAsync(seedCount, reset: false);
}

app.UseExceptionHandler();
app.UseStatusCodePages();

// Swagger is on in every environment so reviewers can explore the API however they start it.
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors();
app.MapControllers();
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

app.Run();

public partial class Program; // for WebApplicationFactory in integration tests
