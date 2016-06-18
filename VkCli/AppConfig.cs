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

        private Dictionary<string, long> AbbrToId_
            = new Dictionary<string, long>();

        private Dictionary<long, string> IdToAbbr_
            = new Dictionary<long, string>();

        public void AddAbbr(string abbr, long id) {
            if (AbbrToId_.ContainsKey(abbr)) {
                throw new AppError(AppError.ErrorCode.ApplicationConfigError,
                    $"abbreviation '{abbr}' already present, corresponding to id {AbbrToId_[abbr]}");
                
            }

            if (IdToAbbr_.ContainsKey(id)) {
                throw new AppError(AppError.ErrorCode.ApplicationConfigError,
                    $"id {id} already present, corresponding to abbreviation '{IdToAbbr_[id]}'");
                
            }

            AbbrToId_.Add(abbr, id);
            IdToAbbr_.Add(id, abbr);
        }

        public void DeleteAbbr(string abbr) {
            CliUtils.Validate(AbbrToId_.ContainsKey(abbr), AppError.ErrorCode.ApplicationConfigError,
                $"abbreviation '{abbr}' not found");

            long id = AbbrToId_[abbr];

            CliUtils.Validate(IdToAbbr_.ContainsKey(id) && IdToAbbr_[id] == abbr, AppError.ErrorCode.AssertionFailed);

            AbbrToId_.Remove(abbr);
            IdToAbbr_.Remove(id);
        }


        public long GetId(string str) {
            CliUtils.Validate(!String.IsNullOrWhiteSpace(str), AppError.ErrorCode.ArgumentParseError,
                "empty id given");

            if (AbbrToId_.ContainsKey(str)) {
                return AbbrToId_[str];
            } else {
                try {
                    return Convert.ToInt64(str);
                } catch {
                    throw new AppError(AppError.ErrorCode.ArgumentParseError, $"unknown abbr or id: '{str}'");
                }
            }
        }

        public string GetAbbr(long id) {
            if (IdToAbbr_.ContainsKey(id))
                return IdToAbbr_[id];
            else
                return id.ToString();
        }

        public IEnumerable<string> GetAbbrs() {
            foreach (string abbr in AbbrToId_.Keys)
                yield return abbr;
        }

        public void SaveChanges() {
            AppConfig.SaveAppData(this);
        }
    }
}
