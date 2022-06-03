using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Octothorpe.UGity.Client
{
    public partial class GitClient : IGitClient, IAsyncGitClient
    {
        /// <summary>
        /// Default command timeout (in millis)
        /// </summary>
        private const int DEFAULT_TIMEOUT = 2000;

        /// <summary>
        /// Period in which asynchronously executed commands check for cancellation
        /// </summary>
        private const int ASYNC_POLL_RATE = 500;

        public event AsyncOutputDelegate OnOutputLine;
        public event AsyncOutputDelegate OnErrorLine;
        public event AsyncTerminationDelegate OnFinishedExecuting;

        public event CommandExecuteDelegate OnCommandExecute;
        public event CommandResultDelegate OnCommandResult;

        public string WorkingDirectory
        {
            get => this.processInfo.WorkingDirectory;
            set => this.processInfo.WorkingDirectory = value;
        }

        private StringBuilder builder;
        private ProcessStartInfo processInfo;
        
        public GitClient(string workingDirectory = "/")
        {
            this.processInfo = new ProcessStartInfo();
            this.processInfo.CreateNoWindow = true;
            this.processInfo.UseShellExecute = false;
            this.processInfo.FileName = "git";
            this.processInfo.WorkingDirectory = workingDirectory;
            this.processInfo.RedirectStandardOutput = true;
            this.processInfo.RedirectStandardError = true;

            this.builder = new StringBuilder();
        }

        public virtual async Task<GitCommandResult> ExecuteAsync(IGitCommand command, string options = "", int timeout = DEFAULT_TIMEOUT)
        {
            using(var source = new CancellationTokenSource())
            {
                source.CancelAfter(timeout);
                return await ExecuteAsync(command, options, source.Token);
            }
        }

        public virtual async Task<GitCommandResult> ExecuteAsync(IGitCommand command, string options = "", CancellationToken token = default)
        {
            Process process = StartCommandProcess(command, options);

            StringBuilder outputBuilder = new StringBuilder();
            StringBuilder errorBuilder = new StringBuilder();

            AsyncOutputDelegate outFuncs = this.OnOutputLine;
            AsyncOutputDelegate errFuncs = this.OnErrorLine;
            AsyncTerminationDelegate exitFuncs = this.OnFinishedExecuting;

            this.OnOutputLine = null;
            this.OnErrorLine = null;
            this.OnFinishedExecuting = null;

            process.OutputDataReceived += ForwardOutput;
            process.ErrorDataReceived += ForwardError;

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            while(true)
            {
                if(process.HasExited)
                {
                    GitCommandResult result = GetCompletedResult(command, process.ExitCode, outputBuilder.ToString(), errorBuilder.ToString(), true);
                    exitFuncs?.Invoke(result, true);
                    return result;
                }
                else if(token.IsCancellationRequested)
                {
                    process.Kill();
                    exitFuncs?.Invoke(null, false);
                    return GetTimedOutResult(command, true);
                }

                await Task.Delay(ASYNC_POLL_RATE);
            }

            void ForwardOutput(object sender, DataReceivedEventArgs args)
            {
                if(args.Data != null)
                {
                    outputBuilder.AppendLine(args.Data);
                    outFuncs?.Invoke(args.Data);
                }
            }

            void ForwardError(object sender, DataReceivedEventArgs args)
            {
                if(args.Data != null)
                {
                    errorBuilder.AppendLine(args.Data);
                    errFuncs?.Invoke(args.Data);
                }
            }
        }
        
        public virtual TResult Execute<TSelf, TResult>(GitCommand<TSelf, TResult> command, string options = "", int timeout = DEFAULT_TIMEOUT)
            where TSelf : GitCommand<TSelf, TResult>
            where TResult : GitCommandResult, new()
        {
            return (TResult) Execute((IGitCommand) command, options, timeout);
        }

        public virtual TResult TryExecute<TSelf, TResult>(GitCommand<TSelf, TResult> command, string options = "", int timeout = DEFAULT_TIMEOUT)
            where TSelf : GitCommand<TSelf, TResult>
            where TResult : GitCommandResult, new()
        {
            return (TResult) TryExecute((IGitCommand) command, options, timeout);
        }
        
        public virtual GitCommandResult Execute(IGitCommand command, string options = "", int timeout = DEFAULT_TIMEOUT)
        {
            return ExecuteSyncInternal(command, options, timeout, true);
        }
        
        public virtual GitCommandResult TryExecute(IGitCommand command, string options = "", int timeout = DEFAULT_TIMEOUT)
        {
            return ExecuteSyncInternal(command, options, timeout, false);
        }
        
        private static void ValidatePathspec(string[] pathspec)
        {
            // If no pathspec was set at all, everything is fine
            if(pathspec == null) return;

            // If a pathspec was set, verify that it contains non-empty elements
            foreach(string path in pathspec)
            {
                if(!string.IsNullOrWhiteSpace(path)) return;
            }

            throw new GitEmptyPathspecException($"Pathspec was provided, but it was empty!");
        }

        private GitCommandResult ExecuteSyncInternal(IGitCommand command, string options, int timeout, bool throwExceptions)
        {
            Process process = StartCommandProcess(command, options);
            
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();

            if(process.WaitForExit(timeout))
            {
                return GetCompletedResult(command, process.ExitCode, output, error, throwExceptions);
            }
            else
            {
                process.Kill();
                return GetTimedOutResult(command, throwExceptions);
            }
        }

        private GitCommandResult GetCompletedResult(IGitCommand command, int exitCode, string output, string error, bool throwExceptions)
        {
            GitCommandResult result = command.ParseResult(exitCode, output.TrimEnd(), error.TrimEnd());
            this.OnCommandResult?.Invoke(command, true, result);

            if(exitCode == 128)
                return (throwExceptions) ? throw new GitFatalErrorException(error) : (GitCommandResult) null;

            return result;
        }

        private GitCommandResult GetTimedOutResult(IGitCommand command, bool throwExceptions)
        {
            this.OnCommandResult?.Invoke(command, false, null);
            return throwExceptions ? throw new GitCommandTimeoutException(command) : (GitCommandResult) null;
        }

        private Process StartCommandProcess(IGitCommand command, string options)
        {
            // Validate the paths provided to ensure that commands intended to target a specific path do not accidentally run with no path
            ValidatePathspec(command.Pathspec);

            this.builder.Clear();

            if(!string.IsNullOrWhiteSpace(options))
                this.builder.Append(options.Trim()).Append(" ");

            command.ToString(this.builder);

            this.processInfo.Arguments = this.builder.ToString();
            this.OnCommandExecute?.Invoke(options, command);

            return Process.Start(this.processInfo);
        }
    }
}
