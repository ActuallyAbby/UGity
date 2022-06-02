using System.Text;

namespace Octothorpe.UGity.Client
{
    public static partial class Git
    {
        public static GitCommitCommand Commit => new GitCommitCommand().WithOption("--dry-run");
    }

    public class GitCommitCommand : GitCommand<GitCommitCommand, GitCommandResult>
    {
        public bool Amending
        {
            get => HasOption("--amend");
            set {
                SetOption("--amend", null);

                if(value == true)
                    WithOption("--amend");

            }
        }

        /// <summary>
        /// <para>
        /// <c>get</c>:<br/>
        /// Gets the current commit message. If no message has yet been specified, returns <c>null</c><br/>
        /// If multiple message options are present, the values with be separated by a newline character (<c>'\n'</c>)<br/>
        /// <br/>
        /// </para>
        /// <c>set</c>:<br/>
        /// Sets or adds to the current message<br/>
        /// If the value is <c>null</c>, any existing <c>-m</c> and <c>--message</c> options are cleared<br/>
        /// If the new value is appending to the old value, such as by doing:
        /// <code>
        ///     Message = Message + "another message"
        /// </code>
        /// Then the appended portion of the string will be added as a separate <c>-m</c> option. For example:
        /// <code>
        /// var commit = new GitCommitCommand();
        /// commit.Message = "message 1";
        /// commit.Message += "message 2";
        /// Console.WriteLine(commit.ToString());
        /// </code>
        /// Results in an output of:
        /// <code>
        /// commit -m "message 1" -m "message 2"
        /// </code>
        /// If the value is not appending to the old value, any existing message options are replaced with a single new message option
        /// </summary>
        public string Message
        {
            get {
                StringBuilder message = new StringBuilder();

                if(HasOption("-m"))
                    message.Append(string.Join("\n", GetOptionValues("-m")));

                if(HasOption("--message"))
                    message.Append(string.Join("\n", GetOptionValues("--message")));

                return message.Length == 0 ? null : message.ToString();
            }
            set {
                string currentMessage = Message;
                if(value == currentMessage) return;

                if(value.StartsWith(Message))
                {
                    value = value.Substring(Message.Length);
                }
                else
                {
                    SetOption("-m", null);
                    SetOption("--message", null);
                }
                
                if(value != null)
                    WithMessage(value);
            }
        }

        public GitCommitCommand(string message = null, bool amend = false) : base("commit")
        {
            if(message != null)
                WithMessage(message);

            Amend(amend);
        }

        public override void ParseExtendedResult(in GitCommandResult result) { }

        public GitCommitCommand Amend(bool amend = true)
        {
            WithOptionIf(amend, "--amend");
            return this;
        }

        public GitCommitCommand WithMessage(string message)
        {
            WithOption("-m", "\"" + message + "\"");
            return this;
        }
    }
}
