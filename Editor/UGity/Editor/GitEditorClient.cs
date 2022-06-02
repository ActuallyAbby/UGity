using System.Collections;
using System.Collections.Generic;
using System.IO;

using Octothorpe.UGity.Client;
using Octothorpe.UGity.Editor.Exceptions;
using Octothorpe.UGity.Editor.Persistence;
using Octothorpe.UGity.Editor.UI;
using Octothorpe.UGity.Editor.Util;

using UnityEditor;

using UnityEngine;
using UnityEngine.Networking;

using static Octothorpe.UGity.Editor.Logger;

namespace Octothorpe.UGity.Editor
{
    public partial class GitEditorClient : GitClient
    {
        const string IGNORE_TEMPLATE_URL = "https://raw.githubusercontent.com/github/gitignore/main/Unity.gitignore";

        public static bool IsInitialized { get; private set; }

        public static GitEditorClient Instance
        {
            get {
                if(instance == null)
                    instance = new GitEditorClient();

                return instance;
            }
        }
        
        public GitEditorState State
        {
            get {
                if(this.state == null)
                    this.state = GitEditorState.Load();

                return this.state == null ? throw new GitEditorException("Failed to load editor state") : this.state;
            }
        }

        private static GitEditorClient instance;

        private GitEditorState state;

        public GitEditorClient() : base(Directory.GetCurrentDirectory())
        {
            OnCommandExecute += GitConsoleWindow.LogCommand;
            OnCommandResult += GitConsoleWindow.LogCommandOutput;
        }

        [InitializeOnLoadMethod]
        protected static void Initialize()
        {
            try
            {
                Instance.Execute(new GitCommand("rev-parse").WithOption("--is-inside-work-tree"));
                IsInitialized = true;
            }
            catch(GitFatalErrorException)
            {
                Log("No git repository found. Use Git > Init to create one at the project root ({0})", Instance.WorkingDirectory);
            }
        }

        public void SaveState()
        {
            if(this.state != null)
                EditorUtility.SetDirty(this.state);
        }

        public GitFile GetStatus(Object asset, bool untracked = true, bool ignored = false) => GetStatus(AssetDatabase.GetAssetPath(asset), untracked, ignored);

        public GitFile GetStatus(string path, bool untracked = true, bool ignored = false)
        {
            return Execute(Git.Status
                .WithOptionIf(untracked, "--untracked-files")
                .WithOptionIf(ignored, "--ignored")
                .WithPathspec(path)).First.Value;
        }

        public bool AddAssets(IEnumerable<string> paths, bool force = false)
        {
            bool success = true;
            foreach(string path in paths)
            {
                if(!AddAsset(path, force))
                    success = false;
            }

            return success;
        }

        public bool AddAsset(Object asset, bool force = false) => AddAsset(AssetDatabase.GetAssetPath(asset), force);

        public bool AddAsset(string assetPath, bool force = false)
        {
            bool success = AddFileInternal(assetPath, force) && AddFileInternal(assetPath + ".meta", force);

            if(success)
                Log("Staged asset {0}", assetPath);
            else
                Log(LogLevel.Error, "Failed to stage asset {0}. See Git > Log for details", assetPath);

            return success;
        }

        public bool AddFiles(IEnumerable<string> paths, bool force = false)
        {
            bool success = true;
            foreach(string path in paths)
            {
                if(!AddFile(path, force))
                    success = false;
            }

            return success;
        }

        public bool AddFile(string path, bool force = false)
        {
            bool success = AddFileInternal(path, force);

            if(success)
                Log("Staged file {0}", path);
            else
                Log(LogLevel.Error, "Failed to stage file {0}. See Git > Log for details", path);

            return success;
        }

        public bool RemoveAsset(Object asset, bool force = false) => RemoveAsset(AssetDatabase.GetAssetPath(asset), force);

        public bool RemoveAsset(string assetPath, bool force = false) => RemoveFileInternal(assetPath, force) && RemoveFileInternal(assetPath + ".meta", force);

        public bool RemoveFiles(IEnumerable<string> paths, bool force = false)
        {
            bool success = true;
            foreach(string path in paths)
            {
                if(!RemoveFile(path, force))
                    success = false;
            }

            return success;
        }

        public bool RemoveFile(string path, bool force = false)
        {
            bool success = RemoveFileInternal(path, force);

            if(!success)
                Log(LogLevel.Error, "Failed to remove file {0} from git. See Git > Log for details", path);

            return success;
        }

        public bool RestoreAssets(IEnumerable<string> paths)
        {
            bool success = true;
            foreach(string path in paths)
            {
                if(!RestoreAsset(path))
                    success = false;
            }

            return success;
        }

        public bool RestoreAsset(Object asset) => RestoreAsset(AssetDatabase.GetAssetPath(asset));

        public bool RestoreAsset(string assetPath) => RestoreFileInternal(assetPath) && RestoreFileInternal(assetPath + ".meta");

        public bool RestoreFiles(IEnumerable<string> paths)
        {
            bool success = true;
            foreach(string path in paths)
            {
                if(!RestoreFile(path))
                    success = false;
            }

            return success;
        }

        public bool RestoreFile(string path)
        {
            bool success = RestoreFileInternal(path);

            if(!success)
                Log(LogLevel.Error, "Failed to restore file {0}. See Git > Log for details", path);

            return success;
        }

        public void InitRepository()
        {
            Execute(new GitCommand("init"));
            EditorUtil.StartCoroutine(Instance.FetchIgnoreTemplate(), () => Log("Local repository initialized!"));
        }
        
        public void Pull()
        {
            try
            {
                OnOutputLine += OnOutput;
                OnErrorLine += OnError;
                OnFinishedExecuting += OnExit;
                
                GitCommandResult result = Execute(new GitCommand("pull").WithOption("--all").WithOption("--dry-run").WithOption("--progress"), timeout: 20000);
            }
            catch(GitFatalErrorException e)
            {
                EditorUtility.DisplayDialog("Pull Failed", e.Message, "Ok");
            }

            void OnOutput(string line)
            {
                Debug.Log(line);
            }

            void OnError(string line)
            {
                Debug.LogWarning(line);
            }

            void OnExit()
            {
                Debug.Log("Finished executing");
            }
        }

        private IEnumerator FetchIgnoreTemplate()
        {
            Log("Fetching Unity .gitignore from {0}", IGNORE_TEMPLATE_URL);

            using(UnityWebRequest request = UnityWebRequest.Get(IGNORE_TEMPLATE_URL))
            {
                string filePath = Path.Combine(WorkingDirectory, ".gitignore");

                var downloadHandler = new DownloadHandlerFile(filePath);
                downloadHandler.removeFileOnAbort = true;
                request.downloadHandler = downloadHandler;

                yield return request.SendWebRequest();
                
                if((UnityWebRequest.Result.ProtocolError | UnityWebRequest.Result.ConnectionError).HasFlag(request.result))
                    throw new GitEditorException("Failed to download .gitignore");
            }
        }

        private bool RestoreFileInternal(string path)
        {
            return Execute(Git.Restore
                .WithOption("-W")
                .WithOption("-S")
                .WithPathspec(path)).ExitCode == 0;
        }

        private bool AddFileInternal(string path, bool force)
        {
            return Execute(Git.Add
                .WithOptionIf(force, "--force")
                .WithPathspec(path)).ExitCode == 0;
        }

        private bool RemoveFileInternal(string path, bool force)
        {
            return Execute(Git.Remove
                .WithOption("--ignore-unmatch")
                .WithOption("--cached")
                .WithOptionIf(force, "-f")
                .WithPathspec(path)).ExitCode == 0;
        }
    }
}
