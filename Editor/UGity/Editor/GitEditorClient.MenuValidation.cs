using Octothorpe.UGity.Client;
using Octothorpe.UGity.Util;

using UnityEditor;

namespace Octothorpe.UGity.Editor
{
    public partial class GitEditorClient
    {
        private static bool isItemSelected;
        private static GitFile currentFile;

        [MenuItem(MENU_GIT_COMMIT, validate = true)]
        protected static bool OpenCommitWindowValidate() => IsInitialized;

        [MenuItem(MENU_GIT_PUSH, validate = true)]
        protected static bool GitPushValidate()
        {
            // TODO: Eventually cache current branch information
            return Instance.GetBranchName() != null;
        }

        [MenuItem(MENU_GIT_PULL, validate = true)]
        protected static bool GitPullValidate() => Instance.GetBranchName() != null;

        [MenuItem(MENU_GIT_INIT, validate = true)]
        protected static bool GitInitValidate() => !IsInitialized;

        [MenuItem(MENU_ASSET_GIT_ADD, validate = true)]
        protected static bool GitAddValidate(MenuCommand command)
        {
            isItemSelected = (Selection.activeObject != null);
            if(!isItemSelected) return false;

            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            currentFile = Instance.GetStatus(path, true, true);

            // Disable if the file is ignored
            if(currentFile.IsIgnored) return false;

            // Enable if the file is not unmodified in EITHER the working tree or the index
            return (currentFile.StateInTree != GitState.Unmodified);
        }

        [MenuItem(MENU_ASSET_GIT_ADD_FORCE, validate = true)]
        protected static bool GitAddFValidate()
        {
            if(!isItemSelected) return false;

            Logger.Log("State: " + currentFile.StateInTree);

            // Enable if the file is modified in EITHER the working tree or the index
            return (currentFile.StateInTree != GitState.Unmodified);
        }

        [MenuItem(MENU_ASSET_GIT_ROLLBACK, validate = true)]
        protected static bool GitRollbackValidate(MenuCommand command)
        {
            if(!isItemSelected) return false;

            // Enable if the file is modified in the index
            return currentFile.StateInIndex != GitState.Unmodified;
        }
    }
}
