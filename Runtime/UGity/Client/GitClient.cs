using System;
using System.Diagnostics;
using System.Text;

namespace Octothorpe.UGity.Client
{
    public partial class GitClient : IGitClient
    {
        /// <summary>
        /// Default command timeout (in millis)
        /// </summary>
        private const int DEFAULT_TIMEOUT = 2000;

        /// <summary>
        /// Event fired when a line is written to standard output during command execution.</br>
        /// This event fires once and will be unregistered after the command has finished executing.
        /// </summary>
        public event GitOutputDelegate OnOutputLine;
        
        /// <summary>
        /// Event fired when a line is written to standard error during command execution.</br>
        /// This event fires once and will be unregistered after the command has finished executing.
        /// </summary>
        public event GitOutputDelegate OnErrorLine;

        /// <summary>
        /// Event fired when a command has finished executing.
        /// This event fires once after this next command is executed, and is then unregistered.
        /// </summary>
        public event Action OnFinishedExecuting;
        
        public event CommandExecuteDelegate OnCommandExecute;
        public event CommandResultDelegate OnCommandResult;

        public string WorkingDirectory
        {
            get => this.processInfo.WorkingDirectory;
            set => this.processInfo.WorkingDirectory = value;
        }

        private StringBuilder builder;
        private ProcessStartInfo processInfo;

        public delegate void GitOutputDelegate(string line);
        public delegate void CommandExecuteDelegate(string gitOptions, IGitCommand command);
        public delegate void CommandResultDelegate(IGitCommand command, GitCommandResult result);

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
        
        public virtual GitCommandResult Execute(IGitCommand command, string options = null, int timeout = DEFAULT_TIMEOUT)
        {
            return ExecuteInternal(command, options, timeout, false);
        }
        
        public virtual GitCommandResult TryExecute(IGitCommand command, string options = null, int timeout = DEFAULT_TIMEOUT)
        {
            return ExecuteInternal(command, options, timeout, true);
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
        
        private GitCommandResult ExecuteInternal(IGitCommand command, string options, int timeout, bool ignoreFatalErrors)
        {
            try
            {
                return ExecuteInternal1(command, options, timeout, ignoreFatalErrors);
            }
            finally
            {
                this.OnOutputLine = null;
                this.OnErrorLine = null;

                this.OnFinishedExecuting?.Invoke();
                this.OnFinishedExecuting = null;
            }
        }

        private GitCommandResult ExecuteInternal1(IGitCommand command, string options, int timeout, bool throwExceptions)
        {
            // Validate the paths provided to ensure that commands intended to target a specific path do not accidentally run with no path
            ValidatePathspec(command.Pathspec);

            this.builder.Clear();

            if(!string.IsNullOrWhiteSpace(options))
                this.builder.Append(options.Trim()).Append(" ");

            command.ToString(this.builder);

            this.processInfo.Arguments = this.builder.ToString();

            Process process = Process.Start(this.processInfo);

            StringBuilder outputBuilder = new StringBuilder();
            StringBuilder errorBuilder = new StringBuilder();

            process.OutputDataReceived += ForwardOutput;
            process.ErrorDataReceived += ForwardError;

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            this.OnCommandExecute?.Invoke(options, command);

            if(process.WaitForExit(timeout))
            {
                int exitCode = process.ExitCode;
                string output = outputBuilder.ToString().TrimEnd();
                string error = errorBuilder.ToString().TrimEnd();
                
                GitCommandResult result = command.ParseResult(exitCode, output, error);

                this.OnCommandResult?.Invoke(command, result);

                if(exitCode == 128)
                {
                    if(throwExceptions)
                        throw new GitFatalErrorException(error);
                    else
                        return null;
                }

                return result;

            }
            else
            {
                process.Kill();

                if(throwExceptions)
                    throw new GitCommandTimeoutExeption(command);
                else
                    return null;
            }

            void ForwardOutput(object sender, DataReceivedEventArgs args)
            {
                if(args.Data != null)
                {
                    outputBuilder.AppendLine(args.Data);
                    this.OnOutputLine?.Invoke(args.Data);
                }
            }

            void ForwardError(object sender, DataReceivedEventArgs args)
            {
                if(args.Data != null)
                {
                    errorBuilder.AppendLine(args.Data);
                    this.OnErrorLine?.Invoke(args.Data);
                }
            }
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
    }
}
