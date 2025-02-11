using System;
using System.Threading.Tasks;
using InstagramApiSharp;
using InstagramApiSharp.API;
using InstagramApiSharp.Classes.Models;
using InstagramApiSharp.Classes;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using Catalyst;
using Catalyst.Models;
using Mosaik.Core;
using Newtonsoft.Json.Linq;
using System.Reflection.Metadata;
using Microsoft.EntityFrameworkCore; // This adds the async extension methods
using System.Threading.Tasks; // For async operations

namespace SwipeVortexWb.Instagram
{
    public class InstagramHashtagScraper
    {
        private readonly IInstaApi _instaApi;
        private readonly Pipeline _nlp;
        private readonly HashSet<string> _fashionKeywords;
        private readonly Dictionary<string, HashSet<string>> _fashionWordGroups;
        private readonly TimeSpan _delayBetweenPosts = TimeSpan.FromSeconds(60);
        private readonly InstagramDbContext _context;

        public InstagramHashtagScraper(IInstaApi instaApi, InstagramDbContext context)
        {
            _instaApi = instaApi;
            _context = context;

            // Initialize NLP pipeline for English
            Catalyst.Models.English.Register();
            Storage.Current = new DiskStorage("catalyst-models");
            _nlp = Pipeline.For(Language.English);

            // Define semantic fashion word groups
            _fashionWordGroups = new Dictionary<string, HashSet<string>>
            {
                {
                    "clothing", new HashSet<string> {
                        "dress", "shirt", "pants", "jacket", "coat",
                        "skirt", "blouse", "sweater", "jeans", "outfit",
                        "wear", "wearing", "worn", "clothes", "clothing"
                    }
                },
                {
                    "style", new HashSet<string> {
                        "fashion", "style", "trend", "trendy", "stylish",
                        "chic", "elegant", "casual", "sophisticated", "glamorous",
                        "aesthetic", "look", "vogue", "couture"
                    }
                },
                {
                    "accessories", new HashSet<string> {
                        "bag", "shoes", "jewelry", "watch", "sunglasses",
                        "belt", "scarf", "hat", "purse", "necklace",
                        "bracelet", "earrings", "accessories"
                    }
                },
                {
                    "fashion_industry", new HashSet<string> {
                        "model", "designer", "brand", "luxury", "collection",
                        "runway", "fashion_week", "photoshoot", "editorial",
                        "fashionista", "influencer", "stylist"
                    }
                },
                {
                    "beauty", new HashSet<string> {
                        "makeup", "beauty", "cosmetics", "skincare", "hair",
                        "hairstyle", "glamour", "gorgeous", "stunning", "beautiful"
                    }
                }
            };

            // Combine all keywords into _fashionKeywords
            _fashionKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var group in _fashionWordGroups.Values)
            {
                _fashionKeywords.UnionWith(group);
            }
        }

        private double CalculateSentenceImportance(IEnumerable<IToken> tokens)
        {
            // Calculate importance based on tokens length
            const double MIN_LENGTH = 3;
            const double MAX_LENGTH = 50;

            int length = tokens.Count();

            if (length < MIN_LENGTH) return 0.5;
            if (length > MAX_LENGTH) return 0.7;

            return 1.0; // Optimal length
        }

        private (double relevance, List<MatchResult>) CalculateTopicRelevance(string text)
        {
            if (string.IsNullOrEmpty(text))
                return (0, new List<MatchResult>());

            var doc = new Catalyst.Document(text, Language.English);
            _nlp.ProcessSingle(doc);

            var matches = new List<MatchResult>();
            var processedWords = new HashSet<string>();
            var weightedScores = new Dictionary<string, double>();

            // Initialize category scores
            foreach (var category in _fashionWordGroups.Keys)
            {
                weightedScores[category] = 0.0;
            }

            // Weight factors
            const double DIRECT_MATCH_WEIGHT = 1.0;
            const double LEMMA_MATCH_WEIGHT = 0.85;
            const double SEMANTIC_MATCH_WEIGHT = 0.7;
            const double CONTEXT_BOOST = 1.2;

            // Process each token in the document
            var allTokens = doc.SelectMany(s => s).ToList();
            var sentenceWords = allTokens.Select(t => t.Value.ToLower()).ToList();
            var sentenceImportance = CalculateSentenceImportance(allTokens);

            foreach (var token in allTokens)
            {
                var word = token.Value.ToLower();
                if (processedWords.Contains(word))
                    continue;

                processedWords.Add(word);

                // Direct matches
                if (_fashionKeywords.Contains(word))
                {
                    var contextScore = CalculateContextScore(sentenceWords, word) * CONTEXT_BOOST;
                    var matchScore = DIRECT_MATCH_WEIGHT * contextScore * sentenceImportance;
                    UpdateCategoryScores(word, matchScore, weightedScores);
                    matches.Add(new MatchResult(word, word, MatchType.Direct, (float)matchScore));
                    continue;
                }

                // Lemma matches
                var lemma = token.Lemma?.ToLower();
                if (!string.IsNullOrEmpty(lemma) && _fashionKeywords.Contains(lemma))
                {
                    var contextScore = CalculateContextScore(sentenceWords, lemma) * CONTEXT_BOOST;
                    var matchScore = LEMMA_MATCH_WEIGHT * contextScore * sentenceImportance;
                    UpdateCategoryScores(lemma, matchScore, weightedScores);
                    matches.Add(new MatchResult(word, lemma, MatchType.Lemma, (float)matchScore));
                    continue;
                }

                // Semantic matches
                foreach (var group in _fashionWordGroups)
                {
                    if (group.Value.Contains(word) || (lemma != null && group.Value.Contains(lemma)))
                    {
                        var contextScore = CalculateContextScore(sentenceWords, word) * CONTEXT_BOOST;
                        var matchScore = SEMANTIC_MATCH_WEIGHT * contextScore * sentenceImportance;
                        weightedScores[group.Key] += matchScore;
                        matches.Add(new MatchResult(word, group.Key, MatchType.Semantic, (float)matchScore));
                        break;
                    }
                }
            }

            // Calculate final relevance score
            double totalScore = weightedScores.Values.Sum();
            double maxPossibleScore = _fashionKeywords.Count * DIRECT_MATCH_WEIGHT * CONTEXT_BOOST;
            double normalizedRelevance = Math.Min(totalScore / maxPossibleScore, 1.0);

            return (normalizedRelevance, matches);
        }

        private double CalculateContextScore(List<string> sentenceWords, string targetWord)
        {
            const int CONTEXT_WINDOW = 3;
            int wordIndex = sentenceWords.IndexOf(targetWord);

            if (wordIndex == -1) return 1.0;

            int contextWordsCount = 0;
            double contextScore = 1.0;

            // Look at surrounding words within the context window
            for (int i = Math.Max(0, wordIndex - CONTEXT_WINDOW);
                 i < Math.Min(sentenceWords.Count, wordIndex + CONTEXT_WINDOW + 1);
                 i++)
            {
                if (i == wordIndex) continue;

                if (_fashionKeywords.Contains(sentenceWords[i]))
                {
                    contextWordsCount++;
                    contextScore += 0.2; // Boost for each fashion-related word in context
                }
            }

            return contextScore;
        }

        private void UpdateCategoryScores(string word, double score, Dictionary<string, double> weightedScores)
        {
            foreach (var category in _fashionWordGroups)
            {
                if (category.Value.Contains(word))
                {
                    weightedScores[category.Key] += score;
                    break;
                }
            }
        }

        private async Task<EngagementMetrics> CalculateEngagementMetrics(InstaMedia media)
        {
            try
            {
                // Fetch user profile to get actual follower count
                var userProfile = await _instaApi.UserProcessor.GetUserInfoByUsernameAsync(media.User.UserName);
                if (!userProfile.Succeeded)
                {
                    throw new Exception($"Failed to get user profile: {userProfile.Info.Message}");
                }
                var followerCount = userProfile.Value.FollowerCount;

                // Pour les impressions, utiliser le total des likes + comments comme estimation minimale
                // si ViewCount n'est pas disponible
                var impressionsCount = media.ViewCount > 0
                    ? media.ViewCount
                    : media.LikesCount + (long.TryParse(media.CommentsCount, out long commentsCount) ? commentsCount : 0);

                // Calcul du taux de pénétration
                var penetrationRate = followerCount > 0
                    ? ((double)impressionsCount / followerCount) * 100
                    : 0;

                return new EngagementMetrics
                {
                    FollowerCount = followerCount,
                    ImpressionsCount = impressionsCount,
                    PenetrationRate = Math.Round(penetrationRate, 2)
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calculating engagement metrics: {ex.Message}");
                // En cas d'erreur, retourner des métriques calculées avec les données disponibles
                return new EngagementMetrics
                {
                    FollowerCount = 0,
                    ImpressionsCount = media.LikesCount + (long.TryParse(media.CommentsCount ?? "0", out long commentsCount) ? commentsCount : 0), // Estimation minimale
                    PenetrationRate = 0
                };
            }
        }

        private double CalculatePenetrationRate(long impressions, long followers)
        {
            if (followers == 0) return 0;
            return (double)impressions / followers * 100;
        }

        private double CalculateFinalScore(double topicRelevance, EngagementMetrics metrics)
        {
            // Weights
            const double TOPIC_WEIGHT = 0.7;
            const double PENETRATION_WEIGHT = 0.3;

            // Calculate weighted scores
            double weightedTopicScore = topicRelevance * TOPIC_WEIGHT;
            double weightedPenetrationScore = (metrics.PenetrationRate / 100.0) * PENETRATION_WEIGHT;

            // Combine scores
            return (weightedTopicScore + weightedPenetrationScore) * 100;
        }

        public async Task ProcessHashtagComplete(string hashtag, CancellationToken cancellationToken)
        {
            try
            {
                Console.OutputEncoding = System.Text.Encoding.UTF8;
                Console.WriteLine("\n=== Starting Complete Hashtag Analysis ===\n");

                // 1. Basic hashtag information search and display
                hashtag = hashtag.TrimStart('#');
                var searchResult = await _instaApi.HashtagProcessor.SearchHashtagAsync(hashtag, null, null);

                if (!searchResult.Succeeded || !searchResult.Value.Any())
                {
                    Console.WriteLine($"\nNo results found for #{hashtag} or error: {searchResult.Info.Message}");
                    return;
                }

                var hashtagInfo = searchResult.Value.First();
                Console.WriteLine($"\nHashtag Information for #{hashtagInfo.Name}:");
                Console.WriteLine("================================");
                Console.WriteLine($"ID: {hashtagInfo.Id}");
                Console.WriteLine($"Name: {hashtagInfo.Name}");
                Console.WriteLine($"Post Count: {hashtagInfo.MediaCount:N0}");

                Console.WriteLine("\nSimilar Hashtags:");
                foreach (var relatedTag in searchResult.Value.Skip(1).Take(5))
                {
                    Console.WriteLine($"- #{relatedTag.Name} ({relatedTag.MediaCount:N0} posts)");
                }

                // 2. Get popular media
                var paginationParameters = PaginationParameters.MaxPagesToLoad(1);
                var result = await _instaApi.HashtagProcessor.GetTopHashtagMediaListAsync(hashtag, paginationParameters);

                if (!result.Succeeded || result.Value?.Medias == null)
                {
                    Console.WriteLine($"\nError retrieving media: {result.Info.Message}");
                    return;
                }

                // 3. Data preparation for analysis
                var hashtagData = new HashtagMediaData
                {
                    Hashtag = hashtag,
                    ScrapeDate = DateTime.UtcNow,
                    Medias = new List<MediaInfo>(),
                    RelatedHashtags = searchResult.Value.Skip(1)
                        .Select(tag => new RelatedHashtagInfo { Name = tag.Name })
                        .ToList()
                };

                // 4. Analyze posts with combined metrics
                var postAnalyses = new List<PostAnalysisData>();

                Console.WriteLine("\nPopular Posts Analysis:");
                Console.WriteLine("=====================");

                int postCount = 0;
                foreach (var media in result.Value.Medias)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    postCount++;
                    Console.WriteLine($"\nAnalyzing post {postCount} of {result.Value.Medias.Count}...");

                    // Basic media info display
                    Console.WriteLine("\n--- Post Details ---");
                    Console.WriteLine($"Type: {media.MediaType}");
                    Console.WriteLine($"Code: {media.Code}");
                    Console.WriteLine($"Posted: {media.TakenAt}");
                    Console.WriteLine($"Likes: {media.LikesCount:N0}");
                    Console.WriteLine($"Comments: {media.CommentsCount:N0}");

                    if (media.User != null)
                    {
                        Console.WriteLine("\nAuthor Information:");
                        Console.WriteLine($"- Username: {media.User.UserName}");
                        Console.WriteLine($"- Full Name: {media.User.FullName}");
                        Console.WriteLine($"- Verified: {(media.User.IsVerified ? "Yes" : "No")}");
                    }

                    // Calculate engagement metrics
                    var metrics = await CalculateEngagementMetrics(media);
                    Console.WriteLine("\nEngagement Metrics:");
                    Console.WriteLine($"- Followers: {metrics.FollowerCount:N0}");
                    Console.WriteLine($"- Impressions: {metrics.ImpressionsCount:N0}");
                    Console.WriteLine($"- Penetration Rate: {metrics.PenetrationRate:F2}%");

                    // NLP Analysis
                    var captionText = media.Caption?.Text ?? "";
                    var (topicRelevance, matchResults) = CalculateTopicRelevance(captionText);

                    // Calculate category scores
                    var categoryScores = new Dictionary<string, double>();
                    foreach (var category in _fashionWordGroups.Keys)
                    {
                        var categoryMatches = matchResults.Where(m =>
                            m.MatchedTerm == category ||
                            _fashionWordGroups[category].Contains(m.OriginalWord.ToLower()) ||
                            _fashionWordGroups[category].Contains(m.MatchedTerm.ToLower())
                        );

                        categoryScores[category] = categoryMatches.Any()
                            ? categoryMatches.Average(m => m.Similarity)
                            : 0.0;
                    }

                    // Calculate final score with new weights
                    double finalScore = CalculateFinalScore(topicRelevance, metrics);

                    postAnalyses.Add(new PostAnalysisData
                    {
                        MediaCode = media.Code,
                        UserName = media.User?.UserName,
                        Caption = captionText,
                        TopicRelevance = topicRelevance,
                        PenetrationRate = metrics.PenetrationRate,
                        LikesCount = metrics.LikesCount,
                        CommentsCount = metrics.CommentsCount,
                        FollowerCount = metrics.FollowerCount,
                        ImpressionsCount = metrics.ImpressionsCount,
                        FinalScore = finalScore,
                        MatchedKeywords = matchResults.Select(m => m.OriginalWord).ToList(),
                        PostUrl = $"https://instagram.com/p/{media.Code}",
                        CategoryScores = categoryScores,
                        MatchResults = matchResults
                    });

                    // Add to original media collection
                    hashtagData.Medias.Add(new MediaInfo
                    {
                        Code = media.Code,
                        MediaType = media.MediaType.ToString(),
                        TakenAt = media.TakenAt,
                        LikesCount = metrics.LikesCount,
                        CommentsCount = metrics.CommentsCount,
                        User = media.User != null ? new UserInfo
                        {
                            UserName = media.User.UserName,
                            FullName = media.User.FullName,
                            IsVerified = media.User.IsVerified
                        } : null,
                        Caption = media.Caption != null ? new CaptionInfo
                        {
                            Text = media.Caption.Text
                        } : null
                    });

                    // Création et sauvegarde immédiate de l'entrée en base de données
                    var userData = await SaveOrUpdateUserData(media.User, metrics.FollowerCount);
                    var mediaData = await SaveMediaData(media, userData, topicRelevance, metrics, finalScore);
                    await SaveCategoryScores(mediaData, categoryScores);

                     // Ajouter un délai entre chaque post
                    if (postCount < result.Value.Medias.Count)
                    {
                        Console.WriteLine($"\nWaiting {_delayBetweenPosts.TotalSeconds} seconds before next post...");
                        await Task.Delay(_delayBetweenPosts);
                    }
                }

                // 5. Display Top 10 posts by combined score
                Console.WriteLine("\nTop 10 Posts by Combined Score (70% Topic Relevance, 20% Engagement, 10% Penetration):");
                Console.WriteLine("=================================================================");

                var topPosts = postAnalyses
                    .OrderByDescending(p => p.FinalScore)
                    .Take(10);

                await SaveTopPostsAnalysis(hashtag, topPosts.ToList());

                foreach (var (post, index) in topPosts.Select((p, i) => (p, i)))
                {
                    Console.WriteLine($"\n{index + 1}. Post by @{post.UserName}");
                    Console.WriteLine($"Final Score: {post.FinalScore:F2}");
                    Console.WriteLine($"Topic Relevance: {post.TopicRelevance:P2} (Weight: 70%)");
                    Console.WriteLine($"Penetration Rate: {post.PenetrationRate:F2}% (Weight: 10%)");
                    Console.WriteLine($"Followers: {post.FollowerCount:N0}");
                    Console.WriteLine($"Impressions: {post.ImpressionsCount:N0}");
                    Console.WriteLine($"Interactions: {post.LikesCount:N0} likes, {post.CommentsCount:N0} comments");

                    Console.WriteLine("\nCategory Scores:");
                    foreach (var score in post.CategoryScores.OrderByDescending(s => s.Value))
                    {
                        Console.WriteLine($"- {score.Key}: {score.Value:P2}");
                    }

                    Console.WriteLine("\nMatched Terms:");
                    foreach (var match in post.MatchResults.Take(5))
                    {
                        Console.WriteLine($"- {match}");
                    }

                    if (post.MatchResults.Count > 5)
                    {
                        Console.WriteLine($"  (and {post.MatchResults.Count - 5} more...)");
                    }

                    Console.WriteLine($"\nURL: {post.PostUrl}");
                    if (!string.IsNullOrEmpty(post.Caption))
                    {
                        Console.WriteLine($"Caption Preview: {post.Caption.Substring(0, Math.Min(100, post.Caption.Length))}...");
                    }
                    Console.WriteLine(new string('-', 60));
                }

                // À la fin de ProcessHashtagComplete, après l'affichage des top posts
                Console.WriteLine("\nVoulez-vous envoyer des messages aux commentateurs des meilleurs posts ? (O/N)");
                if (Console.ReadLine()?.Trim().ToUpper() == "O")
                {
                    await SendDirectMessagesToTopPostsCommenters(topPosts.ToList());
                }

                // 6. Export data
                string fileName = $"hashtag_{hashtag}_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var exportData = new
                {
                    HashtagData = hashtagData,
                    NLPAnalysis = new
                    {
                        TopPosts = topPosts,
                        Keywords = _fashionKeywords.ToList()
                    }
                };

                string jsonString = JsonSerializer.Serialize(exportData, options);
                await File.WriteAllTextAsync(fileName, jsonString);

                Console.WriteLine($"\nData exported to file: {fileName}");
                Console.WriteLine("\n=== Complete Analysis Finished ===");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error during processing: {ex.Message}");
                Console.ResetColor();
                throw;
            }
        }

        private async Task SaveTopPostsAnalysis(string hashtag, List<PostAnalysisData> topPosts)
        {
            var topPostsAnalysis = new TopPostsAnalysis
            {
                Hashtag = hashtag,
                AnalysisDate = DateTime.UtcNow,
                TopMediaDataIds = topPosts.Select(p => 
                    _context.Medias.FirstOrDefault(m => m.MediaCode == p.MediaCode)?.Id ?? 0
                ).ToList(),
                AnalysisRank = 1, // Vous pouvez implémenter un système de séquencement si nécessaire
                AverageFinalScore = topPosts.Average(p => p.FinalScore),
                TotalImpressions = topPosts.Sum(p => p.ImpressionsCount),
                TotalLikes = topPosts.Sum(p => p.LikesCount)
            };

            _context.TopPostsAnalyses.Add(topPostsAnalysis);
            await _context.SaveChangesAsync();
        }

        private async Task<UserData> SaveOrUpdateUserData(InstaUser instaUser, long followerCount)
        {
            if (instaUser == null) return null;

            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.UserName == instaUser.UserName);

            if (existingUser == null)
            {
                existingUser = new UserData
                {
                    UserName = instaUser.UserName,
                    FullName = instaUser.FullName,
                    IsVerified = instaUser.IsVerified,
                    FollowerCount = followerCount
                };
                _context.Users.Add(existingUser);
            }
            else
            {
                // Mettre à jour uniquement si les informations sont différentes
                existingUser.FullName = instaUser.FullName;
                existingUser.IsVerified = instaUser.IsVerified;
                existingUser.FollowerCount = Math.Max(existingUser.FollowerCount, followerCount);
            }

            await _context.SaveChangesAsync();
            return existingUser;
        }

        private async Task<MediaData> SaveMediaData(
            InstaMedia media, 
            UserData userData, 
            double topicRelevance, 
            EngagementMetrics metrics, 
            double finalScore)
        {
            var mediaData = new MediaData
            {
                MediaCode = media.Code,
                MediaType = media.MediaType.ToString(),
                PostedAt = media.TakenAt,
                LikesCount = metrics.LikesCount,
                CommentsCount = metrics.CommentsCount,
                Caption = media.Caption?.Text,
                User = userData,
                TopicRelevance = topicRelevance,
                PenetrationRate = metrics.PenetrationRate,
                FinalScore = finalScore,
                PostUrl = $"https://instagram.com/p/{media.Code}"
            };

            _context.Medias.Add(mediaData);
            await _context.SaveChangesAsync();

            return mediaData;
        }

        private async Task SaveCategoryScores(MediaData mediaData, Dictionary<string, double> categoryScores)
        {
            foreach (var categoryScore in categoryScores)
            {
                var categoryScoreEntry = new CategoryScore
                {
                    CategoryName = categoryScore.Key,
                    Score = categoryScore.Value,
                    MediaData = mediaData
                };

                _context.CategoryScores.Add(categoryScoreEntry);
            }

            await _context.SaveChangesAsync();
        }

        public async Task SendDirectMessagesToTopPostsCommenters(List<PostAnalysisData> topPosts)
        {
            try
            {
                // Ask user for number of posts to process
                Console.WriteLine("\nCombien de posts du top 10 souhaitez-vous traiter ? (1-10)");
                if (!int.TryParse(Console.ReadLine(), out int numberOfPosts) || numberOfPosts < 1 || numberOfPosts > 10)
                {
                    Console.WriteLine("Nombre invalide. Veuillez entrer un nombre entre 1 et 10.");
                    return;
                }

                // Get message to send
                Console.WriteLine("\nEntrez le message que vous souhaitez envoyer aux commentateurs :");
                string message = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(message))
                {
                    Console.WriteLine("Le message ne peut pas être vide.");
                    return;
                }

                // Track contacted users to avoid duplicates
                var contactedUsers = new HashSet<string>();
                int successCount = 0;
                int totalCommenters = 0;

                // Process selected posts
                var selectedPosts = topPosts.Take(numberOfPosts).ToList();
                Console.WriteLine($"\nRécupération des commentaires pour {numberOfPosts} posts...");

                foreach (var post in selectedPosts)
                {
                    try
                    {
                        // Get media ID from post URL/code
                        var mediaInfoResult = await _instaApi.MediaProcessor.GetMediaIdFromUrlAsync(
                            new Uri($"https://www.instagram.com/p/{post.MediaCode}/")
                        );

                        if (!mediaInfoResult.Succeeded)
                        {
                            Console.WriteLine($"Impossible d'obtenir l'ID du média pour {post.MediaCode}: {mediaInfoResult.Info.Message}");
                            continue;
                        }

                        var mediaId = mediaInfoResult.Value;
                        Console.WriteLine($"\nID média récupéré: {mediaId} pour le post de @{post.UserName}");

                        // Get comments using the media ID
                        var commentsResult = await _instaApi.CommentProcessor.GetMediaCommentsAsync(
                            mediaId,
                            PaginationParameters.MaxPagesToLoad(1)  // Limit to first page to avoid rate limits
                        );

                        if (!commentsResult.Succeeded)
                        {
                            Console.WriteLine($"Impossible de récupérer les commentaires pour le post {post.MediaCode}: {commentsResult.Info.Message}");
                            continue;
                        }

                        var commenters = commentsResult.Value.Comments
                            .Select(c => c.User.UserName)
                            .Distinct()
                            .Where(username => !contactedUsers.Contains(username))
                            .ToList();

                        totalCommenters += commenters.Count;
                        Console.WriteLine($"\nTraitement de {commenters.Count} nouveaux commentateurs pour le post de @{post.UserName}");

                        foreach (var commenterUsername in commenters)
                        {
                            try
                            {
                                // Verify user exists and get their ID
                                var userResult = await _instaApi.UserProcessor.GetUserAsync(commenterUsername);
                                if (!userResult.Succeeded)
                                {
                                    Console.WriteLine($"Impossible de trouver l'utilisateur {commenterUsername}: {userResult.Info.Message}");
                                    continue;
                                }

                                // Send the message
                                var sendResult = await _instaApi.MessagingProcessor.SendDirectTextAsync(
                                    userResult.Value.Pk.ToString(),
                                    string.Empty,  // No specific thread
                                    message
                                );

                                if (sendResult.Succeeded)
                                {
                                    successCount++;
                                    contactedUsers.Add(commenterUsername);
                                    Console.WriteLine($"✓ Message envoyé à @{commenterUsername}");
                                }
                                else
                                {
                                    Console.WriteLine($"× Échec de l'envoi à @{commenterUsername}: {sendResult.Info.Message}");
                                }

                                // Wait between messages to avoid rate limits
                                await Task.Delay(TimeSpan.FromSeconds(3));
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Erreur lors de l'envoi à @{commenterUsername}: {ex.Message}");
                            }
                        }

                        // Wait longer between posts
                        await Task.Delay(TimeSpan.FromSeconds(5));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Erreur lors du traitement du post {post.MediaCode}: {ex.Message}");
                    }
                }

                Console.WriteLine($"\nOpération terminée:");
                Console.WriteLine($"- {totalCommenters} commentateurs uniques trouvés");
                Console.WriteLine($"- {successCount} messages envoyés avec succès");
                Console.WriteLine($"- {contactedUsers.Count} utilisateurs contactés au total");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Une erreur est survenue: {ex.Message}");
            }
        }
    }

    public class HashtagMediaData
    {
        public string Hashtag { get; set; }
        public DateTime ScrapeDate { get; set; }
        public List<MediaInfo> Medias { get; set; }
        public List<RelatedHashtagInfo> RelatedHashtags { get; set; }
        public PaginationInfo PaginationInfo { get; set; }
    }

    public class MediaInfo
    {
        public string InstaIdentifier { get; set; }  // Changé de Pk à InstaIdentifier
        public string MediaType { get; set; }
        public string Code { get; set; }
        public DateTime TakenAt { get; set; }
        public long LikesCount { get; set; }  // Changé en long pour correspondre au type de l'API
        public long CommentsCount { get; set; }  // Changé en long pour correspondre au type de l'API
        public UserInfo User { get; set; }
        public CaptionInfo Caption { get; set; }
        public List<MediaUrl> Images { get; set; }
        public List<MediaUrl> Videos { get; set; }
        public LocationInfo Location { get; set; }
    }

    public class UserInfo
    {
        public string UserName { get; set; }
        public string FullName { get; set; }
        public bool IsVerified { get; set; }
    }

    public class CaptionInfo
    {
        public string Text { get; set; }
    }

    public class MediaUrl
    {
        public string Uri { get; set; }
    }

    public class LocationInfo
    {
        public string Name { get; set; }
        public string Address { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }

    public class RelatedHashtagInfo
    {
        public string Name { get; set; }
        public long PostCount { get; set; }  // Changé de Count à PostCount et type en long
    }

    public class PaginationInfo
    {
        public bool MoreAvailable { get; set; }
        public string NextMaxId { get; set; }
        public int NextPage { get; set; }
    }

    public class EngagementMetrics
    {
        public long FollowerCount { get; set; }
        public long ImpressionsCount { get; set; }
        public long LikesCount { get; set; }
        public long CommentsCount { get; set; }
        public double PenetrationRate { get; set; }
    }

    public class MatchResult
    {
        public string OriginalWord { get; }
        public string MatchedTerm { get; }
        public MatchType Type { get; }
        public float Similarity { get; }

        public MatchResult(string originalWord, string matchedTerm, MatchType type, float similarity)
        {
            OriginalWord = originalWord;
            MatchedTerm = matchedTerm;
            Type = type;
            Similarity = similarity;
        }

        public override string ToString() =>
            $"{OriginalWord} -> {MatchedTerm} ({Type}, {Similarity:P0})";
    }

    public enum MatchType
    {
        Direct,
        Lemma,
        Semantic
    }

    // Modifiez la classe PostAnalysisData pour inclure les nouveaux champs
    public class PostAnalysisData
    {
        public string MediaCode { get; set; }
        public string UserName { get; set; }
        public string Caption { get; set; }
        public double TopicRelevance { get; set; }
        public long LikesCount { get; set; }
        public long CommentsCount { get; set; }
        public List<string> MatchedKeywords { get; set; }
        public string PostUrl { get; set; }
        public Dictionary<string, double> CategoryScores { get; set; }
        public List<MatchResult> MatchResults { get; set; }
        public long FollowerCount { get; set; }
        public long ImpressionsCount { get; set; }
        public double PenetrationRate { get; set; }
        public double FinalScore { get; set; }
    }
}