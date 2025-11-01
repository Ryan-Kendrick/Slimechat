using Microsoft.OpenApi.Models;
using Microsoft.EntityFrameworkCore;
using Hubs;
using Models.Slimechat;

var builder = WebApplication.CreateBuilder(args);

string connectionString = builder.Configuration.GetConnectionString("Messages") ?? "Data Source=Messages.db";

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
    // options.IncludeScopes = true;
    options.TimestampFormat = "HH:mm:ss dd-MM ";
}); builder.Logging.SetMinimumLevel(LogLevel.Debug);

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

builder.Services.AddSqlServer<ChatDb>(connectionString);

var app = builder.Build();

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

TaskScheduler.UnobservedTaskException += (sender, err) =>
{
    var logger = app.Services.GetRequiredService<ILoggerFactory>()
        .CreateLogger("UnobservedTask");
    logger.LogError(err.Exception, "Unobserved task exception");
    err.SetObserved();
};

app.Run();
