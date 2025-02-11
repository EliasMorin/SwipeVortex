using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.SignalR;
using SwipeVortexWb;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SwipeVortexWb.Instagram;

// Programme principal (instructions de niveau supérieur)
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors();
builder.Services.AddDbContext<HappnDbContext>();
builder.Services.AddDbContext<BumbleDbContext>();
builder.Services.AddDbContext<InstagramDbContext>();
builder.Services.AddSingleton<InstagramManager>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors(builder => builder
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader());
app.MapControllers();
app.UseDefaultFiles();
app.UseStaticFiles();  // This should come before app.UseRouting()
app.UseRouting();
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
    endpoints.MapHub<LogHub>("/loghub");
});

app.Run();

// Modèles
public class HashtagAnalyticsData
{
    public HashtagData HashtagData { get; set; }
    public NLPAnalysis NLPAnalysis { get; set; }
}

public class HashtagData
{
    public string Hashtag { get; set; }
    public DateTime ScrapeDate { get; set; }
    public List<MediaInfo> Medias { get; set; }
}

public class NLPAnalysis
{
    public List<PostAnalysisData> TopPosts { get; set; }
    public List<string> Keywords { get; set; }
}

public class PostAnalysisData
{
    public string MediaCode { get; set; }
    public string UserName { get; set; }
    public double TopicRelevance { get; set; }
    public long LikesCount { get; set; }
    public long CommentsCount { get; set; }
    public long FollowerCount { get; set; }
    public long ImpressionsCount { get; set; }
    public double PenetrationRate { get; set; }
    public double FinalScore { get; set; }
    public Dictionary<string, double> CategoryScores { get; set; }
}

public class MediaInfo
{
    // Ajoutez les propriétés nécessaires pour MediaInfo
}
