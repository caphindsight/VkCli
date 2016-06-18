using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

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
    }
}
