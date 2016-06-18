using System;
using System.Text;

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
            string abbr = appData.GetAbbr(msg.UserId.GetValueOrDefault(0));
            string date = MiscUtils.FormatDate(msg.Date);
            string body = msg.Body;

            Console.Write(date);
            Console.Write("  ");

            Console.Write(abbr);

            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(body);
            Console.ResetColor();
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

                if (line != "")
                    text.AppendLine(line);
                else
                    return text.ToString();
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
    }
}
