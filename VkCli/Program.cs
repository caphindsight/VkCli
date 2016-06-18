using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Mono.Options;

namespace VkCli {
    public static class Program {
        public static void Main(string[] args) {
            bool debug = false;
            bool help = false;

            #if !NOCATCH
            try {
            #endif

            new OptionSet() {
                { "debug", _ => debug = true },
                { "?|help", _ => help = true },
            }.Parse(args);

            if (!help) {
                Run(args);
            } else {
                ShowHelp();
            }

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

        public static void ShowHelp() {
            Console.WriteLine("VkCli - a command-line VK client.");
            Console.WriteLine("Usage: vk <subcommand> <options>");

            MethodInfo[] methods = typeof(Methods).GetMethods();
            foreach (MethodInfo m in methods) {
                if (m.GetCustomAttribute<CliMethodAttribute>() == null)
                    continue;

                Console.WriteLine();

                string subcommand = String.Join("/", m.GetCustomAttribute<CliMethodAttribute>().Names);

                string args = "";
                if (m.GetCustomAttribute<CliMethodParamsAttribute>() != null) {
                    args = String.Join(" ", m.GetCustomAttribute<CliMethodParamsAttribute>().Params);
                }

                string flags = "";
                foreach (var flagAttr in m.GetCustomAttributes<CliMethodFlagAttribute>()) {
                    flags += $" [{flagAttr.Flag}]";
                }

                Console.WriteLine($"vk {subcommand} {args} {flags}");
                if (m.GetCustomAttribute<CliMethodDescriptionAttribute>() != null) {
                    Console.WriteLine("    " + m.GetCustomAttribute<CliMethodDescriptionAttribute>().Description);
                }

                foreach (var flagAttr in m.GetCustomAttributes<CliMethodFlagAttribute>()) {
                    Console.WriteLine($"    {flagAttr.Flag} - {flagAttr.Description}");
                }
            }

            Console.WriteLine();
            Console.WriteLine("Copyright (C) 2016 hindsight <hindsight@yandex.ru>.");
        }
    }
}
