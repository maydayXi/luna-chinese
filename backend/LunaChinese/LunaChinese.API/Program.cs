using LunaChinese.Core.Features.WordsAnalysis;
using LunaChinese.Infrastructure.Context;
using LunaChinese.Infrastructure.Extensions;
using LunaChinese.Infrastructure.Features.WordsAnalysis;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddLunaChineseContext(builder.Configuration, builder.Environment);
builder.Services.AddScoped<IAnalysisCache, EfCoreAnalysisCache>();

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

app.Run();