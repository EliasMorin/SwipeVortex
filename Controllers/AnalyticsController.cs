using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace SwipeVortexWb.Instagram
{
    [ApiController]
    [Route("api/[controller]")]
    public class InstagramAnalysisController : ControllerBase
    {
        private readonly InstagramDbContext _context;

        public InstagramAnalysisController(InstagramDbContext context)
        {
            _context = context;
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetInstagramStats()
        {
            try
            {
                // Retrieve or create stats
                var stats = await _context.Stats
                    .FirstOrDefaultAsync() ?? new InstagramStats
                    {
                        TotalHashtagsAnalyzed = 0,
                        TotalPostsProcessed = 0,
                        TotalUniqueUsers = 0,
                        TotalRelatedHashtags = 0,
                        LastAnalysisDate = DateTime.UtcNow
                    };

                // Retrieve recent hashtag analyses
                var recentAnalyses = await _context.HashtagAnalyses
                    .OrderByDescending(h => h.ScrapeDate)
                    .Take(10)
                    .ToListAsync();

                var response = new
                {
                    Stats = stats,
                    RecentAnalyses = recentAnalyses
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error reading Instagram stats: {ex.Message}");
            }
        }

        [HttpPost("save-analysis")]
        public async Task<IActionResult> SaveHashtagAnalysis([FromBody] InstagramHashtagAnalysis analysis)
        {
            try
            {
                // Update or create stats
                var stats = await _context.Stats.FirstOrDefaultAsync();
                if (stats == null)
                {
                    stats = new InstagramStats();
                    _context.Stats.Add(stats);
                }

                // Update stats
                stats.TotalHashtagsAnalyzed++;
                stats.TotalPostsProcessed += analysis.Medias.Count;
                stats.TotalUniqueUsers += analysis.Medias.Select(m => m.User.UserName).Distinct().Count();
                stats.TotalRelatedHashtags += analysis.RelatedHashtags.Count;
                stats.LastAnalysisDate = DateTime.UtcNow;

                // Save hashtag analysis
                _context.HashtagAnalyses.Add(analysis);

                await _context.SaveChangesAsync();
                return Ok(analysis);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error saving Instagram analysis: {ex.Message}");
            }
        }

        [HttpGet("hashtag/{hashtag}")]
        public async Task<IActionResult> GetHashtagAnalysis(string hashtag)
        {
            try
            {
                var analysis = await _context.HashtagAnalyses
                    .Include(h => h.Medias)
                    .Include(h => h.RelatedHashtags)
                    .FirstOrDefaultAsync(h => h.Hashtag == hashtag);

                if (analysis == null)
                {
                    return NotFound($"No analysis found for hashtag: {hashtag}");
                }

                return Ok(analysis);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error retrieving hashtag analysis: {ex.Message}");
            }
        }

        [HttpGet("top-posts")]
        public async Task<IActionResult> GetTopPosts()
        {
            try
            {
                // Récupérer d'abord les top posts analyses
                var topPostsAnalyses = await _context.TopPostsAnalyses
                    .OrderByDescending(t => t.AverageFinalScore)
                    .Take(20)
                    .ToListAsync();

                // Préparer les résultats
                var topPosts = new List<object>();

                foreach (var topPostAnalysis in topPostsAnalyses)
                {
                    // Récupérer les médias correspondants
                    var medias = await _context.Medias
                        .Where(m => topPostAnalysis.TopMediaDataIds.Contains(m.Id))
                        .Include(m => m.User)
                        .OrderByDescending(m => m.FinalScore)
                        .Take(10)
                        .ToListAsync();

                    if (medias.Any())
                    {
                        topPosts.Add(new 
                        { 
                            TopPostsAnalysis = topPostAnalysis, 
                            Medias = medias 
                        });
                    }
                }

                return Ok(topPosts);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error retrieving top posts: {ex.Message}");
            }
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetTopUsers()
        {
            try
            {
                var topUsers = await _context.Users
                    .OrderByDescending(u => u.FollowerCount)
                    .Take(50)
                    .ToListAsync();

                return Ok(topUsers);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error retrieving top users: {ex.Message}");
            }
        }

        [HttpGet("category-scores")]
        public async Task<IActionResult> GetCategoryScores()
        {
            try
            {
                var categorySummary = await _context.CategoryScores
                    .GroupBy(cs => cs.CategoryName)
                    .Select(g => new 
                    { 
                        CategoryName = g.Key, 
                        AverageScore = g.Average(cs => cs.Score),
                        TotalMedias = g.Count()
                    })
                    .OrderByDescending(cs => cs.AverageScore)
                    .ToListAsync();

                return Ok(categorySummary);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error retrieving category scores: {ex.Message}");
            }
        }

        [HttpGet("media")]
        public async Task<IActionResult> GetMediaData([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var mediaPosts = await _context.Medias
                    .Include(m => m.User)
                    .OrderByDescending(m => m.LikesCount)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var totalMediaCount = await _context.Medias.CountAsync();

                return Ok(new 
                { 
                    MediaPosts = mediaPosts, 
                    TotalCount = totalMediaCount,
                    Page = page,
                    PageSize = pageSize
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Error retrieving media data: {ex.Message}");
            }
        }

        [HttpGet("related-hashtags")]
        public async Task<IActionResult> GetRelatedHashtags()
        {
            try
            {
                var relatedHashtags = await _context.RelatedHashtags
                    .OrderByDescending(rh => rh.MediaCount)
                    .Take(100)
                    .ToListAsync();

                return Ok(relatedHashtags);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error retrieving related hashtags: {ex.Message}");
            }
        }
    }
}