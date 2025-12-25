using Microsoft.OpenApi.Models;
using Microsoft.EntityFrameworkCore;
using Hubs;
using Models.Slimechat;
using Api.Infrastructure;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("secrets.json", optional: true, reloadOnChange: true);


builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
    // options.IncludeScopes = true;
    options.TimestampFormat = "HH:mm:ss dd-MM ";
}); builder.Logging.SetMinimumLevel(LogLevel.Debug);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(config =>
{
    config.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Slime Chat API",
        Description = "The chat service for https://slimeascend.com/",
        Version = "v1"
    });
});

builder.Services.AddSignalR(o =>
{
    o.EnableDetailedErrors = builder.Environment.IsDevelopment();
});
builder.Services.Configure<ChatSettings>(builder.Configuration.GetSection("ChatSettings"));
builder.Services.Configure<ApiSettings>(builder.Configuration.GetSection("ApiSettings"));
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(
        builder =>
        {
            builder.WithOrigins(["http://localhost:3000", "https://unstable.slimeascend.com", "https://slimeascend.com"])
                .AllowAnyHeader()
                .WithMethods("GET", "POST")
                .AllowCredentials();
        });
});

string connectionString = builder.Configuration.GetConnectionString("Messages") ?? "Data Source=Messages.db";
builder.Services.AddSqlite<ChatDb>(connectionString);

var app = builder.Build();

app.UseExceptionHandler();
app.Use(async (context, next) =>
{
    await next();
    Console.WriteLine($"{context.Request.Method} {context.Request.Path} {context.Response.StatusCode}");
});
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Slime Chat API V1");
    });
}

app.UseRouting();
app.UseCors();
app.MapHub<ChatHub>("/chathub");
app.MapControllers();

app.MapGet("/robots.txt", () =>
{
    var sb = new StringBuilder();
    sb.AppendLine("User-agent: *");
    sb.AppendLine("Disallow: /");
    return Results.Text(sb.ToString(), "text/plain");
});

TaskScheduler.UnobservedTaskException += (sender, err) =>
{
    var logger = app.Services.GetRequiredService<ILoggerFactory>()
        .CreateLogger("UnobservedTask");
    logger.LogError(err.Exception, "Unobserved task exception");
    err.SetObserved();
};

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ChatDb>();
    try
    {
        if (app.Environment.IsDevelopment())
        {
            dbContext.Database.EnsureDeleted();
        }

        dbContext.Database.Migrate();
        app.Logger.LogInformation("Database initialised successfully");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Failed to initialize or migrate database.");
    }
}

app.Run();
