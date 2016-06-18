using System;
using System.Collections.Generic;
using System.IO;

namespace VkCli {
    public static class AppConfig {
        private static readonly FileInfo AppDataFile_;
        private static readonly object AppLock_ = new Object();

        static AppConfig() {
            string userDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var userDataDir = new DirectoryInfo(userDataPath);

            if (!userDataDir.Exists)
                CliUtils.Fail(AppError.ErrorCode.ApplicationConfigError, $"local application data dir ({userDataPath}) does not exist");

            AppDataFile_ = new FileInfo(Path.Combine(userDataDir.FullName, "VkCli.dat"));

            if (!AppDataFile_.Exists) {
                SaveAppData(new AppData());
            }
        }

        public static AppData LoadAppData() {
            lock (AppLock_) {
                return MiscUtils.LoadFromFile<AppData>(AppDataFile_.FullName);
            }
        }

        public static void SaveAppData(AppData appData) {
            lock (AppLock_) {
                MiscUtils.SaveToFile<AppData>(AppDataFile_.FullName, appData);
            }
        }
    }

    [Serializable]
    public sealed class AppData {
        public bool Authorized { get; set; }
            = false;

        public string AccessToken { get; set; }
            = null;

        public string FullName { get; set; }
            = null;

        public long UserId { get; set; }
            = -1;
    }
}
