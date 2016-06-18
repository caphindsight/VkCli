using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Mono.Options;

namespace VkCli {
    public static class Program {
        public static void Main(string[] args) {
            bool debug = false;

            #if !NOCATCH
            try {
            #endif

            new OptionSet() {
                { "debug", _ => debug = true }
            }.Parse(args);
            Run(args);

            #if !NOCATCH
            } catch (AppError e) {
                e.Report(debug);
                e.Fail();
            } catch (Exception e) {
                var err = new AppError(AppError.ErrorCode.UnknownError, e.Message);
                err.Report();
                if (debug)
                    Console.WriteLine(e.StackTrace);
                err.Fail();
            }
            #endif
        }

        public static void Run(string[] args) {
            List<string> cmd = new OptionSet().Parse(args);
            CliUtils.Validate(cmd.Count != 0, AppError.ErrorCode.ArgumentParseError, "no subcommand given");

            string subcommand = cmd[0];
            MethodInfo[] methods = (
                from i in typeof(Methods).GetMethods()
                let attr = i.GetCustomAttribute<CliMethodAttribute>()
                where attr != null
                where attr.Names.Any(_ => _ == subcommand)
                select i
            ).ToArray();

            CliUtils.Validate(methods.Length == 1, AppError.ErrorCode.ArgumentParseError, $"unknown subcomand ({subcommand})");

            MethodInfo method = methods[0];

            AppData appData = AppConfig.LoadAppData();

            if (method.GetCustomAttribute<CliMethodRequiresAuthorizationAttribute>() != null) {
                CliUtils.Validate(appData.Authorized, AppError.ErrorCode.AuthorizationError,
                    "should authorize first (type `vk login`)");
            }

            Action<string[], AppData> action = (Action<string[], AppData>)Delegate.CreateDelegate(
                typeof(Action<string[], AppData>), method);
            action(args, appData);
        }
    }
}
