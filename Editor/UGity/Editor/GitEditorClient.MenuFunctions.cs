using Octothorpe.UGity.Editor.UI;

using UnityEditor;

namespace Octothorpe.UGity.Editor
{
    public partial class GitEditorClient
    {
        [MenuItem(MENU_GIT_COMMIT, priority = 1)]
        protected static void OpenCommitWindow() => GitCommitWindow.Open();

        [MenuItem(MENU_GIT_PUSH, priority = 20)]
        protected static void GitPush() => GitPushWindow.Open();

        [MenuItem(MENU_GIT_PULL, priority = 21)]
        protected static void GitPullQuick() => GitPullWindow.Open(true);

        [MenuItem(MENU_GIT_CONSOLE, priority = 50)]
        protected static void OpenConsoleWindow() => GitConsoleWindow.Open(false);

        [MenuItem(MENU_GIT_INIT, priority = 100)]
        protected static void GitInit() => Instance.InitRepository();

        [MenuItem(MENU_ASSET_GIT_ADD_FORCE, priority = 1)]
        protected static void GitAddFMenu() => Instance.AddAsset(Selection.activeObject, true);

        [MenuItem(MENU_ASSET_GIT_ADD, priority = 0)]
        protected static void GitAddMenu() => Instance.AddAsset(Selection.activeObject);

        //[MenuItem(MENU_ASSET_GIT_ROLLBACK, priority = 21)]
        //protected static void GitRollbackMenu() { }
    }
}
