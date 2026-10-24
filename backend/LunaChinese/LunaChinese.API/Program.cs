using LunaChinese.Core.Features.WordsAnalysis;
using LunaChinese.Core.Features.WordsExtraction;
using LunaChinese.Infrastructure.Context;
using LunaChinese.Infrastructure.Extensions;
using LunaChinese.Infrastructure.Features.WordsAnalysis;
using LunaChinese.Infrastructure.Features.WordsExtraction.Gemini;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddLunaChineseContext(builder.Configuration, builder.Environment);
builder.Services.AddScoped<IAnalysisCache, EfCoreAnalysisCache>();

#region Gemini

const string geminiKey = "Gemini";

var geminiSection = builder.Configuration.GetSection(geminiKey);
var geminiOptions = geminiSection.Get<GeminiExtractionOptions>()
    ?? throw new InvalidOperationException($"Missing '{geminiKey}' configuration section.");
if (string.IsNullOrWhiteSpace(geminiOptions.Model))
    throw new InvalidOperationException($"Missing '{geminiKey}:{nameof(geminiOptions.Model)}' configuration value.");
var geminiApiKey = geminiSection["ApiKey"] ?? 
                   throw new InvalidOperationException($"Missing '{geminiKey}:ApiKey' configuration value.");

builder.Services.AddSingleton(geminiOptions)
    .AddHttpClient<IWordExtractionAiClient, GeminiAiClient>(client =>
    {
        client.BaseAddress = new Uri("https://generativelanguage.googleapis.com/v1beta/");
        client.DefaultRequestHeaders.Add("x-goog-api-key", geminiApiKey);
    });
#endregion

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LunaChineseDbContext>();
    // SQLite dev: create schema based on the model.
    if (app.Environment.IsDevelopment())
        db.Database.EnsureCreated();
    // Postgres (Neon) production: apply migrations
    else 
        db.Database.Migrate();
}

app.MapControllers();

app.Run();