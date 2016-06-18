using System;

using Mono.Options;

using VkNet;
using VkNet.Enums.Filters;
using VkNet.Model.RequestParams;

namespace VkCli {
    public static class Methods {
        public const int AppId = 5513052;
        public const string AppSecret = "8eAHMe33Lu7NXJhmeiwY";
        public const long AccessMagicNumber = 140492287;

        [CliMethod("st", "state")]
        [CliMethodDescription("displays the current session state")]
        public static void State(string[] args, AppData appData) {
            CliUtils.PresentField("State",
                appData.Authorized ? "authorized" : "not authorized",
                appData.Authorized ? ConsoleColor.DarkGreen : ConsoleColor.DarkRed);

            CliUtils.PresentField("User name", appData.FullName);
        }

        [CliMethod("login")]
        [CliMethodDescription("authorize in VK")]
        public static void Login(string[] args, AppData appData) {
            CliUtils.Validate(!appData.Authorized, AppError.ErrorCode.ApplicationConfigError,
                "already authorized");

            Console.WriteLine("Authorizing in the VK api..");

            Console.WriteLine();
            Console.WriteLine("Step 1. OAuth authorization.");
            Console.WriteLine("Visit the following page in your web browser.");
            Console.WriteLine("Authorize if necessary and copy the code (should appear after #code= in your address bar).");
            Console.WriteLine($"https://oauth.vk.com/authorize?client_id={AppId}&redirect_uri=https:%2F%2Foauth.vk.com%2Fblank.html&scope={AccessMagicNumber}");

            string code = CliUtils.ReadString("Code: ");

            Console.WriteLine();
            Console.WriteLine("Step 2. Receiving access token.");
            Console.WriteLine("Sending authorization request..");

            AccessTokenResponse resp = JsonModelUtils.ReceiveJson<AccessTokenResponse>(
                $"https://oauth.vk.com/access_token?client_id={AppId}&client_secret={AppSecret}&code={code}&redirect_uri=https:%2F%2Foauth.vk.com%2Fblank.html");

            CliUtils.Validate(String.IsNullOrEmpty(resp.error), AppError.ErrorCode.AuthorizationError,
                $"{resp.error}, {resp.error_description}");

            string accessToken = resp.access_token;

            Console.WriteLine();
            Console.WriteLine("Step 3. VK authorization.");

            var vk = new VkApi();
            vk.Authorize(accessToken);

            var profileInfo = vk.Account.GetProfileInfo();
            string userName = $"{profileInfo.FirstName} {profileInfo.LastName}";

            appData = new AppData() {
                Authorized = true,
                AccessToken = accessToken,
                FullName = userName,
                UserId = Convert.ToInt64(resp.user_id),
            };

            AppConfig.SaveAppData(appData);

            Console.WriteLine();
            Console.WriteLine($"Welcome, {userName}!");
        }

        [CliMethod("logout")]
        [CliMethodDescription("drop the current access token")]
        public static void Logout(string[] args, AppData appData) {
            CliUtils.Validate(appData.Authorized, AppError.ErrorCode.ApplicationConfigError,
                "already not authorized");

            AppConfig.SaveAppData(new AppData());

            Console.WriteLine($"Goodbye, {appData.FullName}!");
        }

        [CliMethod("profile")]
        [CliMethodDescription("get user profile settings")]
        [CliMethodRequiresAuthorization]
        public static void Profile(string[] args, AppData appData) {
            var vk = new VkApi();
            vk.Authorize(appData.AccessToken);

            var profile = vk.Account.GetProfileInfo();

            CliUtils.PresentField("User name", $"{profile.FirstName} {profile.LastName}");
            CliUtils.PresentField("VK ID", profile.ScreenName);
            CliUtils.PresentField("Status", profile.Status);
            CliUtils.PresentField("Sex", profile.Sex.HasValue ? profile.Sex.Value.ToString() : null);
            CliUtils.PresentField("Birth date", profile.BirthDate);
            CliUtils.PresentField("Home town", profile.HomeTown);
            CliUtils.PresentField("Relation", profile.Relation.HasValue ? profile.Relation.Value.ToString() : null);
            CliUtils.PresentField("Relation partner", $"{profile.RelationPartner.FirstName} {profile.RelationPartner.LastName}");
        }

        [CliMethod("status")]
        [CliMethodDescription("get user status")]
        [CliMethodRequiresAuthorization]
        public static void GetStatus(string[] args, AppData appData) {
            var vk = new VkApi();
            vk.Authorize(appData.AccessToken);

            string status = vk.Status.Get(appData.UserId).Text;
            Console.WriteLine(status);
        }

        [CliMethod("status=")]
        [CliMethodDescription("set user status")]
        [CliMethodRequiresAuthorization]
        public static void SetStatus(string[] args, AppData appData) {
            string[] opts = new OptionSet().Parse(args).ToArray();

            string status = opts.GetArg(1) ?? "";

            var vk = new VkApi();
            vk.Authorize(appData.AccessToken);

            vk.Status.Set(status);
        }
    }
}
