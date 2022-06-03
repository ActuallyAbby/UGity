namespace Octothorpe.UGity.Client
{
    /// <summary>
    /// Handles each command executed by the client
    /// </summary>
    /// <param name="gitOptions"><c>git</c> command options</param>
    /// <param name="command">Command that was executed</param>
    public delegate void CommandExecuteDelegate(string gitOptions, IGitCommand command);

    /// <summary>
    /// Handles the result of each command executed by the client
    /// </summary>
    /// <param name="command">Command that was executed</param>
    /// <param name="completed">Did execution complete successfully? (Did not terminate or time out)</param>
    /// <param name="result">Command result</param>
    public delegate void CommandResultDelegate(IGitCommand command, bool completed, GitCommandResult result);
    
    public interface IGitClient
    {
        /// <summary>
        /// Event to be fired each time a command is executed
        /// </summary>
        public event CommandExecuteDelegate OnCommandExecute;
        
        /// <summary>
        /// Event to be fired each time a command finishes executing
        /// </summary>
        public event CommandResultDelegate OnCommandResult;

        /// <summary>
        /// Execute a <see cref="GitCommand{TSelf, TResult}"/><br/>
        /// Implementations are advised to include the following details:<br/>
        /// <list type="bullet">
        ///     <item>
        ///         <term><see cref="OnCommandExecute"/></term>
        ///         <description>Should be invoked when the command executes</description>
        ///     </item>
        ///     <item>
        ///         <term><see cref="OnCommandResult"/></term>
        ///         <description>Should be invoked when the command finishes execution</description>
        ///     </item>
        ///     <item>
        ///         <term><see cref="GitFatalErrorException"/></term>
        ///         <description>Should be thrown if the command results in a fatal error (exit code 128)</description>
        ///     </item>
        ///     <item>
        ///         <term><see cref="GitCommandTimeoutException"/></term>
        ///         <description>Should be thrown if command execution duration exceeds the value of <c>timeout</c> millis</description>
        ///     </item>
        /// </list>
        /// </summary>
        /// <typeparam name="TSelf">Type of the <see cref="IGitCommand"/></typeparam>
        /// <typeparam name="TResult">Type of the <see cref="GitCommandResult"/></typeparam>
        /// <param name="command"><see cref="GitCommand{TSelf, TResult}"/> to execute</param>
        /// <param name="options">Options to be passed to <c>git</c></param>
        /// <param name="timeout">Time after which execution is forcefully terminated if it has not yet exited</param>
        /// <returns>The command parsed result</returns>
        TResult Execute<TSelf, TResult>(GitCommand<TSelf, TResult> command, string options = "", int timeout = 2000)
            where TSelf : GitCommand<TSelf, TResult>
            where TResult : GitCommandResult, new();

        /// <summary>
        /// Execute a <see cref="GitCommand{TSelf, TResult}"/><br/>
        /// Implementations are advised to include the following details:<br/>
        /// <list type="bullet">
        ///     <item>
        ///         <term><see cref="OnCommandExecute"/></term>
        ///         <description>Should be invoked when the command executes</description>
        ///     </item>
        ///     <item>
        ///         <term><see cref="OnCommandResult"/></term>
        ///         <description>Should be invoked when the command finishes execution</description>
        ///     </item>
        /// </list>
        /// This method should always return without throwing any exception derived from <see cref="GitClientException"/>
        /// </summary>
        /// <typeparam name="TSelf">Type of the <see cref="IGitCommand"/></typeparam>
        /// <typeparam name="TResult">Type of the <see cref="GitCommandResult"/></typeparam>
        /// <param name="command"><see cref="GitCommand{TSelf, TResult}"/> to execute</param>
        /// <param name="options">Options to be passed to <c>git</c></param>
        /// <param name="timeout">Time after which execution is forcefully terminated if it has not yet exited</param>
        /// <returns>The command parsed result, or <c>null</c> if execution failed</returns>
        TResult TryExecute<TSelf, TResult>(GitCommand<TSelf, TResult> command, string options = "", int timeout = 2000)
            where TSelf : GitCommand<TSelf, TResult>
            where TResult : GitCommandResult, new();

        /// <summary>
        /// Execute a non-generic <see cref="IGitCommand"/><br/>
        /// Implementations are advised to include the following details:<br/>
        /// <list type="bullet">
        ///     <item>
        ///         <term><see cref="OnCommandExecute"/></term>
        ///         <description>Should be invoked when the command executes</description>
        ///     </item>
        ///     <item>
        ///         <term><see cref="OnCommandResult"/></term>
        ///         <description>Should be invoked when the command finishes execution</description>
        ///     </item>
        ///     <item>
        ///         <term><see cref="GitFatalErrorException"/></term>
        ///         <description>Should be thrown if the command results in a fatal error (exit code 128)</description>
        ///     </item>
        ///     <item>
        ///         <term><see cref="GitCommandTimeoutException"/></term>
        ///         <description>Should be thrown if command execution duration exceeds the value of <c>timeout</c> millis</description>
        ///     </item>
        /// </list>
        /// </summary>
        /// <param name="command"><see cref="IGitCommand"/> to execute</param>
        /// <param name="options">Options to be passed to <c>git</c></param>
        /// <param name="timeout">Time after which execution is forcefully terminated if it has not yet exited</param>
        /// <returns>The command parsed result</returns>
        GitCommandResult Execute(IGitCommand command, string options = "", int timeout = 2000);

        /// <summary>
        /// Execute a non-generic <see cref="IGitCommand"/><br/>
        /// Implementations are advised to include the following details:<br/>
        /// <list type="bullet">
        ///     <item>
        ///         <term><see cref="OnCommandExecute"/></term>
        ///         <description>Should be invoked when the command executes</description>
        ///     </item>
        ///     <item>
        ///         <term><see cref="OnCommandResult"/></term>
        ///         <description>Should be invoked when the command finishes execution</description>
        ///     </item>
        /// </list>
        /// This method should always return without throwing any exception derived from <see cref="GitClientException"/>
        /// </summary>
        /// <param name="command"><see cref="IGitCommand"/> to execute</param>
        /// <param name="options">Options to be passed to <c>git</c></param>
        /// <param name="timeout">Time after which execution is forcefully terminated if it has not yet exited</param>
        /// <returns>The command parsed result, or <c>null</c> if execution failed</returns>
        GitCommandResult TryExecute(IGitCommand command, string options = "", int timeout = 2000);
    }
}
