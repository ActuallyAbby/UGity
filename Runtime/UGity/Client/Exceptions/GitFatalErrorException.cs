namespace Octothorpe.UGity.Client
{
    public class GitFatalErrorException : GitClientException
    {
        public GitFatalErrorException(string message) : base(message) { }
    }
}
