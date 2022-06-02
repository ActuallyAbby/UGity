namespace Octothorpe.UGity.Client
{
    public class GitClientException : System.Exception
    {
        public GitClientException(GitCommandResult result) : this(result.Error ?? result.Output) { }

        public GitClientException(string message) : base(message) { }
    }
}
