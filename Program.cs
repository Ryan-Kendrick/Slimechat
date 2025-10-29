using Hubs;
using Models.Slimechat;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Debug);

builder.Services.Configure<ChatSettings>(
    builder.Configuration.GetSection("ChatSettings"));
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
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

app.Run();
