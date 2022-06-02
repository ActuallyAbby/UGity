namespace Octothorpe.UGity.Client
{ 
    public interface IGitClient
    {
        TResult Execute<TSelf, TResult>(GitCommand<TSelf, TResult> command, string options = "", int timeout = 2000)
            where TSelf : GitCommand<TSelf, TResult>
            where TResult : GitCommandResult, new();
        
        GitCommandResult Execute(IGitCommand command, string options, int timeout);
    }
}
