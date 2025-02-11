using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using SwipeVortexWb;
using SwipeVortexWb.Instagram;

namespace SwipeVortexWb.Controllers
{
    // BumbleEncounterRequest.cs
    public class BumbleEncounterRequest
    {
        public string Cookie { get; set; }
        public bool Automessage { get; set; }
        public string Message { get; set; }
        public bool Autolike { get; set; }
    }

    public class InstagramHashtagRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string Hashtag { get; set; }
        public bool AutoLike { get; set; }
        public bool AutoMessage { get; set; }
        public string Message { get; set; }
    }

    public class HappnEncounterRequest
    {
        public string Token { get; set; }
        public bool AutoLike { get; set; }
        public bool AutoMessage { get; set; }
        public string Message { get; set; }
    }

    public class StopTaskRequest 
    {
        public string Platform { get; set; }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class DatingController : ControllerBase
    {
        private readonly IHubContext<LogHub> _hubContext;
        private readonly ILogger<DatingController> _logger;
        private readonly InstagramManager _instagramManager;
        private static readonly Dictionary<string, CancellationTokenSource> _cancellationTokenSources = 
            new Dictionary<string, CancellationTokenSource>();

        // Modification du constructeur pour supporter l'injection de dépendances
        public DatingController(
            IHubContext<LogHub> hubContext, 
            ILogger<DatingController> logger,
            InstagramManager instagramManager)
        {
            _hubContext = hubContext;
            _logger = logger;
            _instagramManager = instagramManager;
        }

        [HttpPost("stop")]
        public async Task<IActionResult> StopTask([FromBody] StopTaskRequest request)
        {
            try 
            {
                if (string.IsNullOrEmpty(request.Platform))
                {
                    return BadRequest(new { error = "Platform must be specified" });
                }

                // Vérifier et annuler le token correspondant
                if (_cancellationTokenSources.TryGetValue(request.Platform, out var tokenSource))
                {
                    tokenSource.Cancel();
                    _cancellationTokenSources.Remove(request.Platform);
                    
                    await _hubContext.Clients.All.SendAsync("ReceiveLog", 
                        $"Task for {request.Platform} has been stopped.", "warning");
                }

                return Ok(new { message = $"Stop request for {request.Platform} processed" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error stopping task for {request.Platform}");
                return StatusCode(500, new { error = "An error occurred while stopping the task" });
            }
        }

        [HttpPost("instagram/hashtag")]
        public async Task<IActionResult> ProcessInstagramHashtag([FromBody] InstagramHashtagRequest request)
        {
            var cts = new CancellationTokenSource();
            _cancellationTokenSources["instagram"] = cts;

            try
            {
                if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
                {
                    return BadRequest(new { error = "Instagram credentials are required" });
                }

                await _hubContext.Clients.All.SendAsync("ReceiveLog", $"Starting hashtag analysis for #{request.Hashtag}", "info");

                bool loginSuccess = await _instagramManager.InitializeAndLogin(
                    request.Username,
                    request.Password,
                    cts.Token);

                if (!loginSuccess)
                {
                    _logger.LogError("Instagram login failed");
                    await _hubContext.Clients.All.SendAsync("ReceiveLog", "Failed to login to Instagram", "error");

                    return BadRequest(new { 
                        error = "Instagram login failed", 
                        details = "Check credentials and network connection" 
                    });
                }

                var context = new InstagramDbContext();
                var hashtagScraper = new InstagramHashtagScraper(_instagramManager.GetApi(), context);

                await _hubContext.Clients.All.SendAsync("ReceiveLog", $"Processing hashtag: #{request.Hashtag}", "info");
                await hashtagScraper.ProcessHashtagComplete(request.Hashtag, cts.Token);

                if (request.AutoMessage && !string.IsNullOrWhiteSpace(request.Message))
                {
                    await _hubContext.Clients.All.SendAsync("ReceiveLog", "Auto-messaging not implemented in this version", "warning");
                }

                await _hubContext.Clients.All.SendAsync("ReceiveLog", $"Successfully processed hashtag #{request.Hashtag}", "success");

                return Ok(new { 
                    message = "Hashtag processed successfully", 
                    hashtag = request.Hashtag 
                });
            }
            catch (OperationCanceledException)
            {
                await _hubContext.Clients.All.SendAsync("ReceiveLog", "Instagram task was cancelled", "warning");
                return Ok(new { message = "Instagram task cancelled" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing Instagram hashtag {request.Hashtag}");
                await _hubContext.Clients.All.SendAsync("ReceiveLog", $"Error: {ex.Message}", "error");

                return BadRequest(new { 
                    error = ex.Message, 
                    stackTrace = ex.StackTrace 
                });
            }
            finally 
            {
                if (_cancellationTokenSources.ContainsKey("instagram"))
                {
                    _cancellationTokenSources.Remove("instagram");
                }
            }
        }

        [HttpPost("bumble/encounters")]
        public async Task<IActionResult> GetBumbleEncounters([FromBody] BumbleEncounterRequest request)
        {
            await _hubContext.Clients.All.SendAsync("ReceiveLog", "Received request for Bumble encounters", "info");

            try
            {
                // Set the cookie from the request
                if (string.IsNullOrEmpty(request.Cookie))
                {
                    return BadRequest(new { error = "Bumble cookie is required" });
                }
                
                Bumble.SetCookie(request.Cookie);
                
                List<string> matchedUserIds = new List<string>();
                int totalMatches = 0;

                await _hubContext.Clients.All.SendAsync("ReceiveLog", "Starting Bumble.GetEncounters()", "info");
                await Bumble.GetEncounters();
                await _hubContext.Clients.All.SendAsync("ReceiveLog", "Successfully executed Bumble encounters", "success");

                if (request.Automessage && !string.IsNullOrWhiteSpace(request.Message))
                {
                    await _hubContext.Clients.All.SendAsync("ReceiveLog", "Checking for matches...", "info");
                    (totalMatches, matchedUserIds) = await Bumble.CheckMessages();

                    if (matchedUserIds.Count > 0)
                    {
                        await _hubContext.Clients.All.SendAsync("ReceiveLog", $"Found {matchedUserIds.Count} matches. Sending messages...", "info");
                        await Bumble.SendMessageToAllMatches(matchedUserIds, request.Message);
                        await _hubContext.Clients.All.SendAsync("ReceiveLog", $"Successfully sent messages to {matchedUserIds.Count} matches", "success");
                    }
                    else
                    {
                        await _hubContext.Clients.All.SendAsync("ReceiveLog", "No new matches to message", "info");
                    }
                }

                return Ok(new { 
                    message = "Successfully executed Bumble encounters and messaging",
                    matchesFound = matchedUserIds.Count,
                    messagesSent = request.Automessage ? matchedUserIds.Count : 0
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Bumble encounters and messaging");
                await _hubContext.Clients.All.SendAsync("ReceiveLog", $"Error: {ex.Message}", "error");
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("happn/encounters")]
        public async Task<IActionResult> GetHappnEncounters([FromBody] HappnEncounterRequest request)
        {
            await _hubContext.Clients.All.SendAsync("ReceiveLog", "Received request for Happn encounters", "info");

            try
            {
                // Validate token
                if (string.IsNullOrWhiteSpace(request.Token))
                {
                    await _hubContext.Clients.All.SendAsync("ReceiveLog", "Happn token is missing", "error");
                    return BadRequest(new { error = "Happn token is required" });
                }

                List<string> matchedUserIds = new List<string>();
                int totalMatches = 0;

                // Validate token by attempting to get user info
                try
                {
                    await _hubContext.Clients.All.SendAsync("ReceiveLog", "Validating Happn token...", "info");
                    await Happn.GetUserInfo(request.Token);
                    await _hubContext.Clients.All.SendAsync("ReceiveLog", "Token validated successfully", "success");
                }
                catch (Exception ex)
                {
                    await _hubContext.Clients.All.SendAsync("ReceiveLog", "Invalid Happn token", "error");
                    return BadRequest(new { error = "Invalid Happn token", details = ex.Message });
                }

                // Process recommendations (like/react to everyone)
                await _hubContext.Clients.All.SendAsync("ReceiveLog", "Starting Happn.GetAndReactToRecommendations()", "info");
                await Happn.GetAndReactToRecommendations(request.Token, request.AutoLike);
                await _hubContext.Clients.All.SendAsync("ReceiveLog", "Successfully executed Happn encounters", "success");

                // Retrieve conversations to get matched user IDs
                if (request.AutoMessage && !string.IsNullOrWhiteSpace(request.Message))
                {
                    await _hubContext.Clients.All.SendAsync("ReceiveLog", "Checking for matches...", "info");
                    
                    // Get conversations and crushes
                    var conversationIds = await Happn.GetConversations(request.Token);
                    var crushIds = await Happn.GetCrushes(request.Token);

                    // Combine conversation IDs and crush IDs
                    matchedUserIds = conversationIds.Concat(crushIds).Distinct().ToList();

                    if (matchedUserIds.Count > 0)
                    {
                        await _hubContext.Clients.All.SendAsync("ReceiveLog", $"Found {matchedUserIds.Count} matches. Sending messages...", "info");
                        
                        // Send messages to all matches
                        await Happn.SendGenericMessageToAllConversations(request.Token, conversationIds, crushIds, request.Message);
                        
                        totalMatches = matchedUserIds.Count;
                        await _hubContext.Clients.All.SendAsync("ReceiveLog", $"Successfully sent messages to {totalMatches} matches", "success");
                    }
                    else
                    {
                        await _hubContext.Clients.All.SendAsync("ReceiveLog", "No new matches to message", "info");
                    }
                }

                return Ok(new { 
                    message = "Successfully executed Happn encounters and messaging",
                    matchesFound = matchedUserIds.Count,
                    messagesSent = request.AutoMessage ? matchedUserIds.Count : 0
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Happn encounters and messaging");
                await _hubContext.Clients.All.SendAsync("ReceiveLog", $"Error: {ex.Message}", "error");
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}