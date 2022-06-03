namespace Octothorpe.UGity.Client
{ 
    public interface IGitClient
    {
        /// <summary>
        /// Execute a <see cref="GitCommand{TSelf, TResult}"/><br/>
        /// Implementations are encouraged to throw the following exceptions:
        /// <list type="bullet">
        ///     <item>
        ///         <term><see cref="GitFatalErrorException"/></term>
        ///         <description>if the command results in a fatal error (exit code 128)</description>
        ///     </item>
        ///     <item>
        ///         <term><see cref="GitCommandTimeoutExeption"/></term>
        ///         <description>if command execution duration exceeds the value of <c>timeout</c> millis</description>
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
        /// Implementations are encouraged to throw the following exceptions:
        /// <list type="bullet">
        ///     <item>
        ///         <term><see cref="GitFatalErrorException"/></term>
        ///         <description>if the command results in a fatal error (exit code 128)</description>
        ///     </item>
        ///     <item>
        ///         <term><see cref="GitCommandTimeoutExeption"/></term>
        ///         <description>if command execution duration exceeds the value of <c>timeout</c> millis</description>
        ///     </item>
        /// </list>
        /// </summary>
        /// <param name="command"><see cref="IGitCommand"/> to execute</param>
        /// <param name="options">Options to be passed to <c>git</c></param>
        /// <param name="timeout">Time after which execution is forcefully terminated if it has not yet exited</param>
        /// <returns>The command parsed result</returns>
        GitCommandResult Execute(IGitCommand command, string options, int timeout);

        /// <summary>
        /// Execute a non-generic <see cref="IGitCommand"/><br/>
        /// This method should always return without throwing any exception derived from <see cref="GitClientException"/>
        /// </summary>
        /// <param name="command"><see cref="IGitCommand"/> to execute</param>
        /// <param name="options">Options to be passed to <c>git</c></param>
        /// <param name="timeout">Time after which execution is forcefully terminated if it has not yet exited</param>
        /// <returns>The command parsed result, or <c>null</c> if execution failed</returns>
        GitCommandResult TryExecute(IGitCommand command, string options, int timeout);
    }
}
