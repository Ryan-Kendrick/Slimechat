using Hubs;
using Models.Slimechat;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
    // options.IncludeScopes = true;
    options.TimestampFormat = "HH:mm:ss dd-MM ";
}); builder.Logging.SetMinimumLevel(LogLevel.Debug);

builder.Services.Configure<ChatSettings>(
    builder.Configuration.GetSection("ChatSettings"));
builder.Services.AddSignalR(o =>
{
    o.EnableDetailedErrors = builder.Environment.IsDevelopment();
});
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


var app = builder.Build();

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
