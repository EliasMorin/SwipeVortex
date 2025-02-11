using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.IO.Compression;
using Spectre.Console;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;

namespace SwipeVortexWb 
{
    public class Bumble
    {
        // HttpClient
        private static readonly HttpClient client;

        private static readonly BumbleDbContext _context = new BumbleDbContext();
        private static Stats _stats;
        private static string _cookieString;

        static Bumble()
        {
            var handler = new HttpClientHandler { UseCookies = false };
            client = new HttpClient(handler);
            SetDefaultHeaders(client);
            
            // Initialize database and load stats
            _context.Database.EnsureCreated();
            _stats = _context.Stats.FirstOrDefault() ?? new Stats 
            { 
                TotalLikes = 0,
                TotalMatches = 0,
                TotalMessagesSent = 0,
                LastUpdated = DateTime.UtcNow 
            };
            if (_stats.Id == 0)
            {
                _context.Stats.Add(_stats);
                _context.SaveChanges();
            }
        }

        public static void SetCookie(string cookieString)
        {
            _cookieString = cookieString;
            SetDefaultHeaders(client);
        }
        
        private static void SetDefaultHeaders(HttpClient client)
        {
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Accept", "*/*");
            client.DefaultRequestHeaders.Add("Accept-Language", "en-GB,en-US;q=0.9,en;q=0.8");
            client.DefaultRequestHeaders.Add("Cache-Control", "no-cache");
            client.DefaultRequestHeaders.Add("Origin", "https://am1.bumble.com");
            client.DefaultRequestHeaders.Add("Pragma", "no-cache");
            client.DefaultRequestHeaders.Add("Referer", "https://am1.bumble.com/app");
            client.DefaultRequestHeaders.Add("Sec-Ch-Ua", "\"Not/A)Brand\";v=\"8\", \"Chromium\";v=\"126\"");
            client.DefaultRequestHeaders.Add("Sec-Ch-Ua-Mobile", "?0");
            client.DefaultRequestHeaders.Add("Sec-Ch-Ua-Platform", "\"Linux\"");
            client.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "empty");
            client.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "cors");
            client.DefaultRequestHeaders.Add("Sec-Fetch-Site", "same-origin");
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            client.DefaultRequestHeaders.Add("X-Use-Session-Cookie", "1");
            
            if (!string.IsNullOrEmpty(_cookieString))
            {
                client.DefaultRequestHeaders.Add("Cookie", _cookieString);
            }
        }

        public static async Task GetEncounters()
        {
            var url = Sessions.BASE_URL + "SERVER_GET_ENCOUNTERS";
            var payload = CreateEncountersPayload();
            var response = await SendRequest(url, payload, 81);

            if (response != null)
            {
                ProcessEncountersResponse(response);
            }
        }

        private static Dictionary<string, object> CreateEncountersPayload()
        {
            return new Dictionary<string, object>
            {
                {"$gpb", "badoo.bma.BadooMessage"},
                {"body", new[]
                    {
                        new Dictionary<string, object>
                        {
                            {"message_type", 81},
                            {"server_get_encounters", new Dictionary<string, object>
                                {
                                    {"number", 50},
                                    {"context", 1},
                                    {"user_field_filter", new Dictionary<string, object>
                                        {
                                            {"projection", new[] { 210, 370, 200, 230, 490, 540, 530, 560, 291, 732, 890, 930, 662, 570, 380, 493, 1140, 1150, 1160, 1161 }},
                                            {"request_albums", new[]
                                                {
                                                    new Dictionary<string, object> { {"album_type", 7} },
                                                    new Dictionary<string, object> { {"album_type", 12}, {"external_provider", 12}, {"count", 8} }
                                                }
                                            },
                                            {"game_mode", 0},
                                            {"request_music_services", new Dictionary<string, object>
                                                {
                                                    {"top_artists_limit", 8},
                                                    {"supported_services", new[] { 29 }},
                                                    {"preview_image_size", new Dictionary<string, int> { {"width", 120}, {"height", 120} }}
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                },
                {"message_id", 8},
                {"message_type", 81},
                {"version", 1},
                {"is_background", false}
            };
        }

        private static void ProcessEncountersResponse(string responseBody)
        {
            var document = JObject.Parse(responseBody);
            var results = document["body"]?[0]?["client_encounters"]?["results"];

            if (results != null)
            {
                foreach (var result in results)
                {
                    if (result["$gpb"]?.ToString() == "badoo.bma.SearchResult")
                    {
                        var user = result["user"];
                        if (user != null)
                        {
                            string userId = user["user_id"]?.ToString();
                            string name = user["name"]?.ToString();
                            int age = user["age"]?.ToObject<int>() ?? -1;

                            // Save encounter to database
                            var encounter = new Encounter
                            {
                                UserId = userId,
                                Name = name,
                                Age = age,
                                Date = DateTime.UtcNow,
                                IsMatch = false
                            };
                            _context.Encounters.Add(encounter);
                            
                            // Update stats
                            _stats.TotalLikes++;
                            _stats.LastUpdated = DateTime.UtcNow;
                            
                            _context.SaveChanges();

                            AnsiConsole.MarkupLine($"[bold]User ID:[/] {userId}, [bold]Name:[/] {name}, [bold]Age:[/] {age}");

                            // Vote for each user
                            VoteForPerson(userId).Wait();
                        }
                    }
                }
            }
        }

        private static async Task VoteForPerson(string personId)
        {
            var url = Sessions.BASE_URL + "SERVER_ENCOUNTERS_VOTE";
            var payload = CreateVotePayload(personId);
            var response = await SendRequest(url, payload, 80);

            if (response != null)
            {
                ProcessVoteResponse(response, personId);
            }
        }

        private static Dictionary<string, object> CreateVotePayload(string personId)
        {
            return new Dictionary<string, object>
            {
                {"$gpb", "badoo.bma.BadooMessage"},
                {"body", new[]
                    {
                        new Dictionary<string, object>
                        {
                            {"message_type", 80},
                            {"server_encounters_vote", new Dictionary<string, object>
                                {
                                    {"person_id", personId},
                                    {"vote", 2},
                                    {"vote_source", 1},
                                    {"game_mode", 0}
                                }
                            }
                        }
                    }
                },
                {"message_id", 27},
                {"message_type", 80},
                {"version", 1},
                {"is_background", false}
            };
        }

        private static void ProcessVoteResponse(string responseBody, string personId)
        {
            var document = JObject.Parse(responseBody);
            var responsesCount = document["responses_count"]?.ToObject<int>() ?? 0;

            if (responsesCount == 1)
            {
                AnsiConsole.MarkupLine($"[bold green]Person ID:[/] {personId}, [bold green]Status:[/] Liked");
            }
            else if (responsesCount == 3)
            {
                AnsiConsole.MarkupLine($"[bold green]Person ID:[/] {personId}, [bold green]Status:[/] Match");
                
                // Update match status in database
                var encounter = _context.Encounters.FirstOrDefault(e => e.UserId == personId);
                if (encounter != null)
                {
                    encounter.IsMatch = true;
                    _stats.TotalMatches++;
                    _context.SaveChanges();
                }
            }
            else
            {
                AnsiConsole.MarkupLine($"[bold yellow]Person ID:[/] {personId}, [bold yellow]Responses Count:[/] {responsesCount}");
            }
        }

        public static async Task<(int totalMatches, List<string> userIds)> CheckMessages()
				{
				    var url = Sessions.BASE_URL + "SERVER_GET_USER_LIST";
				    var payload = CreateCheckMessagesPayload();
				    var response = await SendRequest(url, payload, 245);

				    List<string> userIds = new List<string>();
				    int totalMatches = 0;

				    if (response != null)
				    {
				        var document = JObject.Parse(response);
				        totalMatches = document["body"]?[0]?["client_user_list"]?["section"]?[0]?["total_count"]?.ToObject<int>() ?? 0;
				        var users = document["body"]?[0]?["client_user_list"]?["section"]?[0]?["users"];

				        if (users != null)
				        {
				            foreach (var user in users)
				            {
				                string userId = user["user_id"]?.ToString();
				                if (!string.IsNullOrEmpty(userId))
				                {
				                    userIds.Add(userId);
				                }
				            }
				        }
				    }

				    return (totalMatches, userIds);
				}

				public static async Task SendMessageToAllMatches(List<string> userIds, string message)
				{
				    foreach (var userId in userIds)
				    {
				        await SendMessage(userId, message);
				    }
				}

				private static async Task SendMessage(string userId, string message)
				{
				    var url = Sessions.BASE_URL + "SERVER_SEND_CHAT_MESSAGE";
				    var payload = new Dictionary<string, object>
				    {
				        {"$gpb", "badoo.bma.BadooMessage"},
				        {"body", new[]
				            {
				                new Dictionary<string, object>
				                {
				                    {"message_type", 104},
				                    {"chat_message", new Dictionary<string, object>
				                        {
				                            {"mssg", message},
				                            {"message_type", 1},
				                            {"uid", DateTime.UtcNow.Ticks.ToString()},
				                            {"from_person_id", Sessions.personId},  // Utilisez personId au lieu de USER_ID
				                            {"to_person_id", userId},
				                            {"read", false}
				                        }
				                    }
				                }
				            }
				        },
				        {"message_id", 16},
				        {"message_type", 104},
				        {"version", 1},
				        {"is_background", false}
				    };

				    var response = await SendRequest(url, payload, 104);
                    _stats.TotalMessagesSent++;
                    _context.SaveChanges();
				}

        private static Dictionary<string, object> CreateCheckMessagesPayload()
        {
            return new Dictionary<string, object>
            {
                {"$gpb", "badoo.bma.BadooMessage"},
                {"body", new[]
                    {
                        new Dictionary<string, object>
                        {
                            {"message_type", 245},
                            {"server_get_user_list", new Dictionary<string, object>
                                {
                                    {"user_field_filter", new Dictionary<string, object>
                                        {
                                            {"projection", new[] { 200, 210, 340, 230, 640, 580, 300, 860, 280, 590, 591, 250, 700, 762, 592, 880, 582, 930, 585, 583, 305, 330, 763, 1423, 584, 1262, 911, 912 }}
                                        }
                                    },
                                    {"preferred_count", 30},
                                    {"folder_id", 0}
                                }
                            }
                        }
                    }
                },
                {"message_id", 43},
                {"message_type", 245},
                {"version", 1},
                {"is_background", false}
            };
        }

        private static void ProcessCheckMessagesResponse(string responseBody)
        {
            var document = JObject.Parse(responseBody);
            var totalCount = document["body"]?[0]?["client_user_list"]?["section"]?[0]?["total_count"]?.ToObject<int>() ?? 0;
            var users = document["body"]?[0]?["client_user_list"]?["section"]?[0]?["users"];

            AnsiConsole.MarkupLine($"[bold]Total Matches:[/] {totalCount}");

            if (users != null)
            {
                foreach (var user in users)
                {
                    string userId = user["user_id"]?.ToString();
                    AnsiConsole.MarkupLine($"[bold]User ID:[/] {userId}");
                }
            }
        }

        private static async Task<string> SendRequest(string url, Dictionary<string, object> payload, int messageType)
        {
            var json = JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            string xPingback = CalculateBumbleChecksum(json);
            client.DefaultRequestHeaders.Remove("X-Pingback");
            client.DefaultRequestHeaders.Add("X-Pingback", xPingback);

            client.DefaultRequestHeaders.Remove("X-Message-Type");
            client.DefaultRequestHeaders.Add("X-Message-Type", messageType.ToString());

            var response = await client.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }

        private static string CalculateBumbleChecksum(string inputString)
        {
            inputString += "whitetelevisionbulbelectionroofhorseflying";
            using (var md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.ASCII.GetBytes(inputString);
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
        }

        public static async Task SendVoteAsync(string sessionCookie, string deviceId, string personId, int vote)
        {
            var url = Sessions.BASE_URL + "SERVER_ENCOUNTERS_VOTE";
            var payload = CreateVotePayload(personId);
            var response = await SendRequest(url, payload, 80);

            if (response != null)
            {
                ProcessVoteResponse(response, personId);
            }
        }

        private class BumbleStats
        {
            public List<EncounterData> Encounters { get; set; } = new List<EncounterData>();
            public int TotalLikes { get; set; }
            public int TotalMatches { get; set; }
            public int TotalMessagesSent { get; set; }
            public DateTime LastUpdated { get; set; }
            
            public class EncounterData
            {
                public string UserId { get; set; }
                public string Name { get; set; }
                public int Age { get; set; }
                public bool IsMatch { get; set; }
                public DateTime Date { get; set; }
            }
        }
    }
}