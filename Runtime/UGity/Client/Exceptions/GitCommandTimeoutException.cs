namespace Octothorpe.UGity.Client
{
    public class GitCommandTimeoutExeption : GitClientException
    {
        public GitCommandTimeoutExeption(IGitCommand command) : base("Execution of command git " + command.Name + " timed out") { }
        
        public GitCommandTimeoutExeption(string message) : base(message) { }
    }
}
