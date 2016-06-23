using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

using VkNet;
using VkNet.Enums;
using VkNet.Model;

namespace VkCli {
    public sealed class AppError: Exception {
        public enum ErrorCode {
            UnknownError = 1,
            AssertionFailed = 2,

            ArgumentParseError = 3,

            ApplicationConfigError = 4,
            DeserializationError = 5,

            AuthorizationError = 6,
        }

        public ErrorCode Code { get; private set; }

        public AppError(ErrorCode code, string message)
            : base(message)
        {
            Code = code;
        }

        public void Report(bool stackTrace = false) {
            if (Console.CursorLeft != 0)
                Console.WriteLine();

            string errorStr = Code.ToString();
            string message = Message;

            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write(errorStr);

            if (message != null)
                Console.Write($": {message}");
            
            Console.ResetColor();
            Console.WriteLine();

            if (stackTrace)
                Console.WriteLine(StackTrace);
        }

        public void Fail() {
            Environment.Exit((int)Code);
        }
    }

    public static class CliUtils {
        public static void Fail(AppError.ErrorCode errorCode, string message = null) {
            var err = new AppError(errorCode, message);
            err.Report();
            err.Fail();
        }

        public static void Validate(bool expr, AppError.ErrorCode code, string message = null) {
            if (!expr)
                throw new AppError(code, message);
        }

        public static void PresentField(string field, object value, ConsoleColor? color = null) {
            Console.Write($"{field}: ");

            if (color.HasValue)
                Console.ForegroundColor = color.Value;

            if (value != null)
                Console.Write(value);

            if (color.HasValue)
                Console.ResetColor();

            Console.WriteLine();
        }

        public static void PresentMessage(Message msg, AppData appData) {
            if (msg.ChatId.HasValue) {
                PresentField("Room", appData.GetAbbr(msg.ChatId.Value));
            }

            string abbr = appData.GetAbbr(msg.UserId ?? 0);
            string date = MiscUtils.FormatDate(msg.Date);
            string body = msg.Body ?? "";

            if (msg.Type == MessageType.Sended) {
                Console.WriteLine(date);
                Console.Write("> ");
                WriteLineColor(body, ConsoleColor.DarkGreen);
            } else {
                Console.WriteLine($"{date}  {abbr}");
                WriteLineColor(body, ConsoleColor.Cyan);
            }
        }

        public static void PresentDialog(Message msg, AppData appData) {
            string room = msg.ChatId.HasValue ? appData.GetAbbr(msg.ChatId.Value) : null;
            string abbr = appData.GetAbbr(msg.UserId ?? 0);
            string date = MiscUtils.FormatDate(msg.Date);
            string body = msg.Body ?? "";

            if (room != null) {
                PresentField("Room", room, ConsoleColor.Yellow);
                PresentField("Last message", $"{date}, by {abbr}");
            } else {
                PresentField("Buddy", abbr, ConsoleColor.Yellow);
                PresentField("Last message", date);
            }

            Console.WriteLine(body);
        }

        public static string ReadString(string msg) {
            if (msg != null)
                Console.Write(msg);

            return Console.ReadLine();
        }

        public static string ReadText(ConsoleColor? color = null) {
            var text = new StringBuilder();

            for (;;) {
                Console.Write("> ");

                if (color.HasValue)
                    Console.ForegroundColor = color.Value;

                string line = Console.ReadLine();

                if (color.HasValue)
                    Console.ResetColor();

                if (line.EndsWith(@"\")) {
                    line = line.Substring(0, line.Length - 1);
                    text.AppendLine(line);
                } else {
                    text.AppendLine(line);
                    return text.ToString();
                }
            }
        }

        public static string ReadPassword(string msg) {
            if (msg != null)
                Console.Write(msg);

            string pass = "";

            for (;;) {
                ConsoleKeyInfo key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Enter) {
                    Console.WriteLine();
                    return pass;
                } else if (key.Key == ConsoleKey.Backspace && pass.Length > 0) {
                    pass = pass.Substring(0, (pass.Length - 1));
                    Console.CursorLeft--;
                    Console.Write(" ");
                    Console.CursorLeft--;
                } else if (key.Key == ConsoleKey.Escape) {
                    for (int i = 0; i < pass.Length; i++) {
                        Console.CursorLeft--;
                        Console.Write(" ");
                        Console.CursorLeft--;
                    }
                    pass = "";
                } else {
                    pass += key.KeyChar;
                    Console.Write("*");
                }
            }
        }

        public static void WriteColor(string msg, ConsoleColor? color) {
            if (color.HasValue)
                Console.ForegroundColor = color.Value;

            Console.Write(msg);

            if (color.HasValue)
                Console.ResetColor();
        }

        public static void WriteLineColor(string msg, ConsoleColor? color) {
            WriteColor(msg, color);
            Console.WriteLine();
        }

        public static void LaunchChatMode(VkApi vk, AppData appData, long id, bool room) {
            using (var ctx = new ChatContext(vk, appData, id, room)) {
                var syncThread = new Thread(() => {
                    try {
                        for (;;) {
                            lock (ctx) {
                                try {
                                    ctx.Sync();
                                    ctx.ResetErrors();
                                } catch (ThreadAbortException) {
                                    throw;
                                } catch {
                                    if (!ctx.ReportError())
                                        throw;
                                }
                            }
                            Thread.Sleep(1000);
                        }
                    } catch (ThreadAbortException) {
                        lock (ctx) {
                            ctx.Display();
                        }
                    }
                });

                syncThread.Start();

                for (;;) {
                    while (Console.KeyAvailable) {
                        var key = Console.ReadKey(true);
                        if (key.Key == ConsoleKey.Enter) {
                            Console.WriteLine();
                            string text = ReadText(ConsoleColor.DarkGreen);

                            if (String.IsNullOrWhiteSpace(text)) {
                                Console.WriteLine("(aborted)");
                                ChatContext.HorizontalLine();
                            } else {
                                ChatContext.HorizontalLine();
                                lock (ctx) {
                                    ctx.AddOutgoing(text);
                                }
                            }
                        } else if (key.Key == ConsoleKey.Escape) {
                            goto abort;
                        }
                    }

                    if (Monitor.TryEnter(ctx)) {
                        try {
                            ctx.Display();
                        } finally {
                            Monitor.Exit(ctx);
                        }
                    }

                    Thread.Sleep(50);
                }

                abort:
                syncThread.Abort();
                syncThread.Join();
            }
        }
    }
}
