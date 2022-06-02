using System.Collections.Generic;
using System.Text;

namespace Octothorpe.UGity.Client
{
    public class GitCommand : GitCommand<GitCommand, GitCommandResult>
    {
        public GitCommand(string name) : base(name) { }

        public override void ParseExtendedResult(in GitCommandResult result) { }

        // Expose this method for this class as it represents arbitrary commands with any arguments
        public GitCommand WithArgument(string argument, bool quoted = false) => base.WithArgument(int.MinValue, argument, quoted);

        public static implicit operator GitCommand(string name) => new GitCommand(name);
    }

    public abstract class GitCommand<TSelf, TResult> : IGitCommand
        where TSelf : GitCommand<TSelf, TResult>
        where TResult : GitCommandResult, new()
    {
#if UGITY_DEBUG
        private static readonly HashSet<string> DryRunCommands = new HashSet<string>
        {
            "push",
            "pull",
            "fetch",
            "commit",
        };
#endif

        public string Name { get; private set; }
        public string[] Pathspec { get; private set; }

        private Dictionary<string, List<string>> options = new Dictionary<string, List<string>>();
        private SortedDictionary<int, List<string>> arguments = new SortedDictionary<int, List<string>>();

        public GitCommand(string name)
        {
            Name = name;
#if UGITY_DEBUG
            if(DryRunCommands.Contains(name))
                WithOption("--dry-run");
#endif
        }

        public abstract void ParseExtendedResult(in TResult result);

        public string ToString(StringBuilder builder)
        {
            builder.Append(Name);

            foreach(KeyValuePair<string, List<string>> arg in this.options)
            {
                if(string.IsNullOrEmpty(arg.Key)) continue;

                foreach(string value in arg.Value)
                {
                    builder.Append(" ").Append(FormatArgumentValue(arg.Key, value));
                }
            }

            foreach(List<string> valueList in this.arguments.Values)
            {
                foreach(string value in valueList)
                    builder.Append(" ").Append(value);
            }
            
            if(Pathspec != null)
            {
                builder.Append(" -- ");
                foreach(string path in Pathspec)
                {
                    builder.Append("\"").Append(path).Append("\" ");
                }
            }

            return builder.ToString();
        }
        
        public TResult ParseResult(int exitCode, string output, string error)
        {
            TResult result = GitCommandResult.Create<TResult>(exitCode, output, error);

            ParseExtendedResult(result);
            return result;
        }

        public TSelf WithOptionIf(bool condition, string option, object value = null)
        {
            if(condition)
                WithOption(option, value);

            return (TSelf) this;
        }

        public TSelf WithOption(string option, object value = null)
        {
            option = HyphenateOption(option);
            AppendOption(option, value);
            return (TSelf) this;
        }

        public TSelf WithFlags(string flags)
        {
            foreach(char flag in flags)
            {
                if(flag == '-') continue;
                AppendOption(HyphenateOption(flag.ToString()), null);
            }

            return (TSelf) this;
        }

        public TSelf WithPathspec(params string[] pathspec) => WithPathspec((IEnumerable<string>) pathspec);

        public TSelf WithPathspec(IEnumerable<string> pathspec)
        {
            Pathspec = new List<string>(pathspec).ToArray();
            return (TSelf) this;
        }

        public IReadOnlyList<string> GetOptionValues(string option)
        {
            if(!this.options.ContainsKey(option))
                return new string[0];

            return this.options[option].ToArray();
        }

        public string GetOptionValue(string option)
        {
            if(!this.options.ContainsKey(option))
                return null;

            return this.options[option][0];
        }

        public bool HasOption(string name)
        {
            return this.options.ContainsKey(HyphenateOption(name));
        }

        public void SetOption(string option, object value)
        {
            option = HyphenateOption(option);
            this.options.Remove(option);

            if(value != null)
                AppendOption(option, value);
        }

        protected TSelf WithArgument(int index, string value, bool quoted = false)
        {
            AppendArgument(index, value, quoted);
            return (TSelf) this;
        }

        protected void AppendArgument(int index, string value, bool quoted = false)
        {
            if(!this.arguments.ContainsKey(index))
                this.arguments.Add(index, new List<string>());

            if(quoted)
                value = string.Concat("\"", value, "\"");
            
            this.arguments[index].Add(value);
        }

        protected void AppendOption(string arg, object value)
        {
            if(!this.options.ContainsKey(arg))
                this.options.Add(arg, new List<string>());

            this.options[arg].Add(value?.ToString());
        }

        protected void SetArgument(int index, string value, bool quoted = false)
        {
            this.arguments.Remove(index);

            if(value != null)
                AppendArgument(index, value, quoted);
        }

        private static string HyphenateOption(string option)
        {
            option = option.Trim();
            if(string.IsNullOrEmpty(option)) return "";

            // Ensure the flag has at least one hyphen
            if(!option.StartsWith("-"))
                option = '-' + option;

            // If the flag name contains more than one character, ensure that it has a second hyphen
            if(option.Length > 2 && !option.StartsWith("--"))
                option = '-' + option;

            return option;
        }
        
        private static string FormatArgumentValue(string option, object value)
        {
            if(value == null)
                return option;
            else if(option.Length == 2)
                return option + " " + value.ToString();
            else
                return option + "=" + value.ToString();
        }

        GitCommandResult IGitCommand.ParseResult(int exitCode, string output, string error) => ParseResult(exitCode, output, error);

        IGitCommand IGitCommand.WithOption(string option, object value) => WithOption(option, value);

        IGitCommand IGitCommand.WithPathspec(params string[] pathspec) => WithPathspec(pathspec);

        public sealed override string ToString() => ToString(new StringBuilder());
    }

    public interface IGitCommand
    {
        string Name { get; }
        string[] Pathspec { get; }

        GitCommandResult ParseResult(int exitCode, string output, string error);

        // the temptation to upgrade to Unity 2021 for C# 9.0 and covariant return types :(
        IGitCommand WithOption(string option, object value = null);

        IGitCommand WithPathspec(params string[] pathspec);

        IReadOnlyList<string> GetOptionValues(string option);
        
        string GetOptionValue(string option);
        
        bool HasOption(string name);
        
        void SetOption(string option, object value);

        string ToString(StringBuilder builder);
    }
}
