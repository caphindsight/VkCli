using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace VkCli {
    public static class MiscUtils {
        private static readonly BinaryFormatter BinaryFormatter_ = new BinaryFormatter();

        public static void SaveToFile<T>(string filePath, T obj) {
            using (var stream = File.OpenWrite(filePath)) {
                BinaryFormatter_.Serialize(stream, obj);
            }
        }

        public static T LoadFromFile<T>(string filePath) {
            using (var stream = File.OpenRead(filePath)) {
                var obj = BinaryFormatter_.Deserialize(stream);

                CliUtils.Validate(obj is T, AppError.ErrorCode.DeserializationError,
                    $"type mismatch (expected {typeof(T).Name}, got {obj.GetType().Name})");

                return (T)obj;
            }
        }

        public static T GetArg<T>(this T[] args, int n)
            where T: class
        {
            if (n >= 0 && n < args.Length)
                return args[n];
            else
                return null;
        }

        public enum StringAlignment {
            Left, Right
        }

        public static string FitString(string s, int n,
            int m = Int32.MaxValue, StringAlignment alignment = StringAlignment.Left, char blank = ' ')
        {
            if (s == null)
                s = "";

            int l = s.Length;
            if (l < n) {
                var t = new StringBuilder();

                if (alignment == StringAlignment.Left)
                    t.Append(s);

                for (int i = l; i < n; i++)
                    t.Append(blank);

                if (alignment == StringAlignment.Right)
                    t.Append(s);

                return t.ToString();
            } else if (l > m) {
                if (m < 2)
                    return s.Substring(0, m);
                else
                    return s.Substring(0, m - 2) + "..";
            } else {
                return s;
            }
        }

        public static string FormatDate(string date, char sep = '.') {
            if (date == null)
                return null;

            string[] parts = date.Split(new char[] { sep }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < parts.Length; i++) {
                parts[i] = FitString(parts[i], 2, Int32.MaxValue, StringAlignment.Right, '0');
            }

            return String.Join(sep.ToString(), parts);
        }

        public static string FormatDate(DateTime? date) {
            if (date == null)
                return "--.--.---- --:--:--";

            return FormatDate(date.Value);
        }

        public static string FormatDate(DateTime date) {
            string strD = $"{date.Day}.{date.Month}.{date.Year}";
            string strT = $"{date.Hour}:{date.Minute}:{date.Second}";
            return FormatDate(strD, '.') + " " + FormatDate(strT, ':');
        }
    }
}
