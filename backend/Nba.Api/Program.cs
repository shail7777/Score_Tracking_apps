using Microsoft.EntityFrameworkCore;
using Nba.Api.Chat;
using Nba.Api.Data;
using Nba.Api.Import;
using Nba.Api.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Connection string 'Default' is not configured.");

builder.Services.AddDbContext<NbaDbContext>(options =>
    options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure(10, TimeSpan.FromSeconds(5), null)));

builder.Services.Configure<NbaGraphQlOptions>(builder.Configuration.GetSection("NbaGraphQl"));
builder.Services.AddHttpClient<NbaGraphQlClient>(c => c.Timeout = TimeSpan.FromSeconds(60));
builder.Services.AddScoped<NbaImportService>();
builder.Services.AddScoped<ITeamService, TeamService>();
builder.Services.AddScoped<IPlayerService, PlayerService>();
builder.Services.AddScoped<ITeamPlayerService, TeamPlayerService>();
builder.Services.AddScoped<IGameService, GameService>();
builder.Services.AddScoped<ScorecardService>();
builder.Services.AddScoped<ScoringService>();
builder.Services.AddHostedService<NbaImportHostedService>();

builder.Services.Configure<ChatOptions>(builder.Configuration.GetSection("Chat"));
var chatOptions = builder.Configuration.GetSection("Chat").Get<ChatOptions>() ?? new ChatOptions();
builder.Services.AddHttpClient<IChatModelClient, OllamaChatClient>(c =>
{
    c.BaseAddress = new Uri(chatOptions.OllamaUrl);
    c.Timeout = TimeSpan.FromSeconds(chatOptions.TimeoutSeconds);
});
builder.Services.AddSingleton<FaqChatService>();

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddHealthChecks().AddDbContextCheck<NbaDbContext>();

var app = builder.Build();

// Applies any pending EF migrations (creating the database/tables on first run).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<NbaDbContext>();
    await db.Database.MigrateAsync();
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
