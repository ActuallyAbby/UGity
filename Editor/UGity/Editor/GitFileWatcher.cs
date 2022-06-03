using System.Collections.Generic;

using Octothorpe.UGity.Client;
using Octothorpe.UGity.Editor.Persistence;
using Octothorpe.UGity.Editor.Util;
using Octothorpe.UGity.Util;

using UnityEditor;
using UnityEditor.Callbacks;

namespace Octothorpe.UGity.Editor
{
    public class GitFileWatcher : UnityEditor.AssetModificationProcessor
    {
        const string PROMPT_FORMAT_IGNORE = @"
Tracked file:
{0}

Would be ignored at new location:
{1}

Do you want to remove this file from git?
";
        
        const string PROMPT_FORMAT_ADD = @"
Would you like to add the following asset to git?
{0}

(You can add them later by selecting Git > Add from the context menu)
";

        private static GitEditorClient client;

        private static Queue<string> createdAssets;
        private static int lastAddTime;

        [DidReloadScripts(10)]
        public static void Initialize()
        {
            client = GitEditorClient.Instance;
            createdAssets = new Queue<string>();
            EditorApplication.update += OnEditorUpdate;
        }

        private static void OnEditorUpdate()
        {
            if(System.Environment.TickCount - lastAddTime < 250) return;
            if(createdAssets.Count == 0) return;

            int option = 0;
            if(!client.State.AlwaysAdd)
                option = PromptAddAssets(createdAssets);

            string nextAsset = createdAssets.Dequeue();

            switch(option)
            {
                case 1: break;
                case 2: client.State.AlwaysAdd = true; goto case 0;
                case 0: client.AddAsset(nextAsset); break;
            }
        }

        protected static void OnWillCreateAsset(string path)
        {
            if(client == null) return;

            if(path.StartsWith(GitEditorState.STATE_FILE))
                return;

            if(!path.EndsWith(".meta"))
                return;

            path = EditorUtil.GetAssetObjectPath(path);
            if(client.CheckIgnore(path)) return;

            createdAssets.Enqueue(path);
            lastAddTime = System.Environment.TickCount;
        }

        protected static AssetDeleteResult OnWillDeleteAsset(string path, RemoveAssetOptions options)
        {
            client.RemoveAsset(path);
            return AssetDeleteResult.DidNotDelete;
        }

        protected static AssetMoveResult OnWillMoveAsset(string oldPath, string newPath)
        {
            GitFile file = client.Execute(Git.Status
                .WithOption("--untracked-files")
                .WithOption("--ignored")
                .WithPathspec(oldPath)).First.Value;

            bool oldPathIsStaged = (file.StateInIndex == GitState.Added) || (file.StateInIndex == GitState.Modified);
            bool newPathIsIgnored = client.CheckIgnore(newPath);

            bool removeAfterMove = false;

            if(oldPathIsStaged && newPathIsIgnored)
            {
                int choice = PromptMoveAsset(oldPath, newPath);

                if(choice == 1)
                    return AssetMoveResult.FailedMove;
                else if(choice == 0)
                    removeAfterMove = true;
            }

            //git mv the file and its associated meta file (if it exists) to the new location
            client.Execute(new GitCommand("mv").WithArgument(oldPath, true).WithArgument(newPath, true));
            client.TryExecute(new GitCommand("mv").WithArgument(oldPath + ".meta", true).WithArgument(newPath + ".meta", true));

            if(removeAfterMove)
                client.RemoveAsset(newPath);

            return AssetMoveResult.DidMove;
        }

        private static int PromptMoveAsset(string oldPath, string newPath)
        {
            string message = string.Format(PROMPT_FORMAT_IGNORE, oldPath, newPath);
            return EditorUtility.DisplayDialogComplex("Tracked State Mismatch", message, "Remove", "Cancel Move", "Keep Added");
        }

        private static int PromptAddAssets(IEnumerable<string> paths)
        {
            if(paths == null) return -1;

            int assetCount = createdAssets.Count;
            if(assetCount == 0) return -1;

            string remainingAssets = "-> " + string.Join("\n", paths);
            string message = string.Format(PROMPT_FORMAT_ADD, remainingAssets);

            return EditorUtility.DisplayDialogComplex("New Asset(s) Found", message, "Add", "Skip", assetCount > 1 ? "Add All" : null);
        }
    }
}
