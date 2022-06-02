using System.Collections;
using System.Collections.Generic;

namespace Octothorpe.UGity.Client
{
    public static partial class Git
    {
        public static GitLogCommand Log => new GitLogCommand();
    }

    public class GitLogCommand : GitCommand<GitLogCommand, GitLogResult>
    {
        // %H = Commit hash
        // %h = Commit hash (Short)
        // %s = Commit message subject (First line of message)
        // %b = Commit message body
        // %cN = Committer current name
        // %cE = Committer current e-mail
        public const string PRETTY_FORMAT = "%H%x00%h%x00%s%x00%b%x00%cN%x00%cE";

        public GitLogCommand() : base("log") => WithOption("--pretty", $"format:{PRETTY_FORMAT}");

        public override void ParseExtendedResult(in GitLogResult result)
        {
            IList<CommitInfo> commits = new List<CommitInfo>();

            foreach(string line in result.Lines)
            {
                if(string.IsNullOrEmpty(line)) continue;
                commits.Add(ParseInfo(line));
            }

            result.Commits = commits;
        }

        public GitLogCommand WithRange(string begin, string end = "HEAD")
        {
            SetArgument(0, string.Concat(begin, "..", end));
            return this;
        }

        private static CommitInfo ParseInfo(string line)
        {
            string[] data = line.Split('\0');

            string longHash = data[0];
            string shortHash = data[1];
            string subject = data[2];
            string body = data[3];
            string committerName = data[4];
            string committerEmail = data[5];

            return new CommitInfo(longHash, shortHash, subject, body, committerName, committerEmail);
        }
    }

    public class GitLogResult : GitCommandResult, IEnumerable<CommitInfo>
    {
        internal IEnumerable<CommitInfo> Commits { get; set; }

        public new IEnumerator<CommitInfo> GetEnumerator() => Commits.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public struct CommitInfo
    {
        public string Hash { get; }
        public string ShortHash { get; }
        public string Subject { get; }
        public string Body { get; }
        public string CommitterName { get; }
        public string CommitterEmail { get; }

        public CommitInfo(string longHash, string shortHash, string subject, string body, string committerName, string committerEmail)
        {
            Hash = longHash;
            ShortHash = shortHash;
            Subject = subject;
            Body = body;
            CommitterName = committerName;
            CommitterEmail = committerEmail;
        }
    }
}
