using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace SwipeVortexWb
{
    public class Happn
    {
        private static readonly HappnDbContext _context = new HappnDbContext();
        private static HappnStats _stats;

        static Happn()
        {
            // Initialize database and load stats
            _context.Database.EnsureCreated();
            _stats = _context.Stats.FirstOrDefault() ?? new HappnStats 
            { 
                TotalLikes = 0,
                TotalMatches = 0,
                TotalMessagesSent = 0,
                TotalConversations = 0,
                TotalCrushes = 0,
                LastUpdated = DateTime.UtcNow 
            };
            if (_stats.Id == 0)
            {
                _context.Stats.Add(_stats);
                _context.SaveChanges();
            }
        }

        static async public Task GetUserInfo(string token)
        {
            string fields = "id,audios,first_name,last_name,gender,gender_alias,age,about,job,workplace,school,modification_date,is_moderator,is_admin,type,status,last_position_update,register_date,sensitive_traits_preferences,mysterious_mode_preferences,residence_city,teaser_answers,picture.mode(1).width(1400).height(1600).fields(id,is_default,url,width,height),profiles.mode(1).width(1400).height(1600).fields(id,is_default,url,width,height),spotify_tracks,social_synchronization.fields(facebook,vk,apple_sign_in,instagram,google_sign_in),traits,traits_v2,verification.fields(status,reason),unread_conversations,unread_notifications,is_premium,user_balance,credits,subscription_level,matching_preferences.fields(matching_traits.fields(enabled,traits)),location_preferences,mysterious_mode_preferences,marketing_preferences,notification_settings,biometric_preferences,last_tos_version_accepted,last_sdc_version_accepted,last_cookie_v1_version_accepted,last_cookie_v2_version_accepted,last_cookie_v3_version_accepted,location_city,residence_city,pending_likers,login,device_info,first_ip,country,language";

            using (var client = new HttpClient())
            {
                SetupHttpClient(client, token);

                var response = await client.GetAsync($"https://api.happn.fr/api/users/me?fields={Uri.EscapeDataString(fields)}");

                // Les résultats ne sont plus affichés
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"User Info Error: {response.StatusCode}");
                }
            }
        }

        static async public Task GetAndReactToRecommendations(string token, bool likeEveryone)
        {
            string fields = "type,content.fields(crossing_nb_times,position.fields(lat,lon,modification_date),user.fields(id,audios,first_name,last_name,gender,gender_alias,age,about,job,workplace,school,modification_date,is_moderator,is_admin,type,status,last_position_update,register_date,sensitive_traits_preferences,mysterious_mode_preferences,residence_city,teaser_answers,picture.mode(1).width(1400).height(1600).fields(id,is_default,url,width,height),profiles.mode(1).width(1400).height(1600).fields(id,is_default,url,width,height),spotify_tracks,social_synchronization.fields(facebook,vk,apple_sign_in,instagram,google_sign_in),traits,traits_v2,verification.fields(status,reason),unread_conversations,unread_notifications,is_premium,user_balance,credits,subscription_level,matching_preferences.fields(matching_traits.fields(enabled,traits)),location_preferences,mysterious_mode_preferences,marketing_preferences,notification_settings,biometric_preferences,last_tos_version_accepted,last_sdc_version_accepted,last_cookie_v1_version_accepted,last_cookie_v2_version_accepted,last_cookie_v3_version_accepted,location_city,residence_city,pending_likers,login,device_info,first_ip,country,language))";
            string scrollIdFrom = "";

            using (var client = new HttpClient())
            {
                SetupHttpClient(client, token);

                var uriBuilder = new UriBuilder("https://api.happn.fr/api/v1/users/me/recommendations");
                var query = HttpUtility.ParseQueryString(uriBuilder.Query);
                query["fields"] = fields;
                query["scroll_id_from"] = scrollIdFrom;
                uriBuilder.Query = query.ToString();

                var response = await client.GetAsync(uriBuilder.Uri);

                await HandleRecommendationsResponse(response, client, token, likeEveryone);
            }
        }

        static void SetupHttpClient(HttpClient client, string token)
        {
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"OAuth=\"{token}\"");
            client.DefaultRequestHeaders.Add("x-happn-cid", "285deb5a-ee33-422f-ba32-cb4de3663810");
            client.DefaultRequestHeaders.Add("x-happn-did", "24711c12-8e0d-435c-b582-069849ebcc12");
            client.DefaultRequestHeaders.Add("x-happn-version", "happn-webapp/2024.9.1");
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (X11; CrOS x86_64 14541.0.0) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36");
        }

        static async Task HandleRecommendationsResponse(HttpResponseMessage response, HttpClient client, string token, bool likeEveryone)
        {
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var jsonResponse = JObject.Parse(content);

                var recommendations = jsonResponse["data"].ToObject<JArray>();

                foreach (var recommendation in recommendations)
                {
                    if (recommendation["content"]?["user"] != null)
                    {
                        var user = recommendation["content"]["user"];
                        var id = user["id"].Value<string>();
                        var firstName = user["first_name"].Value<string>();
                        var age = user["age"].Value<int>();
                        var gender = user["gender"].Value<string>();

                        string residenceCity = "Unknown";
                        if (user["residence_city"] != null && user["residence_city"]["city"] != null)
                        {
                            residenceCity = user["residence_city"]["city"].Value<string>();
                        }

                        // Save encounter to database
                        var encounter = new HappnEncounter
                        {
                            UserId = id,
                            FirstName = firstName,
                            Age = age,
                            Gender = gender,
                            ResidenceCity = residenceCity,
                            Date = DateTime.UtcNow,
                            IsMatch = false
                        };
                        Console.WriteLine($"Adding encounter: {firstName}, {age}, {gender}");
                        _context.Encounters.Add(encounter);
                        
                        // Update stats
                        _stats.TotalLikes++;
                        _stats.LastUpdated = DateTime.UtcNow;
                        
                        _context.SaveChanges();
                        Console.WriteLine($"Encounter added successfully. Total encounters: {_context.Encounters.Count()}");

                        Console.WriteLine($"ID: {id}");
                        Console.WriteLine($"First Name: {firstName}");
                        Console.WriteLine($"Age: {age}");
                        Console.WriteLine($"Gender: {gender}");
                        Console.WriteLine($"Residence City: {residenceCity}");

                        if (likeEveryone)
                        {
                            var likeResponse = await SendHeartReaction(client, token, id);
                            if (likeResponse.HasLikedMe)
                            {
                                // Update encounter as a match
                                encounter.IsMatch = true;
                                _stats.TotalMatches++;
                                _context.SaveChanges();
                            }
                        }

                        Console.WriteLine("--------------------");
                    }
                }
            }
            else
            {
                Console.WriteLine($"Recommendations Error: {response.StatusCode}");
            }
        }

        static async Task<(bool HasLikedMe, int RemainingLikes)> SendHeartReaction(HttpClient client, string token, string userId)
        {
            var url = $"https://api.happn.fr/api/v1/users/me/reacted/{userId}";
            var payload = new
            {
                reaction = new { id = "heart" },
                container = new { type = "PHOTO", content = new { id = "a57bc650-e46b-11ee-b8b5-e7afd455b97b" } },
                tracking_custom_data = new { reaction_index = 0, container_index = 0, content_index = 0 }
            };

            var jsonPayload = Newtonsoft.Json.JsonConvert.SerializeObject(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(url, content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var jsonResponse = JObject.Parse(responseContent);

                bool hasLikedMe = jsonResponse["data"]["has_liked_me"].Value<bool>();
                int remainingLikes = jsonResponse["data"]["total_remaining_likes"].Value<int>();

                return (hasLikedMe, remainingLikes);
            }
            else
            {
                Console.WriteLine($"Heart Reaction Error: {response.StatusCode}");
                return (false, 0);
            }
        }

        static async public Task<List<string>> GetConversations(string token)
        {
            string fields = "id,creation_date,modification_date,is_read,is_disabled,last_message.fields(message,sender.fields(id,is_moderator)),participants.fields(id,status,last_read_date_time,user.fields(age,gender,gender_alias,residence_city,modification_date,first_name,picture.mode(1).width(160).height(160).fields(id,is_default,url,width,height),is_moderator))";
            var conversationIds = new List<string>();

            using (var client = new HttpClient())
            {
                SetupHttpClient(client, token);

                var uriBuilder = new UriBuilder("https://api.happn.fr/api/users/me/conversations");
                var query = HttpUtility.ParseQueryString(uriBuilder.Query);
                query["fields"] = fields;
                query["view_id"] = "ongoing";
                query["limit"] = "20";
                uriBuilder.Query = query.ToString();

                var response = await client.GetAsync(uriBuilder.Uri);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var jsonResponse = JObject.Parse(content);

                    Console.WriteLine("\n=== Active Conversations ===\n");
                    var conversations = jsonResponse["data"]?.ToObject<JArray>();

                    if (conversations != null)
                    {
                        foreach (var conversation in conversations)
                        {
                            try
                            {
                                string conversationId = conversation["id"]?.ToString() ?? "N/A";
                                conversationIds.Add(conversationId);
                                string creationDate = conversation["creation_date"]?.ToString() ?? "N/A";
                                bool isRead = conversation["is_read"]?.Value<bool>() ?? false;

                                Console.WriteLine($"Conversation ID: {conversationId}");
                                Console.WriteLine($"Created on: {creationDate}");
                                Console.WriteLine($"Is Read: {isRead}");

                                var lastMessage = conversation["last_message"];
                                if (lastMessage != null && lastMessage.Type != JTokenType.Null)
                                {
                                    string messageText = lastMessage["message"]?.ToString() ?? "N/A";
                                    string senderId = lastMessage["sender"]?["id"]?.ToString() ?? "N/A";
                                    bool isSenderModerator = lastMessage["sender"]?["is_moderator"]?.Value<bool>() ?? false;

                                    Console.WriteLine($"Last Message: {messageText}");
                                    Console.WriteLine($"Sender ID: {senderId}");
                                    Console.WriteLine($"Sender is Moderator: {isSenderModerator}");
                                }
                                else
                                {
                                    Console.WriteLine("No last message available.");
                                }

                                Console.WriteLine("\nParticipants:");
                                var participants = conversation["participants"]?.ToObject<JArray>();
                                if (participants != null)
                                {
                                    foreach (var participant in participants)
                                    {
                                        var user = participant["user"];
                                        if (user != null && user.Type != JTokenType.Null)
                                        {
                                            string firstName = user["first_name"]?.ToString() ?? "N/A";
                                            int age = user["age"]?.Value<int>() ?? 0;
                                            string gender = user["gender"]?.ToString() ?? "N/A";

                                            Console.WriteLine($"- Name: {firstName}, Age: {age}, Gender: {gender}");

                                            var residenceCity = user["residence_city"];
                                            if (residenceCity != null && residenceCity.Type != JTokenType.Null && residenceCity["city"] != null)
                                            {
                                                string city = residenceCity["city"].ToString();
                                                Console.WriteLine($"  City: {city}");
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("No participant information available.");
                                }

                                Console.WriteLine("\n" + new string('-', 40) + "\n");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error processing conversation: {ex.Message}");
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("No conversations found.");
                    }
                }
                else
                {
                    Console.WriteLine($"Conversations Error: {response.StatusCode}");
                }
            }

            // Update stats
            _stats.TotalConversations = conversationIds.Count;
            _stats.LastUpdated = DateTime.UtcNow;
            _context.SaveChanges();

            return conversationIds;
        }

        static async public Task<List<string>> GetCrushes(string token)
        {
            string fields = "id,creation_date,modification_date,is_read,is_disabled,last_message.fields(message,sender.fields(id,is_moderator)),participants.fields(id,status,last_read_date_time,user.fields(age,gender,gender_alias,residence_city,modification_date,first_name,picture.mode(1).width(160).height(160).fields(id,is_default,url,width,height),is_moderator))";
            var crushIds = new List<string>();

            using (var client = new HttpClient())
            {
                SetupHttpClient(client, token);

                var uriBuilder = new UriBuilder("https://api.happn.fr/api/users/me/conversations");
                var query = HttpUtility.ParseQueryString(uriBuilder.Query);
                query["fields"] = fields;
                query["view_id"] = "pending";
                query["limit"] = "20";
                uriBuilder.Query = query.ToString();

                var response = await client.GetAsync(uriBuilder.Uri);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var jsonResponse = JObject.Parse(content);

                    Console.WriteLine("\n=== Your Crushes ===\n");
                    var conversations = jsonResponse["data"]?.ToObject<JArray>();

                    if (conversations != null && conversations.Count > 0)
                    {
                        foreach (var conversation in conversations)
                        {
                            try
                            {
                                string conversationId = conversation["id"]?.ToString() ?? "N/A";
                                crushIds.Add(conversationId);
                                string creationDate = conversation["creation_date"]?.ToString() ?? "N/A";
                                bool isRead = conversation["is_read"]?.Value<bool>() ?? false;

                                Console.WriteLine($"Conversation ID: {conversationId}");
                                Console.WriteLine($"Created on: {creationDate}");
                                Console.WriteLine($"Is Read: {isRead}");

                                var lastMessage = conversation["last_message"];
                                if (lastMessage != null && lastMessage.Type != JTokenType.Null)
                                {
                                    string messageText = lastMessage["message"]?.ToString() ?? "No message";
                                    string senderId = lastMessage["sender"]?["id"]?.ToString() ?? "N/A";
                                    bool isSenderModerator = lastMessage["sender"]?["is_moderator"]?.Value<bool>() ?? false;

                                    Console.WriteLine($"Last Message: {messageText}");
                                    Console.WriteLine($"Sender ID: {senderId}");
                                    Console.WriteLine($"Sender is Moderator: {isSenderModerator}");
                                }
                                else
                                {
                                    Console.WriteLine("No last message available.");
                                }

                                Console.WriteLine("\nParticipants:");
                                var participants = conversation["participants"]?.ToObject<JArray>();
                                if (participants != null)
                                {
                                    foreach (var participant in participants)
                                    {
                                        var user = participant["user"];
                                        if (user != null && user.Type != JTokenType.Null)
                                        {
                                            string firstName = user["first_name"]?.ToString() ?? "N/A";
                                            int age = user["age"]?.Value<int>() ?? 0;
                                            string gender = user["gender"]?.ToString() ?? "N/A";

                                            Console.WriteLine($"- Name: {firstName}, Age: {age}, Gender: {gender}");

                                            var residenceCity = user["residence_city"];
                                            if (residenceCity != null && residenceCity.Type != JTokenType.Null && residenceCity["city"] != null)
                                            {
                                                string city = residenceCity["city"].ToString();
                                                Console.WriteLine($"  City: {city}");
                                            }

                                            var picture = user["picture"];
                                            if (picture != null && picture.Type != JTokenType.Null && picture["url"] != null)
                                            {
                                                string pictureUrl = picture["url"].ToString();
                                                Console.WriteLine($"  Picture URL: {pictureUrl}");
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("No participant information available.");
                                }

                                Console.WriteLine("\n" + new string('-', 40) + "\n");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error processing crush: {ex.Message}");
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("No crushes found.");
                    }
                }
                else
                {
                    Console.WriteLine($"Crushes Error: {response.StatusCode}");
                }
            }

            // Update stats
             _stats.TotalCrushes = crushIds.Count;
            _stats.LastUpdated = DateTime.UtcNow;
             _context.SaveChanges();

            return crushIds;
        }

        public static async Task SendGenericMessageToAllConversations(string token, List<string> conversationIds, List<string> crushIds, string genericMessage)
        {
            using (var client = new HttpClient())
            {
                SetupHttpClient(client, token);

                var allConversations = new List<string>();
                allConversations.AddRange(conversationIds);
                allConversations.AddRange(crushIds);

                foreach (var conversationId in allConversations)
                {
                    var url = $"https://api.happn.fr/api/conversations/{conversationId}/messages";
                    var payload = new
                    {
                        message = genericMessage,
                        conversation_id = conversationId
                    };

                    var jsonPayload = Newtonsoft.Json.JsonConvert.SerializeObject(payload);
                    var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                    var response = await client.PostAsync(url, content);

                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"Message sent successfully to conversation {conversationId}");

                        // Update stats
                        _stats.TotalMessagesSent++;
                        _context.SaveChanges();
                    }
                    else
                    {
                        Console.WriteLine($"Failed to send message to conversation {conversationId}. Status: {response.StatusCode}");
                    }
                }
            }
        }
    }
}

