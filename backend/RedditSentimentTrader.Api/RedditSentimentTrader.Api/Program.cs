using Microsoft.EntityFrameworkCore;
using OpenAI.Chat;
using RedditSentimentTrader.Api.Data;
using RedditSentimentTrader.Api.Options;
using RedditSentimentTrader.Api.Repositories;
using RedditSentimentTrader.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
);

builder.Services.AddScoped<IRedditPostRepository, RedditPostRepository>();
builder.Services.AddScoped<IRedditPostService, RedditPostService>();
builder.Services.AddScoped<IRedditAuthService, RedditAuthService>();
builder.Services.AddScoped<IRedditApiService, RedditApiService>();
builder.Services.AddScoped<IWsbDailyService, WsbDailyService>();
builder.Services.AddHostedService<RedditIngestionWorker>();
builder.Services.AddScoped<IRedditDailyThreadService, RedditDailyThreadService>();
builder.Services.AddScoped<WsbLocator>();


builder.Services.Configure<RedditOptions>(builder.Configuration.GetSection("Reddit"));

builder.Services.AddHttpClient("RedditAPI", c =>
{
    c.DefaultRequestHeaders.UserAgent.ParseAdd(
        builder.Configuration.GetSection("Reddit")["UserAgent"] ?? "SentimentTrader/1.0"
    );
});

builder.Services.AddSingleton<ChatClient>(sp =>
{
    var cfg = builder.Configuration;
    var apiKey =
        cfg["OpenAI:ApiKey"]
        ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY")
        ?? throw new InvalidOperationException("OpenAI API key no good");

    var model = cfg["OpenAI:Model"] ?? "gpt-4o-mini";

    return new ChatClient(model: model, apiKey: apiKey);
});

builder.Services.AddScoped<ISentimentService, OpenAiSentimentService>();
var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
