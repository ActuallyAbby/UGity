namespace Octothorpe.UGity.Client
{
    public static partial class Git
    {
        public static GitPullCommand Pull => new GitPullCommand();
    }

    public class GitPullCommand : GitCommand<GitPullCommand, GitCommandResult>
    {
        private const int ARG_INDEX_REMOTE = 0;
        private const int ARG_INDEX_TARGET = 1;

        public GitPullCommand() : base("pull") { }

        public override void ParseExtendedResult(in GitCommandResult result) { }

        public GitPullCommand WithRemote(string remote)
        {
            SetArgument(ARG_INDEX_REMOTE, remote);
            return this;
        }

        public GitPullCommand WithRefspec(string src, string dst = null, bool force = false)
        {
            string value;
            if(src == null && dst == null)
                value = ":";
            else if(dst == null || src == dst)
                value = src;
            else if(src == null)
                value = ":" + dst;
            else
                value = string.Concat(src, ":", dst);

            if(force)
                value = "+" + value;

            WithArgument(ARG_INDEX_TARGET, value);
            return this;
        }
    }
}
