using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;

using VkNet;
using VkNet.Enums;
using VkNet.Model;
using VkNet.Model.RequestParams;

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

        public static List<Message> RecvMessages(VkApi vk, long id, int? allOrSome, bool reverse, bool quiet) {
            bool all = allOrSome.HasValue;
            int n = allOrSome.GetValueOrDefault(200);

            bool room = false;

            var raw = vk.Messages.Get(new MessagesGetParams() {
                Count = 200,
                Out = MessageType.Received
            });

            var msgs = new List<Message>();

            bool hasUnread = false;

            foreach (var m in raw.Messages) {
                if (m.UserId != id && m.ChatId != id)
                    continue;

                if (m.UserId == id && m.ChatId != null)
                    continue;

                if (m.ChatId == id)
                    room = true;

                if (m.IsDeleted.HasValue && m.IsDeleted.Value)
                    continue;

                bool unread = m.ReadState == MessageReadState.Unreaded;

                if (unread)
                    hasUnread = true;

                if (!unread && !all)
                    continue;

                msgs.Add(m);
            }

            if (all) {
                Thread.Sleep(400);

                raw = vk.Messages.Get(new MessagesGetParams() {
                    Count = 200,
                    Out = MessageType.Sended
                });

                foreach (var m in raw.Messages) {
                    if (m.UserId != id && m.ChatId != id)
                        continue;

                    if (m.UserId == id && m.ChatId != null)
                        continue;

                    if (m.ChatId == id)
                        room = true;

                    if (m.IsDeleted.HasValue && m.IsDeleted.Value)
                        continue;

                    msgs.Add(m);
                }
            }

            msgs.Sort((a, b) => {
                if (!a.Date.HasValue || !b.Date.HasValue) {
                    if (a.Date.HasValue && !b.Date.HasValue)
                        return 1;
                    else if (!a.Date.HasValue && b.Date.HasValue)
                        return -1;
                    else
                        return 0;
                } else {
                    return -DateTime.Compare(a.Date.Value, b.Date.Value);
                }
            });

            if (msgs.Count > (int)n) {
                msgs.RemoveRange((int)n, msgs.Count - (int)n);
            }

            if (!reverse) {
                msgs.Reverse();
            }

            if (!quiet && hasUnread) {
                Thread.Sleep(400);
                vk.Messages.MarkAsRead(from m in msgs where m.Id.HasValue select m.Id.Value, ((room ? 2000000000L : 0) + id).ToString(), null);
            }

            return msgs;
        }

        public static void Send(VkApi vk, long id, string text, bool room = false) {
            vk.Messages.Send(new MessagesSendParams() {
                UserId = room ? null : (long?) id,
                ChatId = room ? (long?)  id : null,
                Message = text,
            });
        }
    }
}
