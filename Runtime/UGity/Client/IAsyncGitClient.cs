using System.Threading.Tasks;
using System.Threading;

namespace Octothorpe.UGity.Client
{
    /// <summary>
    /// Handles a single line of text output by an asynchronously executing command
    /// </summary>
    /// <param name="line"></param>
    public delegate void AsyncOutputDelegate(string line);

    /// <summary>
    /// Handles termination (either via cancellation or by naturally exiting) of a command that was asynchronously executed
    /// </summary>
    /// <param name="result"></param>
    /// <param name="completed"></param>
    public delegate void AsyncTerminationDelegate(GitCommandResult result, bool completed);
    
    public interface IAsyncGitClient
    {
        /// <summary>
        /// Event to be fired when a line is written to standard output during the next asynchronous command execution.</br>
        /// This event fires once and will be unregistered after the command has finished executing.
        /// </summary>
        event AsyncOutputDelegate OnOutputLine;

        /// <summary>
        /// Event to be fired when a line is written to standard error during the next asynchronous command execution.</br>
        /// This event fires once and will be unregistered after the command has finished executing.
        /// </summary>
        event AsyncOutputDelegate OnErrorLine;

        /// <summary>
        /// Event to be fired when the next asynchronous command has finished executing.
        /// This event fires once after the next command is executed, and is then unregistered.
        /// </summary>
        event AsyncTerminationDelegate OnFinishedExecuting;
        
        Task<GitCommandResult> ExecuteAsync(IGitCommand command, string options = "", int timeout = 2000);
        
        Task<GitCommandResult> ExecuteAsync(IGitCommand command, string options = "", CancellationToken token = default);
    }
}
