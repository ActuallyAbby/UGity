using System.Collections.Generic;
using System.Text;

using Octothorpe.UGity.Client;
using Octothorpe.UGity.Editor.Persistence;
using Octothorpe.UGity.Editor.Util;
using Octothorpe.UGity.Util;

using UnityEditor;
using UnityEditor.IMGUI.Controls;

using UnityEngine;

namespace Octothorpe.UGity.Editor.UI
{
    public class GitCommitWindow : GitEditorUtilityWindow<GitCommitWindow>
    {
        private const string MESSAGE_PLACEHOLDER = "Enter a commit message...";
        private const string MESSAGE_DEFAULT = "Initial commit";

        protected override string Title { get; set; } = "Commit Changes";
        protected override Texture Icon => GitEditorStyles.CommitWindowIcon;
        protected override bool HasHeader { get; } = false;

        private bool IsAmending
        {
            get => this.state.Amend;
            set {
                if(this.state.Amend == value) return;
                this.state.Amend = value;

                if(value == true)
                {
                    this.lastCommitMessage = GitClientExtensions.GetLastCommitMessage(Client);
                    this.amendMessage = this.lastCommitMessage;
                }
            }
        }

        private string CommitMessage
        {
            get => IsAmending ? this.amendMessage : this.state.Message;
            set {
                if(IsAmending)
                    this.amendMessage = value;
                else
                    this.state.Message = value;
            }
        }

        [SerializeField]
        private string amendMessage;

        private GitTreeView tree;
        private GitCommitState state;
        private SearchField search;

        private string lastCommitHash;
        private string lastCommitMessage;
        private string branchName;

        private GUIContent amendLabel;

        protected override void OnOpen() => minSize = new Vector2(250f, 0f);

        protected override void OnInitialize()
        {
            this.state = Client.State.LoadSubState<GitCommitState>();

            this.lastCommitHash = GitClientExtensions.GetHeadHash(Client);
            if(this.lastCommitHash == null)
                CommitMessage = MESSAGE_DEFAULT;

            IsAmending = this.state.Amend;

            this.tree = new GitTreeView(this.state);
            this.search = new SearchField();

            this.amendLabel = new GUIContent("Amend");
            
            Refresh();
        }

        protected override void OnPostInitialize() => Refresh();

        protected override void OnUninitialize()
        {
            if(this.state != null)
                EditorUtility.SetDirty(this.state);
        }

        protected override void OnRefocus()
        {
            AssetDatabase.SaveAssets();
            Refresh();

            if(IsAmending)
                this.lastCommitMessage = GitClientExtensions.GetLastCommitMessage(Client);
        }

        protected override void OnDraw()
        {
            GUIContent commitText = new GUIContent(GetCommitButtonText());

            // Draw the toolbar
            DrawToolbar(EditorGUILayout.GetControlRect(false, 24f));

            // Draw the tree & search
            DrawTree();

            bool canCommit = CanCommit(out string warning);
            bool canAmend = (this.lastCommitHash != null);

            if(!canCommit)
                EditorGUILayout.HelpBox(warning, MessageType.Warning, true);
            else
                GUILayout.Label(warning, GUIStyle.none, GUILayout.Width(0f), GUILayout.Height(0f));
            
            using(new EditorGUI.DisabledScope(!canAmend))
                IsAmending = EditorGUILayout.ToggleLeft(this.amendLabel, IsAmending);
            
            CommitMessage = EditorUtil.AddPlaceholder(EditorGUILayout.TextArea(CommitMessage, GUILayout.Height(128f)), MESSAGE_PLACEHOLDER);
            
            using(new EditorGUI.DisabledScope(!canCommit))
                if(GUILayout.Button(commitText, GitEditorStyles.ConfirmButton))
                    InitiateCommit();
        }

        private string GetCommitButtonText()
        {
            if(IsAmending && this.tree.GetIncludedPaths().Count == 0)
                return ("Update commit message for " + this.lastCommitHash);
            else if(IsAmending)
                return ("Amend commit on " + this.branchName);
            else
                return ("Commit on " + this.branchName);
        }

        private bool CanCommit(out string failureMessage)
        {
            // Can't commit with an empty message
            if(string.IsNullOrWhiteSpace(CommitMessage))
            {
                failureMessage = "Please enter a commit message";
                return false;
            }
            // Can't commit if amending and nothing has been changed
            if(this.tree.GetIncludedPaths().Count == 0)
            {
                if(!IsAmending || CommitMessage == this.lastCommitMessage)
                {
                    failureMessage = "No changes included";
                    return false;
                }
            }

            failureMessage = null;
            return true;
        }

        private bool ConfirmCommit(GitCommitCommand command, ISet<GitFile> included)
        {
            var modal = GitCommitModal.Create();
            modal.minSize = new Vector2(450f, 600f);

            return modal.GetResult((this.branchName, command, included));
        }

        private void Commit(GitCommitCommand command, ISet<GitFile> included)
        {
            List<string> addForCommit = new List<string>();
            List<string> resetForCommit = new List<string>();

            foreach(GitFile change in Client.Execute(Git.Status))
            {
                bool isInCommit = included.Contains(change);

                // File is included, and changes from the working tree have been staged. Do nothing
                if(isInCommit && change.StateInTree == GitState.Unmodified)
                    continue;

                // File is included, and has changed in the working tree. Keep track of it so we can stage the changes
                else if(isInCommit && change.StateInTree != GitState.Unmodified)
                    addForCommit.Add(change.Path);

                // File is NOT included, and has staged changes. Keep track of it so we can temporarily remove it
                else if(!isInCommit && !change.IsUntracked && change.StateInIndex != GitState.Unmodified)
                    resetForCommit.Add(change.Path);
            }

            // Add files that need to be added
            foreach(string path in addForCommit)
                Client.TryExecute(Git.Add.WithPathspec(path));

            // Reset files that are already added, but not in the commit
            foreach(string path in resetForCommit)
                Client.TryExecute(Git.Reset.WithPathspec(path));

            // Execute the commit command and display a success message
            try
            {
                Client.Execute(command);

                IsAmending = false;
                CommitMessage = "";
                
                this.lastCommitHash = Client.GetHeadHash();
                EditorUtility.DisplayDialog("Success!", $"{included.Count} file(s) committed\nHEAD is now at {this.lastCommitHash}", "Close");
            }
            // Display a failure message if execution of the commit command threw an exception
            catch(GitClientException e)
            {
                EditorUtility.DisplayDialog("Commit Failed", e.Message, "Close");
            }
            // Ensure the reset files are always re-added
            finally
            {
                foreach(string path in resetForCommit)
                    Client.TryExecute(Git.Add.WithOption("--ignore-errors").WithPathspec(resetForCommit));

                Refresh();
            } 
        }

        private void DrawToolbar(Rect rect)
        {
            const int elements = 5;
            float elementWidth = rect.width / elements;

            EditorGUI.DrawRect(rect, new Color(0.1f, 0.1f, 0.1f));

            if(Button("TreeEditor.Refresh", "Refresh"))
                Refresh();

            if(Button("Toolbar Minus", "Un-stage Selected", this.tree.CanRemoveSelected))
            {
                Client.RemoveFiles(this.tree.GetSelectedPaths(), true);
                Refresh();
            }

            if(Button("file-restore", "Restore Selected to Previous Revision", this.tree.CanRestoreSelected))
                RestoreWithPrompt();

            if(Button("Toolbar Plus", "Stage Selected", this.tree.CanAddSelected))
            {
                Client.AddFiles(this.tree.GetSelectedPaths());
                Refresh();
            }

            bool showHierarchy = this.tree.ShowHierarchy;
            string hierarchyTooltip = "Click to display " + (showHierarchy ? "flattened" : "hierarchal") + " view";

            this.tree.ShowHierarchy = Button("UnityEditor.HierarchyWindow", hierarchyTooltip, isToggled: this.tree.ShowHierarchy);

            bool Button(string iconName, string tooltip, bool? enableCondition = null, bool? isToggled = null)
            {
                Rect buttonRect = EditorUtil.CutRect(ref rect, elementWidth);

                Texture icon = EditorUtil.LoadIcon(iconName + ".png");
                if(icon == null)
                    icon = EditorGUIUtility.IconContent(iconName).image;

                GUIContent content = new GUIContent(icon, tooltip);

                using(new EditorGUI.DisabledScope(enableCondition is false))
                {
                    return isToggled is null
                        ? GUI.Button(buttonRect, content, EditorStyles.toolbarButton)
                        : GUI.Toggle(buttonRect, isToggled.Value, content, EditorStyles.toolbarButton);
                }  
            }
        }

        private void DrawTree()
        {
            Rect searchRect = EditorGUILayout.GetControlRect(false, 24f);
            Rect treeRect = EditorUtil.GetFlexibleSpace();

            if(this.tree.FileCount == 0)
            {
                EditorGUI.LabelField(searchRect, "No files to commit!");
            }
            else
            {
                this.tree.searchString = this.search.OnGUI(searchRect, this.tree.searchString);
                this.tree.OnGUI(treeRect);
            }

            EditorGUILayout.Space(16f);
        }

        private void InitiateCommit()
        {
            GitCommitCommand command = new GitCommitCommand();
            ISet<GitFile> included = this.tree.GetIncludedFiles();
            
            // If we are amending a previous commit, and the message has not changed, omit the message option and add the --no-edit flag
            string message = CommitMessage;
            if(IsAmending && message == GitClientExtensions.GetLastCommitMessage(Client))
            {
                command.WithOption("--no-edit");
            }

            command.Amend(IsAmending).WithMessage(message);

            // Get confirmation from the user before the committing
            if(ConfirmCommit(command, included))
                Commit(command, included);
        }

        private void Refresh()
        {
            if(!IsInitialized) return;

            GitStatusResult status = Client.Execute(Git.Status.WithOption("--untracked-files"));

            this.branchName = status.BranchName ?? this.lastCommitHash;
            this.tree.Populate(status);
        }

        private void RestoreWithPrompt()
        {
            List<string> filesToDelete = new List<string>();
            foreach(GitFile file in this.tree.GetSelectedFiles())
            {
                if(file.StateInIndex == GitState.Added)
                    filesToDelete.Add(file.Path);
            }

            StringBuilder message = new StringBuilder("The following ")
                .Append(filesToDelete.Count)
                .Append(" file(s) will be deleted locally:\n")
                .Append(string.Join("\n", filesToDelete))
                .Append("\n\nAre you sure?");

            bool doRestore = true;
            if(filesToDelete.Count > 0)
                doRestore = EditorUtility.DisplayDialog("Confirm Restore", message.ToString(), "Yes, delete them", "Abort");

            if(doRestore)
            {
                Client.RestoreFiles(this.tree.GetSelectedPaths());
                AssetDatabase.Refresh();
                Refresh();
            }
        }
    }
}
