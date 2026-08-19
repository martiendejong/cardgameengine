using CardGameEngine.Api.Hubs;
using CardGameEngine.Api.Services;
using CardGameEngine.Engine;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Game services (singletons for in-memory state)
builder.Services.AddSingleton<GameDefinitionService>();
builder.Services.AddSingleton<MatchService>();
builder.Services.AddSingleton<RuleEngine>();
builder.Services.AddSingleton<StateProjector>();
builder.Services.AddSingleton<MatchConnectionRegistry>();
builder.Services.AddSingleton<BotService>();

// CORS - allow frontend dev server
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .WithOrigins("http://localhost:5173", "http://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();

// Serve static frontend files in production
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseRouting();

app.MapControllers();
app.MapHub<GameHub>("/gamehub");

// Fallback for SPA routing
app.MapFallbackToFile("index.html");

app.Run();
