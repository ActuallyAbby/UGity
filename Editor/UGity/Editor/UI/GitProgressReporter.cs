using Octothorpe.UGity.Client;
using System.Text.RegularExpressions;
using System.Threading;

using UnityEditor;

namespace Octothorpe.UGity.Editor.UI
{
    public class GitProgressReporter : CancellationTokenSource
    {
        private const string PCT_PATTERN = @"(\d{1,3})%";
        private static readonly Regex pctRegex = new Regex(PCT_PATTERN, RegexOptions.Compiled);

        public bool Cancelled { get; private set; }

        private int taskId;

        private int stage;
        private int maxStages;
        private float stageProgress;
        
        public enum Task
        {
            Push = 4,
            Pull = 4,
        }

        public GitProgressReporter(IAsyncGitClient client, Task task, string taskName)
        {
            this.taskId = Progress.Start(taskName);
            Progress.RegisterCancelCallback(this.taskId, CancelTask);

            this.stage = 1;
            this.maxStages = (int) task;

            client.OnErrorLine += UpdateProgress;
            client.OnFinishedExecuting += (r, c) => Complete();
        }

        public void Complete() => Progress.Remove(this.taskId);

        private bool CancelTask()
        {
            Cancelled = true;
            this.Cancel();
            return true;
        }

        public void UpdateProgress(string line)
        {
            Match match = pctRegex.Match(line);
            if(!match.Success) return;

            string percentString = match.Groups[1].Value;
            
            float stagePct = (float.Parse(percentString) / 100f);
            if(stagePct < this.stageProgress)
            {
                this.stage++;
            }
            
            this.stageProgress = stagePct;

            float completedStages = this.stage - 1;
            float totalPct = (completedStages * (1f / this.maxStages)) + (stagePct * (1f / this.maxStages));

            Progress.Report(this.taskId, totalPct, line);
        }
    }
}
