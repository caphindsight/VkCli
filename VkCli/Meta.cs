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
    public sealed class CliMethodRequiresAuthorizationAttribute: Attribute {}
}

