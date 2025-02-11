using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SwipeVortexWb
{
    internal class Sessions
    {
        // Bumble
        public const string BASE_URL = "https://am1.bumble.com/mwebapi.phtml?";
        public const string USER_AGENT = "Mozilla/5.0 (X11; CrOS x86_64 14541.0.0) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36";

        public static string sessionCookie = "";
        public static string deviceId = "";
        public static string personId = "";
        public static int vote = 2;  

        // Happn
        public static string token = "eyJhbGciOiJIUzI1NiJ9.eyJzY29wZSI6WyJ1c2VyX2RlbGV0ZSIsInVzZXJfcmVwb3J0X3JlYWQiLCJ1c2VyX3RyYWl0X2Fuc3dlcl93cml0ZSIsInVzZXJfdXBkYXRlIiwiYm9vc3RfY3JlYXRlIiwiYm9vc3RfcmVhZCIsInVzZXJfc29jaWFsX2RlbGV0ZSIsInVzZXJfY29udmVyc2F0aW9uX3JlYWQiLCJ1c2VyX2FjY2VwdGVkX3JlYWQiLCJ1c2VyX2NvbnZlcnNhdGlvbl9jcmVhdGUiLCJhbGxfdXNlcl90cmFpdF9hbnN3ZXJfcmVhZCIsInVzZXJfcmVqZWN0ZWRfY3JlYXRlIiwidXNlcl9ibG9ja2VkX3JlYWQiLCJzaG9ydGxpc3RfcmVhZCIsInVzZXJfcmVwb3J0X3VwZGF0ZSIsInVzZXJfYWNoaWV2ZW1lbnRfcmVhZCIsInJld2FyZGVkX2Fkc191cGRhdGUiLCJ1c2VyX2FjaGlldmVtZW50X2RlbGV0ZSIsInVzZXJfcmVwb3J0X2RlbGV0ZSIsInVzZXJfYXVkaW9jYWxsX2NyZWF0ZSIsInVzZXJfYWNoaWV2ZW1lbnRfdXBkYXRlIiwidXNlcl92aWRlb2NhbGxfdXBkYXRlIiwidXNlcl9hcHBsaWNhdGlvbnNfcmVhZCIsInVzZXJfYmxvY2tlZF9kZWxldGUiLCJ1c2VyX3N1YnNjcmlwdGlvbl9jcmVhdGUiLCJwYWNrX3JlYWQiLCJ1c2VyX29yZGVyX3VwZGF0ZSIsInVzZXJfcmVhZCIsIm5vdGlmaWNhdGlvbl90eXBlX3JlYWQiLCJ1c2VyX2FjaGlldmVtZW50X2NyZWF0ZSIsInVzZXJfbWVzc2FnZV9yZWFkIiwidXNlcl9pbWFnZV9jcmVhdGUiLCJ1c2VyX2NvbnZlcnNhdGlvbl9kZWxldGUiLCJ1c2VyX3NvY2lhbF91cGRhdGUiLCJ1c2VyX2RldmljZV9kZWxldGUiLCJ1c2VyX2FjY2VwdGVkX2NyZWF0ZSIsInN1YnNjcmlwdGlvbl90eXBlX3JlYWQiLCJ1c2VyX3Bva2VfY3JlYXRlIiwidHJhaXRfcmVhZCIsInVzZXJfYXBwbGljYXRpb25zX3VwZGF0ZSIsInVzZXJfcmVwb3J0X2NyZWF0ZSIsInVzZXJfb3JkZXJfY3JlYXRlIiwidXNlcl9kZXZpY2VfdXBkYXRlIiwidXNlcl9zaG9wX3JlYWQiLCJhcmNoaXZlX2NyZWF0ZSIsInVzZXJfcmVqZWN0ZWRfcmVhZCIsInVzZXJfYXBwbGljYXRpb25zX2RlbGV0ZSIsInVzZXJfc3Vic2NyaXB0aW9uX2RlbGV0ZSIsInVzZXJfYXVkaW9jYWxsX3JlYWQiLCJ1c2VyX3N1YnNjcmlwdGlvbl9yZWFkIiwidXNlcl92aWRlb2NhbGxfcmVhZCIsInVzZXJfYmxvY2tlZF9jcmVhdGUiLCJ1c2VyX3N1YnNjcmlwdGlvbl91cGRhdGUiLCJ1c2VyX21lc3NhZ2VfY3JlYXRlIiwidXNlcl9tZXNzYWdlX2RlbGV0ZSIsInVzZXJfbW9kZV9yZWFkIiwidXNlcl9zb2NpYWxfY3JlYXRlIiwidXNlcl9pbWFnZV91cGRhdGUiLCJsb2NhbGVfcmVhZCIsInVzZXJfbm90aWZpY2F0aW9uc19yZWFkIiwiYWNoaWV2ZW1lbnRfdHlwZV9yZWFkIiwic2VhcmNoX3VzZXIiLCJ1c2VyX2ltYWdlX2RlbGV0ZSIsInVzZXJfZGV2aWNlX3JlYWQiLCJhbGxfdXNlcl9yZWFkIiwicmV3YXJkZWRfYWRzX3JlYWQiLCJ1c2VyX3NvY2lhbF9yZWFkIiwiYXJjaGl2ZV9yZWFkIiwidXNlcl9kZXZpY2VfY3JlYXRlIiwidXNlcl9wb3NpdGlvbl9yZWFkIiwicGF5bWVudF9wb3J0YWxfcmVhZCIsInVzZXJfYWNjZXB0ZWRfZGVsZXRlIiwidXNlcl9tZXNzYWdlX3VwZGF0ZSIsInVzZXJfYXVkaW9jYWxsX3VwZGF0ZSIsInVzZXJfb3JkZXJfcmVhZCIsInVzZXJfdmlkZW9jYWxsX2NyZWF0ZSIsImxhbmd1YWdlX3JlYWQiLCJhbGxfaW1hZ2VfcmVhZCIsInVzZXJfY29udmVyc2F0aW9uX3VwZGF0ZSIsInVzZXJfaW1hZ2VfcmVhZCIsImNoZWNrb3V0X2NyZWF0ZSIsInVzZXJfcmVqZWN0ZWRfZGVsZXRlIiwicmVwb3J0X3R5cGVfcmVhZCIsInVzZXJfcG9zaXRpb25fdXBkYXRlIl0sImp0aSI6IjZiMzk5ZTM0LTY2ZjAtNDA2ZS05NWRmLTQzODFjZDRjY2RlNyIsInN1YiI6IjI1MGM2NjFjLTg0MDktNGM3My1hMjgwLTBkZmFkNzdkMDBjOSIsImF1ZCI6IlNxSFNQcW02anlvRlhTMnNBaEU2TmNjNUR2azlYUWp4MG1Ud2x3Q0tMdCIsImV4cCI6MTczMDU2OTg0NiwiaWF0IjoxNzMwNDgzNDQ2fQ.YbXOs1c8IaNUagph-6MBnXpdPa-lr7RjC2bq0cUxKu4";

        // Tinder
        public static string XAuthToken = "a21e1daf-d4ea-4ae3-971c-f86ac3e11f6f";
        public static string UserId = "66c503dc7d6ef601007760c0";

        // Instagram
        public static string InstagramUsername = "mothersaintarm";
        public static string InstagramPassword = "Laser2wx!#";

        // Messages prédéfinis
        public const string DEFAULT_HAPPN_MESSAGE = "Hey! Comment vas-tu ? 😊";
        public const string DEFAULT_BUMBLE_MESSAGE = "Hey";
    }
}