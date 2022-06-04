namespace Octothorpe.UGity.Client
{
    public static partial class Git
    {        
        public static GitCommand Add => new GitCommand("add");
        public static GitCommand Branch => new GitCommand("branch");
        public static GitCommand Checkout => new GitCommand("checkout");
        public static GitCommand Merge => new GitCommand("merge");
        public static GitCommand Rebase => new GitCommand("rebase");
        public static GitCommand Reset => new GitCommand("reset");
        public static GitCommand Restore => new GitCommand("restore");
        public static GitCommand Tag => new GitCommand("tag");
    }
}
