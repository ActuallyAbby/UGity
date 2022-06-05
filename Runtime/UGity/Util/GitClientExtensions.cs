using System.Collections.Generic;

using Octothorpe.Ugity.Editor.Util;
using Octothorpe.UGity.Client;

namespace Octothorpe.UGity.Util
{
    // TODO: Eventually make this obsolete by implementing a central cache
    public static class GitClientExtensions
    {
        public static Dictionary<string, List<string>> GetBranchesPerRemote(this IGitClient client)
        {
            string[] lines = client.Execute(Git.Branch.WithOption("-r")).Lines;

            Dictionary<string, List<string>> branches = new Dictionary<string, List<string>>();
            
            foreach(string line in lines)
            {
                string[] parts = line.Trim().Split('/');
                
                string remote = parts[0];
                string branch = parts[1];

                if(!branches.ContainsKey(remote))
                    branches[remote] = new List<string>();

                branches[remote].Add(branch);
            }

            return branches;
        }

        public static ISet<string> GetUnmergedFiles(this IGitClient client)
        {
            GitCommandResult result = client.Execute(new GitCommand("ls-files").WithOption("-u"));
            ISet<string> set = new HashSet<string>(result.Lines);
            set.Remove("");

            return set;
        }
        
        public static string[] GetBranches(this IGitClient client)
        {
            return client.Execute(Git.Branch.WithOption("--format", "%(refname:short)")).Lines.TrimAll();
        }
        
        public static string[] GetRemotes(this IGitClient client)
        {
            return client.Execute(new GitCommand("remote")).Lines.TrimAll();
        }

        public static string[] GetRemoteBranches(this IGitClient client)
        {
            return client.Execute(Git.Branch.WithOption("-r")).Lines.TrimAll();
        }
        
        public static string GetBranchName(this IGitClient client)
        {
            GitCommandResult result = client.Execute(new GitCommand("rev-parse")
                .WithOption("--abbrev-ref")
                .WithArgument("HEAD"));

            if(result.Output == "HEAD")
                return null;
            else
                return result.Output.Trim();
        }
        
        public static string GetConfigOption(this IGitClient client, string option)
        {
            GitCommandResult result = client.Execute(new GitCommand("config").WithOption("--get").WithArgument(option));
            if(result.ExitCode == 0)
                return result.Output.Trim();
            else
                return null;
        }
        
        public static string GetLastCommitMessage(this IGitClient client)
        {
            GitCommand command = new GitCommand("log")
                .WithOption("-1")
                .WithOption("--pretty", "%B");

            return client.TryExecute(command)?.Output;
        }
        
        public static string GetHeadHash(this IGitClient client)
        {
            GitCommand command = new GitCommand("rev-parse")
                .WithOption("--short")
                .WithArgument("HEAD");

            return client.TryExecute(command)?.Output?.Trim();
        }

        public static string GetHeadDisplayName(this IGitClient client) => GetBranchName(client) ?? GetHeadHash(client);

        public static string GetUpstream(this IGitClient client, string branch = "")
        {
            return client.TryExecute(new GitCommand("rev-parse")
                .WithOption("--abbrev-ref")
                .WithOption("--symbolic-full-name")
                .WithArgument(branch + "@{upstream}"))?.Output?.Trim();
        }
        
        public static string ValidateBranchName(this IGitClient client, string name)
        {
            return client.Execute(new GitCommand("check-ref-format")
                    .WithOption("--branch")
                    .WithArgument(name, true)).Output;
        }

        public static bool CheckIgnore(this IGitClient client, string path)
        {
            return client.Execute(new GitCommand("check-ignore")
                .WithOption("-q")
                .WithArgument(path, true)).ExitCode == 0;
        }
    }
}
