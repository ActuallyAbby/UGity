namespace Octothorpe.UGity.Client
{
    public static partial class Git
    {
        public static GitRemoveCommand Remove => new GitRemoveCommand();
    }

    public class GitRemoveCommand : GitCommand<GitRemoveCommand, GitRemoveResult>
    {
        public GitRemoveCommand() : base("rm") { }

        public override void ParseExtendedResult(in GitRemoveResult result)
        {
            string[] files = new string[result.Lines.Length];
            for(int i = 0;i < result.Lines.Length;i++)
            {
                files[i] = result.Lines[i].Trim('\'');
            }

            result.Files = files;
        }
    }

    public class GitRemoveResult : GitCommandResult
    {
        public string[] Files { get; internal set; }
    }
}
