using System;
using System.Collections.Generic;

using VkNet;
using VkNet.Model;

namespace VkCli {
    public sealed class ChatContext: IDisposable {
        private ChatContext() {
            HorizontalLine();
        }

        public ChatContext(VkApi vk, AppData appData, long id, bool room)
            : this()
        {
            Vk = vk;
            AppData = appData;
            Id = id;
            Room = room;
        }

        public VkApi Vk { get; private set; }
        public AppData AppData { get; private set; }
        public long Id { get; private set; }
        public bool Room { get; private set; }

        public void Dispose() {
            Console.WriteLine();
            Console.WriteLine("End of chat.");
        }

        private readonly List<Message> Incoming_
            = new List<Message>();

        private readonly List<string> Outgoing_
            = new List<string>();

        public static void HorizontalLine() {
            Console.WriteLine();
            CliUtils.WriteLineColor("***", ConsoleColor.White);
        }

        public void Sync() {
            var msgs = MiscUtils.RecvMessages(Vk, Id, null, false, false);
            Incoming_.AddRange(msgs);

            foreach (string body in Outgoing_)
                MiscUtils.Send(Vk, Id, body, Room);

            Outgoing_.Clear();
        }

        public void Display() {
            if (Incoming_.Count == 0)
                return;

            foreach (var msg in Incoming_) {
                Console.WriteLine();
                CliUtils.PresentMessage(msg, AppData);
            }

            Incoming_.Clear();

            HorizontalLine();
        }

        public void AddOutgoing(string body) {
            Outgoing_.Add(body);
        }

        private int ErrorsReported_ = 0;
        private const int MaxErrors_ = 10;

        public bool ReportError() {
            ErrorsReported_++;
            return ErrorsReported_ < MaxErrors_;
        }

        public void ResetErrors() {
            ErrorsReported_ = 0;
        }
    }
}
