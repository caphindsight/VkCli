using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Web.Script.Serialization;


namespace VkCli {
    public static class JsonModelUtils {
        private static readonly JavaScriptSerializer JsonSerializer_ = new JavaScriptSerializer();

        public static T ReceiveJson<T>(string uri) {
            // Because C#'s fucking API seems to not work correctly with https.
            // Server returns 401. Yeah. And curl is somehow special, lol.

            string json;

            var psi = new ProcessStartInfo() {
                FileName = "curl",
                Arguments = $"\"{uri}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };

            using (var p = Process.Start(psi)) { 
                json = p.StandardOutput.ReadToEnd();
            }

            return JsonSerializer_.Deserialize<T>(json);
        }
    }

    public sealed class AccessTokenResponse {
        public string error { get; set; }
            = null;

        public string error_description { get; set; }
            = null;

        public string access_token { get; set; }
            = null;

        public string expires_in { get; set; }
            = null;

        public string user_id { get; set; }
            = null;

        public string email { get; set; }
            = null;
    }
}

