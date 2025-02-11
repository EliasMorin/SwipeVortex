using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SwipeVortexWb;
using System.Text.Json;

[ApiController]
[Route("api/[controller]")]
public class HappnController : ControllerBase
{
    private readonly HappnDbContext _context;
    
    public HappnController(HappnDbContext context)
    {
        _context = context;
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetHappnStats()
    {
        try
        {
            // Récupérer les statistiques
            var stats = await _context.Stats
                .FirstOrDefaultAsync() ?? new HappnStats
                {
                    TotalLikes = 0,
                    TotalMatches = 0,
                    TotalMessagesSent = 0,
                    TotalConversations = 0,
                    TotalCrushes = 0,
                    LastUpdated = DateTime.UtcNow
                };

            // Récupérer les rencontres
            var encounters = await _context.Encounters
                .OrderByDescending(e => e.Date)
                .ToListAsync();

            // Créer l'objet de réponse
            var response = new
            {
                Encounters = encounters,
                TotalLikes = stats.TotalLikes,
                TotalMatches = stats.TotalMatches,
                TotalMessagesSent = stats.TotalMessagesSent,
                TotalConversations = stats.TotalConversations,
                TotalCrushes = stats.TotalCrushes,
                LastUpdated = stats.LastUpdated
            };
            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest($"Error reading Happn stats: {ex.Message}");
        }
    }

    [HttpPost("stats")]
    public async Task<IActionResult> UpdateStats([FromBody] HappnStats newStats)
    {
        try
        {
            var existingStats = await _context.Stats.FirstOrDefaultAsync();
            
            if (existingStats == null)
            {
                _context.Stats.Add(newStats);
            }
            else
            {
                existingStats.TotalLikes = newStats.TotalLikes;
                existingStats.TotalMatches = newStats.TotalMatches;
                existingStats.TotalMessagesSent = newStats.TotalMessagesSent;
                existingStats.TotalConversations = newStats.TotalConversations;
                existingStats.TotalCrushes = newStats.TotalCrushes;
                existingStats.LastUpdated = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync();
            return Ok(newStats);
        }
        catch (Exception ex)
        {
            return BadRequest($"Error updating Happn stats: {ex.Message}");
        }
    }
}