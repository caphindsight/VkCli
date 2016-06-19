using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

using Mono.Options;

using VkNet;
using VkNet.Enums;
using VkNet.Enums.Filters;
using VkNet.Model;
using VkNet.Model.RequestParams;

using UserCollection = System.Collections.ObjectModel.ReadOnlyCollection
    <VkNet.Model.User>;

using GroupCollection = System.Collections.ObjectModel.ReadOnlyCollection
    <VkNet.Model.Group>;

namespace VkCli {
    public static class Methods {
        public const int AppId = 5513052;
        public const string AppSecret = "8eAHMe33Lu7NXJhmeiwY";
        public const long AccessMagicNumber = 140492287;

        [CliMethod("st", "state")]
        [CliMethodDescription("displays the current session state")]
        [CliMethodFlag("access-token", "displays the access token")]
        public static void State(string[] args, AppData appData) {
            bool accessToken = false;
            new OptionSet() {
                { "access-token", _ => accessToken = true },
            }.Parse(args);

            CliUtils.PresentField("State",
                appData.Authorized ? "authorized" : "not authorized",
                appData.Authorized ? ConsoleColor.DarkGreen : ConsoleColor.DarkRed);

            if (accessToken) {
                CliUtils.PresentField("Access token", appData.AccessToken);
            }

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

            appData.AddAbbr("self", appData.UserId);

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
            CliUtils.PresentField("Birth date", MiscUtils.FormatDate(profile.BirthDate));
            CliUtils.PresentField("Home town", profile.HomeTown);
            CliUtils.PresentField("Relation", profile.Relation.HasValue ? profile.Relation.Value.ToString() : null);
            CliUtils.PresentField("Relation partner",
                profile.RelationPartner != null ? $"{profile.RelationPartner.FirstName} {profile.RelationPartner.LastName}" : null);
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
        [CliMethodParams("new_status")]
        [CliMethodRequiresAuthorization]
        public static void SetStatus(string[] args, AppData appData) {
            string[] opts = new OptionSet().Parse(args).ToArray();

            string status = opts.GetArg(1) ?? "";

            var vk = new VkApi();
            vk.Authorize(appData.AccessToken);

            vk.Status.Set(status);
        }

        [CliMethod("friend")]
        [CliMethodDescription("get person's info")]
        [CliMethodParams("id_or_abbr")]
        [CliMethodRequiresAuthorization]
        public static void Friend(string[] args, AppData appData) {
            string[] opts = new OptionSet().Parse(args).ToArray();
            long id = appData.GetId(opts.GetArg(1));

            var vk = new VkApi();
            vk.Authorize(appData.AccessToken);

            User user;

            ProfileFields fields =
                ProfileFields.Nickname
                | ProfileFields.FirstName
                | ProfileFields.LastName
                | ProfileFields.Sex
                | ProfileFields.Status
                | ProfileFields.BirthDate
                | ProfileFields.Relation
                | ProfileFields.RelationPartner;

            try {
                user = vk.Users.Get(id, fields | ProfileFields.Online);
            } catch (Exception e) {
                if (e.Message == "Can not convert String to Int64.") {
                    Thread.Sleep(400);
                    user = vk.Users.Get(id, fields);
                } else {
                    throw;
                }
            }

            CliUtils.PresentField("User name", $"{user.FirstName} {user.LastName}");
            CliUtils.PresentField("Online",
                user.Online.HasValue ? (user.Online.Value ? "online" : "offline") : "unknown",
                user.Online.HasValue ? (user.Online.Value ? ConsoleColor.DarkGreen : ConsoleColor.DarkBlue) : ConsoleColor.Gray);
            CliUtils.PresentField("VK ID", user.ScreenName);
            CliUtils.PresentField("Status", user.Status);
            CliUtils.PresentField("Sex", user.Sex);
            CliUtils.PresentField("Birth date", MiscUtils.FormatDate(user.BirthDate));
            CliUtils.PresentField("Home town", user.HomeTown);
            CliUtils.PresentField("Relation", user.Relation);
            CliUtils.PresentField("Relation partner",
                user.RelationPartner != null ? $"{user.RelationPartner.FirstName} {user.RelationPartner.LastName}" :  null);
        }

        [CliMethod("friends")]
        [CliMethodDescription("get user friends list")]
        [CliMethodFlag("-i, --ids", "show only ids")]
        [CliMethodFlag("-o, --online", "show only online users")]
        [CliMethodFlag("-s, --sort", "sort users alphabetically")]
        [CliMethodRequiresAuthorization]
        public static void Friends(string[] args, AppData appData) {
            bool onlyOnline = false;
            bool sort = false;

            Action<UserCollection> fullPF = (friends) => {
                if (friends.Count == 0) {
                    Console.WriteLine("You have no friends :(");
                }

                var table = new Table();

                foreach (var friend in friends) {
                    table.Add(
                        appData.GetAbbr(friend.Id),
                        friend.Online.HasValue ? (friend.Online.Value ? "online" : "") : "",
                        $"{friend.FirstName} {friend.LastName}",
                        MiscUtils.FormatDate(friend.BirthDate)
                    );
                }

                if (sort)
                    table.SortBy(2);

                table.Display();
            };

            Action<UserCollection> idsPF = (friends) => {
                bool first = true;
                foreach (var friend in friends) {
                    if (first)
                        first = false;
                    else
                        Console.Write(" ");

                    Console.Write(appData.GetAbbr(friend.Id));
                }
                Console.WriteLine();
            };

            Action<UserCollection> act = fullPF;

            new OptionSet() {
                { "i|ids", _ => act = idsPF },
                { "o|online", _ => onlyOnline = true },
                { "s|sort", _ => sort = true }
            }.Parse(args);

            var vk = new VkApi();
            vk.Authorize(appData.AccessToken);

            var res = vk.Friends.Get(new FriendsGetParams() {
                Count = null,
                Fields =
                    ProfileFields.Nickname
                    | ProfileFields.FirstName
                    | ProfileFields.LastName
                    | ProfileFields.BirthDate
            });

            if (onlyOnline) {
                var res2 = (
                    from f in res
                    where f.Online.HasValue
                    where f.Online.Value
                    select f
                ).ToArray();

                res = new UserCollection(res2);
            }

            act(res);
        }

        [CliMethod("abbr", "abbreviate")]
        [CliMethodParams("abbr", "id")]
        [CliMethodFlag("-d, --delete", "delete abbreviation")]
        [CliMethodFlag("-R, --room", "mark an abbreviation of a room")]
        [CliMethodDescription("handles abbreviations")]
        public static void Abbr(string[] args, AppData appData) {
            bool delete = false;
            bool room = false;

            string[] opts = new OptionSet() {
                { "d|delete", _ => delete = true },
                { "R|room", _ => room = true },
            }.Parse(args).ToArray();

            string abbr = opts.GetArg(1);
            string ids = opts.GetArg(2);

            if (!delete) {
                CliUtils.Validate(!String.IsNullOrWhiteSpace(abbr), AppError.ErrorCode.ArgumentParseError,
                    "passed abbreviation is empty");

                CliUtils.Validate(!String.IsNullOrWhiteSpace(ids), AppError.ErrorCode.ArgumentParseError,
                    "passed id is empty");

                long id;

                try {
                    id = Convert.ToInt64(ids);
                } catch {
                    throw new AppError(AppError.ErrorCode.ArgumentParseError, $"unable to convert id '{ids}' to integer");
                }

                appData.AddAbbr(abbr, id, room);
                appData.SaveChanges();
            } else {
                CliUtils.Validate(ids == null, AppError.ErrorCode.ArgumentParseError,
                    "passed id, but requested abbreviation deletion");

                appData.DeleteAbbr(abbr);
                appData.SaveChanges();
            }
        }

        [CliMethod("abbrs", "abbreviations")]
        [CliMethodDescription("shows abbreviations")]
        [CliMethodFlag("-e, --erase", "erase all abbreviations")]
        [CliMethodFlag("-s=.., --save=..", "save abbreviations to file")]
        [CliMethodFlag("-l=.., --load=..", "load and append abbreviations from file")]
        public static void Abbrs(string[] args, AppData appData) {
            bool erase = false;
            string save = null;
            string load = null;

            new OptionSet() {
                { "e|erase", _ => erase = true },
                { "s=|save=", _ => save = _ },
                { "l=|load=", _ => load = _ },
            }.Parse(args);

            if (erase && save == null && load == null) {
                string[] abbrs = appData.GetAbbrs().ToArray();
                foreach (string abbr in abbrs)
                    appData.DeleteAbbr(abbr);

                appData.SaveChanges();
            } else if (!erase && save != null && load == null) {
                var table = new Table();

                foreach (string abbr in appData.GetAbbrs())
                    table.Add(appData.GetId(abbr), abbr, (appData.IsRoom(abbr) ?? false) ? "room" : "");

                using (var w = new StreamWriter(save)) {
                    table.Display(w);
                }
            } else if (!erase && save == null && load != null) {
                using (var r = new StreamReader(load)) {
                    while (!r.EndOfStream) {
                        string line = r.ReadLine();
                        string[] tokens = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (tokens.Length >= 2) {
                            long id = Convert.ToInt64(tokens[0]);
                            string abbr = tokens[1];
                            bool room = tokens.Length >= 3 && tokens[2] == "room";

                            appData.AddAbbr(abbr, id, room);
                        }
                    }
                }

                appData.SaveChanges();
            } else if (!erase && save == null && load == null) {
                var table = new Table();

                foreach (string abbr in appData.GetAbbrs())
                    table.Add(appData.GetId(abbr), abbr, (appData.IsRoom(abbr) ?? false) ? "room" : "");

                table.Display();
            } else {
                throw new AppError(AppError.ErrorCode.ArgumentParseError, "multiple modes (erase/save/load) are given");
            }
        }

        [CliMethod("check")]
        [CliMethodDescription("shows pending dialogs")]
        [CliMethodFlag("-a, --all", "show dialogs without unread messages")]
        [CliMethodRequiresAuthorization]
        public static void Check(string[] args, AppData appData) {
            bool all = false;

            new OptionSet() {
                { "a|all", _ => all = true }
            }.Parse(args);

            var vk = new VkApi();
            vk.Authorize(appData.AccessToken);

            var dialogs = vk.Messages.GetDialogs(new MessagesDialogsGetParams() {
                Count = 200,
                Unread = !all,
            });

            var msgs = (
                from m in dialogs.Messages
                where !all || m.ChatId == null
                select m
            ).ToList();

            if (msgs.Count == 0) {
                return;
            }

            CliUtils.PresentField("Dialogs", msgs.Count, ConsoleColor.Magenta);
            Console.WriteLine();

            foreach (var m in msgs) {
                Console.WriteLine();
                CliUtils.PresentDialog(m, appData);
            }
        }

        [CliMethod("rooms")]
        [CliMethodDescription("shows available rooms")]
        [CliMethodRequiresAuthorization]
        public static void Rooms(string[] args, AppData appData) {
            var vk = new VkApi();
            vk.Authorize(appData.AccessToken);

            var dialogs = vk.Messages.GetDialogs(new MessagesDialogsGetParams() {
                Count = 200,
            });

            if (dialogs.TotalCount == 0) {
                return;
            }

            var msgs = (
                from m in dialogs.Messages
                where m.ChatId != null
                where m.ChatActiveIds != null && m.ChatActiveIds.Count() >= 2
                select m
            ).ToList();

            CliUtils.PresentField("Rooms", msgs.Count, ConsoleColor.Magenta);

            foreach (var m in msgs) {
                Console.WriteLine();
                CliUtils.PresentDialog(m, appData);
            }
        }

        [CliMethod("room")]
        [CliMethodDescription("shows room info")]
        [CliMethodParams("id_or_abbr")]
        [CliMethodRequiresAuthorization]
        public static void Room(string[] args, AppData appData) {
            string[] opts = new OptionSet() {
            }.Parse(args).ToArray();

            long id = appData.GetId(opts.GetArg(1));

            var vk = new VkApi();
            vk.Authorize(appData.AccessToken);

            var res = vk.Messages.GetChat(id);

            CliUtils.PresentField("Title", res.Title);
            CliUtils.PresentField("Admin", appData.GetAbbr(res.AdminId ?? 0));
            CliUtils.PresentField("Users", String.Join(" ", from i in res.Users select appData.GetAbbr(i)));
        }

        [CliMethod("recv", "receive")]
        [CliMethodDescription("receives the conversation")]
        [CliMethodParams("id_or_abbr")]
        [CliMethodFlag("-q, --quiet", "do not mark received messages as read")]
        [CliMethodFlag("-a=.., --all=..", "receive n messages instead of all of unread messages")]
        [CliMethodFlag("-r, --reverse", "reverse the order of displayed messages")]
        [CliMethodFlag("-R, --room", "retrieve messages from a room instead of a user")]
        [CliMethodRequiresAuthorization]
        public static void Recv(string[] args, AppData appData) {
            bool quiet = false;
            bool all = false;
            uint n = 200;
            bool reverse = false;
            bool room = false;

            string[] opts = new OptionSet() {
                { "q|quiet", _ => quiet = true },
                { "a=|all=", _ => { all = true; n = Convert.ToUInt32(_); } },
                { "r|reverse", _ => reverse = true },
                { "R|room", _ => room = true },
            }.Parse(args).ToArray();

            long id = appData.GetId(opts.GetArg(1));

            if (appData.IsRoom(opts.GetArg(1)) == true && !room) {
                throw new AppError(AppError.ErrorCode.ArgumentParseError, $"abbr '{appData.GetAbbr(id)}' is mentioned as a room, but no -R|--room given");
            }

            var vk = new VkApi();
            vk.Authorize(appData.AccessToken);

            var msgs = MiscUtils.RecvMessages(vk, id, all ? (int?)n : null, reverse, quiet);

            if (msgs.Count > 0) {
                CliUtils.PresentField("Messages", msgs.Count, ConsoleColor.Magenta);
                CliUtils.PresentField("Quiet", quiet);

                foreach (var m in msgs) {
                    Console.WriteLine();
                    CliUtils.PresentMessage(m, appData);
                }
            }
        }

        [CliMethod("send")]
        [CliMethodDescription("sends messages")]
        [CliMethodParams("id_or_abbr", "text")]
        [CliMethodFlag("-e, --edit", "enter message interactively")]
        [CliMethodFlag("-R, --room", "send message to a room instead of a user")]
        [CliMethodRequiresAuthorization]
        public static void Send(string[] args, AppData appData) {
            bool edit = false;
            bool room = false;

            string[] opts = new OptionSet() {
                { "e|edit", _ => edit = true },
                { "R|room", _ => room = true },
            }.Parse(args).ToArray();

            long id = appData.GetId(opts.GetArg(1));

            if (appData.IsRoom(opts.GetArg(1)) == true && !room) {
                throw new AppError(AppError.ErrorCode.ArgumentParseError, $"abbr '{appData.GetAbbr(id)}' is mentioned as a room, but no -R|--room given");
            }

            string text = String.Join(" ", from i in Enumerable.Range(2, opts.Length - 2) select opts[i]);

            if (edit) {
                CliUtils.Validate(String.IsNullOrWhiteSpace(text), AppError.ErrorCode.ArgumentParseError,
                    "both -e|--edit mode and message body arguments are present");

                text = CliUtils.ReadText(ConsoleColor.DarkGreen);

                CliUtils.Validate(!String.IsNullOrWhiteSpace(text), AppError.ErrorCode.ArgumentParseError,
                    $"empty message");
            } else {
                CliUtils.Validate(!String.IsNullOrWhiteSpace(text), AppError.ErrorCode.ArgumentParseError,
                    "no message body is passed and -e|--edit mode is not enabled");
            }

            var vk = new VkApi();
            vk.Authorize(appData.AccessToken);

            MiscUtils.Send(vk, id, text, room);
        }

        [CliMethod("chat")]
        [CliMethodDescription("enter chat mode")]
        [CliMethodParams("id_or_abbr")]
        [CliMethodFlag("-p=.., --prev=..", "show n previous messages")]
        [CliMethodFlag("-R, --room", "chat in a room")]
        [CliMethodRequiresAuthorization]
        public static void Chat(string[] args, AppData appData) {
            int p = -1;
            bool room = false;

            string[] opts = new OptionSet() {
                { "p=|prev=", _ => p = Convert.ToInt32(_) },
                { "R|room", _ => room = true },
            }.Parse(args).ToArray();

            long id = appData.GetId(opts.GetArg(1));

            if (appData.IsRoom(opts.GetArg(1)) == true && !room) {
                throw new AppError(AppError.ErrorCode.ArgumentParseError, $"abbr '{appData.GetAbbr(id)}' is mentioned as a room, but no -R|--room given");
            }

            var vk = new VkApi();
            vk.Authorize(appData.AccessToken);

            var msgs = MiscUtils.RecvMessages(vk, id, p >= 0 ? (int?)p : null, false, false);

            Console.WriteLine("Entering chat mode. Press enter at any time to begin typing.");
            if (!room)
                CliUtils.PresentField("Buddy", appData.GetAbbr(id));
            else
                CliUtils.PresentField("Room", appData.GetAbbr(id));

            if (p != 0) {
                foreach (var m in msgs) {
                    Console.WriteLine();
                    CliUtils.PresentMessage(m, appData);
                }
            }

            CliUtils.LaunchChatMode(vk, appData, id, room);

            Console.WriteLine();
            Console.WriteLine("End of chat.");
        }

        [CliMethod("important")]
        [CliMethodDescription("retrieve important messages")]
        [CliMethodFlag("-n=..", "show n last messages")]
        [CliMethodRequiresAuthorization]
        public static void Important(string[] args, AppData appData) {
            uint n = 0;

            new OptionSet() {
                { "n=", _ => n = Convert.ToUInt32(_) }
            }.Parse(args);

            CliUtils.Validate(n > 0, AppError.ErrorCode.ArgumentParseError,
                $"should pass the number of messages to retrieve (-n=..)");

            var vk = new VkApi();
            vk.Authorize(appData.AccessToken);

            var messages = vk.Messages.Get(new MessagesGetParams() {
                Count = n,
                Filters = MessagesFilter.Important,
            }).Messages;

            CliUtils.PresentField("Messages", messages.Count);

            foreach (var m in messages) {
                Console.WriteLine();
                CliUtils.PresentMessage(m, appData);
            }
        }
    }
}
