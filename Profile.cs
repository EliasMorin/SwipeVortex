using System;
using System.Threading.Tasks;
using System.IO;
using InstagramApiSharp.API;
using InstagramApiSharp.Classes.Models;
using Newtonsoft.Json;
using System.Collections.Generic;
using InstagramApiSharp;

namespace Instagram
{
    public class InstagramProfileScraper
    {
        private readonly IInstaApi _instaApi;
        private const int MAX_PAGES = 5;

        private void WriteColored(string prefix, string message, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.Write(prefix + " ");
            Console.WriteLine(message);
            Console.ResetColor();
        }

        private void WriteInfo(string message) => WriteColored("[-]", message, ConsoleColor.DarkYellow);
        private void WriteSuccess(string message) => WriteColored("[+]", message, ConsoleColor.DarkGreen);
        private void WriteError(string message) => WriteColored("[x]", message, ConsoleColor.DarkRed);

        public InstagramProfileScraper(IInstaApi instaApi)
        {
            _instaApi = instaApi;
        }

        public class ProfileData
        {
            public InstaUserInfo UserInfo { get; set; }
            public List<InstaUserShort> Followers { get; set; }
            public List<InstaUserShort> Following { get; set; }
            public List<InstaMedia> MediaItems { get; set; }
            public Dictionary<string, int> MediaStats { get; set; }
            public DateTime ScrapedAt { get; set; }
        }

        public async Task<ProfileData> GetProfileDataAsync()
        {
            try
            {
                var currentUser = await _instaApi.GetCurrentUserAsync();
                if (!currentUser.Succeeded)
                {
                    throw new Exception($"Failed to get current user: {currentUser.Info.Message}");
                }

                WriteInfo($"Collecting data for user: {currentUser.Value.UserName}");

                var profileData = new ProfileData
                {
                    ScrapedAt = DateTime.UtcNow
                };

                // Get user info
                var userInfo = await _instaApi.UserProcessor.GetUserInfoByUsernameAsync(currentUser.Value.UserName);
                profileData.UserInfo = userInfo.Value;
                WriteInfo("User info collected...");

                // Get followers
                var followers = await _instaApi.UserProcessor.GetUserFollowersAsync(currentUser.Value.UserName,
                    PaginationParameters.MaxPagesToLoad(MAX_PAGES));
                profileData.Followers = followers.Value;
                WriteInfo($"Collected {followers.Value.Count} followers...");

                // Get following
                var following = await _instaApi.UserProcessor.GetUserFollowingAsync(currentUser.Value.UserName,
                    PaginationParameters.MaxPagesToLoad(MAX_PAGES));
                profileData.Following = following.Value;
                WriteInfo($"Collected {following.Value.Count} following...");

                // Get media
                var media = await _instaApi.UserProcessor.GetUserMediaAsync(currentUser.Value.UserName,
                    PaginationParameters.MaxPagesToLoad(MAX_PAGES));
                profileData.MediaItems = media.Value;
                WriteInfo($"Collected {media.Value.Count} media items...");

                // Calculate media stats
                profileData.MediaStats = new Dictionary<string, int>
                {
                    { "TotalLikes", profileData.MediaItems.Sum(m => (int)m.LikesCount) },
                    { "PhotoCount", profileData.MediaItems.Count(m => m.MediaType == InstaMediaType.Image) },
                    { "VideoCount", profileData.MediaItems.Count(m => m.MediaType == InstaMediaType.Video) },
                    { "AlbumCount", profileData.MediaItems.Count(m => m.MediaType == InstaMediaType.Carousel) }
                };

                WriteSuccess("Data collection completed successfully!");
                return profileData;
            }
            catch (Exception ex)
            {
                WriteError($"Error collecting profile data: {ex.Message}");
                throw;
            }
        }

        public async Task SaveProfileDataAsync(ProfileData profileData)
        {
            try
            {
                var fileName = $"{profileData.UserInfo.Username}_profile.json";
                var json = JsonConvert.SerializeObject(profileData, Formatting.Indented,
                    new JsonSerializerSettings
                    {
                        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                        NullValueHandling = NullValueHandling.Ignore
                    });

                await File.WriteAllTextAsync(fileName, json);
                WriteSuccess($"Profile data saved to {fileName}");
            }
            catch (Exception ex)
            {
                WriteError($"Error saving profile data: {ex.Message}");
                throw;
            }
        }
    }
}