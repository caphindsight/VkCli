using System;

namespace VkCli {
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class CliMethodAttribute: Attribute {
        public string[] Names { get; set; }

        public CliMethodAttribute(params string[] names) {
            Names = names;
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class CliMethodDescriptionAttribute: Attribute {
        public string Description { get; set; }

        public CliMethodDescriptionAttribute(string description) {
            Description = description;
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class CliMethodParamsAttribute: Attribute {
        public string[] Params { get; set; }

        public CliMethodParamsAttribute(params string[] @params) {
            Params = @params;
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class CliMethodFlagAttribute: Attribute {
        public string Flag { get; set; }
        public string Description { get; set; }

        public CliMethodFlagAttribute(string flag, string description) {
            Flag = flag;
            Description = description;
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class CliMethodRequiresAuthorizationAttribute: Attribute {}
}

