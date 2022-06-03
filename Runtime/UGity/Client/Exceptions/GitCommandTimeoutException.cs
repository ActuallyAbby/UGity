namespace Octothorpe.UGity.Client
{
    public class GitCommandTimeoutException : GitClientException
    {
        public GitCommandTimeoutException(IGitCommand command) : base("Execution of command git " + command.Name + " timed out") { }
        
        public GitCommandTimeoutException(string message) : base(message) { }
    }
}
