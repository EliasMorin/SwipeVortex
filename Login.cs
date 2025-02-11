using System;
using System.IO;
using System.Threading.Tasks;
using InstagramApiSharp.API;
using InstagramApiSharp.Classes;
using InstagramApiSharp.API.Builder;
using InstagramApiSharp.Classes.Android.DeviceInfo;
using InstagramApiSharp.Logger;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text;
using MicrosoftLogLevel = Microsoft.Extensions.Logging.LogLevel;
using InstaLogLevel = InstagramApiSharp.Logger.LogLevel;

namespace SwipeVortexWb.Instagram
{
    public class InstagramManager
    {
        private readonly ILogger<InstagramManager> _logger;
        private IInstaApi _instaApi;
        private const string STATE_FILE = "state.json";

        public event EventHandler<string> VerificationRequired;

        public InstagramManager(ILogger<InstagramManager> logger)
        {
            _logger = logger;
        }

        private void WriteLog(string prefix, string message, MicrosoftLogLevel logLevel)
        {
            ConsoleColor color = logLevel switch
            {
                MicrosoftLogLevel.Information => ConsoleColor.DarkYellow,
                MicrosoftLogLevel.Error => ConsoleColor.DarkRed,
                MicrosoftLogLevel.Debug => ConsoleColor.DarkGreen,
                _ => ConsoleColor.Gray
            };

            Console.ForegroundColor = color;
            Console.Write(prefix + " ");
            Console.WriteLine(message);
            Console.ResetColor();

            switch (logLevel)
            {
                case MicrosoftLogLevel.Error:
                    _logger.LogError(message);
                    break;
                case MicrosoftLogLevel.Information:
                    _logger.LogInformation(message);
                    break;
                case MicrosoftLogLevel.Debug:
                    _logger.LogDebug(message);
                    break;
            }
        }

        private void WriteInfo(string message) => WriteLog("[-]", message, MicrosoftLogLevel.Information);
        private void WriteSuccess(string message) => WriteLog("[+]", message, MicrosoftLogLevel.Debug);
        private void WriteError(string message) => WriteLog("[x]", message, MicrosoftLogLevel.Error);

        private async Task<bool> HandleChallengeAsync(IInstaApi api)
        {
            try
            {
                var challengeMethod = await api.GetChallengeRequireVerifyMethodAsync();
                if (!challengeMethod.Succeeded)
                {
                    WriteError($"Failed to get challenge verification methods: {challengeMethod.Info.Message}");
                    return false;
                }

                // Trigger verification required event
                string verificationMethod = !string.IsNullOrEmpty(challengeMethod.Value.StepData.Email) 
                    ? challengeMethod.Value.StepData.Email 
                    : "unknown";
                
                // Raise an event instead of using SignalR
                VerificationRequired?.Invoke(this, verificationMethod);

                return true;
            }
            catch (Exception ex)
            {
                WriteError($"Challenge verification error: {ex.Message}");
                return false;
            }
        }

        // Method to verify the code from web interface
        public async Task<bool> VerifyInstagramCodeAsync(string verificationCode)
        {
            if (string.IsNullOrEmpty(verificationCode))
            {
                WriteError("Verification code cannot be empty");
                return false;
            }

            try
            {
                var verifyCodeResult = await _instaApi.VerifyCodeForChallengeRequireAsync(verificationCode);
                
                if (verifyCodeResult.Succeeded)
                {
                    WriteSuccess("Challenge completed successfully!");
                    await SaveSessionAsync();
                    return true;
                }
                else
                {
                    WriteError($"Challenge verification failed: {verifyCodeResult.Info.Message}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                WriteError($"Error during challenge verification: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> InitializeAndLogin(string username, string password, CancellationToken cancellationToken)
        {
            try
            {
                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    WriteError("Username or password cannot be empty");
                    return false;
                }

                Console.OutputEncoding = Encoding.UTF8;
                WriteInfo("Initializing Instagram API...");

                var customDevice = new AndroidDevice
                {
                    AndroidBoardName = "HONOR",
                    DeviceBrand = "HUAWEI",
                    HardwareManufacturer = "HUAWEI",
                    DeviceModel = "PRA-LA1",
                    DeviceModelIdentifier = "PRA-LA1",
                    FirmwareBrand = "HWPRA-H",
                    HardwareModel = "hi6250",
                    DeviceGuid = Guid.NewGuid(),
                    PhoneGuid = Guid.NewGuid(),
                    Resolution = "1080x1812",
                    Dpi = "480dpi"
                };

                var userSession = new UserSessionData
                {
                    UserName = username,
                    Password = password
                };

                _instaApi = InstaApiBuilder.CreateBuilder()
                    .SetUser(userSession)
                    .UseLogger(new DebugLogger(InstaLogLevel.Exceptions))
                    .SetRequestDelay(RequestDelay.FromSeconds(2, 2))
                    .Build();

                _instaApi.SetDevice(customDevice);
                WriteInfo("Device configuration completed");

                // Load previous session if exists
                if (File.Exists(STATE_FILE))
                {
                    WriteInfo("Loading previous session...");
                    await LoadSessionAsync();
                }

                if (!_instaApi.IsUserAuthenticated)
                {
                    WriteInfo("Logging into Instagram...");
                    var loginResult = await _instaApi.LoginAsync();

                    if (loginResult.Succeeded)
                    {
                        await SaveSessionAsync();
                        WriteSuccess("Successfully logged in to Instagram!");
                        return true;
                    }
                    else
                    {
                        if (loginResult.Value == InstaLoginResult.ChallengeRequired)
                        {
                            WriteInfo("Security verification required");
                            var challengeResult = await HandleChallengeAsync(_instaApi);
                            if (challengeResult)
                            {
                                await SaveSessionAsync();
                                WriteSuccess("Successfully logged in after verification!");
                                return true;
                            }
                        }
                        else if (loginResult.Value == InstaLoginResult.TwoFactorRequired)
                        {
                            WriteInfo("Two-factor authentication required");
                            Console.Write("Enter 2FA code: ");
                            var twoFactorCode = Console.ReadLine();
                            var twoFactorResult = await _instaApi.TwoFactorLoginAsync(twoFactorCode);

                            if (twoFactorResult.Succeeded)
                            {
                                await SaveSessionAsync();
                                WriteSuccess("Successfully logged in with 2FA!");
                                return true;
                            }
                            else
                            {
                                WriteError($"2FA failed: {twoFactorResult.Info.Message}");
                            }
                        }
                        else
                        {
                            WriteError($"Login failed: {loginResult.Info.Message}");
                        }
                        return false;
                    }
                }

                WriteSuccess("Already logged in to Instagram!");
                return true;
            }
            catch (Exception ex)
            {
                WriteError($"Initialization error: {ex.Message}");
                _logger.LogError(ex, "Instagram login failed");
                return false;
            }
        }

        private async Task LoadSessionAsync()
        {
            try
            {
                var json = await File.ReadAllTextAsync(STATE_FILE);
                var sessionData = JsonConvert.DeserializeObject<StateData>(json);
                _instaApi.LoadStateDataFromString(sessionData?.State);
                WriteSuccess("Previous session loaded successfully");
            }
            catch (Exception ex)
            {
                WriteError($"Error loading session: {ex.Message}");
            }
        }

        private async Task SaveSessionAsync()
        {
            try
            {
                var stateData = new StateData { State = _instaApi.GetStateDataAsString() };
                var json = JsonConvert.SerializeObject(stateData);
                await File.WriteAllTextAsync(STATE_FILE, json);
                WriteSuccess("Session saved successfully");
            }
            catch (Exception ex)
            {
                WriteError($"Error saving session: {ex.Message}");
            }
        }

        private class StateData
        {
            public string State { get; set; }
        }

        public bool IsLoggedIn()
        {
            return _instaApi?.IsUserAuthenticated ?? false;
        }

        public IInstaApi GetApi()
        {
            return _instaApi;
        }
    }
}