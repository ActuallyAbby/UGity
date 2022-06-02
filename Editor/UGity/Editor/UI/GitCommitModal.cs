using System.Collections.Generic;

using Octothorpe.UGity.Client;

using UnityEditor;

using UnityEngine;

namespace Octothorpe.UGity.Editor.UI
{
    public class GitCommitModal : GitEditorWindow.Modal<GitCommitModal, (string Branch, GitCommitCommand Command, ISet<GitFile> Files), bool>
    {
        public bool Confirmed { get; private set; }

        protected override string Title { get; set; } = "Confirm Commit";
        protected override bool HasHeader { get; } = true;

        private SortedSet<GitFile> files;
        private Vector2 scrollPosition;

        private GUIContent headerContent;
        private GUIContent buttonContent;

        protected override void OnDrawHeader()
        {
            EditorGUILayout.LabelField(this.headerContent, GitEditorStyles.Header);
        }

        protected override void OnDraw()
        {
            using(var view = new EditorGUILayout.ScrollViewScope(this.scrollPosition, false, false))
            {
                foreach(GitFile file in this.files)
                {             
                    DrawItem(file);
                }

                this.scrollPosition = view.scrollPosition;
            }

            if(GUILayout.Button(this.buttonContent, GitEditorStyles.TerminalButton))
            {
                Output = true;
                this.Close();
            }
        }

        protected override void OnInitialize()
        {
            if(Input.Command.Amending)
                titleContent = new GUIContent("Confirm Amend");
            
            Input.Files = new SortedSet<GitFile>(Input.Files, new GitFileChangeTypeComparer());
            this.files = new SortedSet<GitFile>(Input.Files, new GitFileChangeTypeComparer());

            this.headerContent = new GUIContent(GetHeaderMessage(), GitEditorStyles.CommitIcon);
            this.buttonContent = new GUIContent(GitConsoleWindow.CARET + Input.Command.ToString());
            
            // Adjust the window size to fit the button text
            Rect newPosition = position;
            newPosition.width = GitEditorStyles.TerminalButton.CalcSize(this.buttonContent).x + 8;
            position = newPosition;
        }

        private static CommitChangeType GetChangeType(GitFile file)
        {
            if(file.StateInIndex == GitState.Modified || file.StateInTree == GitState.Modified)
                return CommitChangeType.Modified;
            else if(file.StateInIndex == GitState.Deleted)
                return CommitChangeType.Deleted;
            else if(file.StateInIndex == GitState.Added || file.IsUntracked)
                return CommitChangeType.Added;
            else
                return CommitChangeType.Unknown;
        }

        private static Texture2D GetChangeTypeIcon(CommitChangeType changeType)
        {
            switch(changeType)
            {
                case CommitChangeType.Conflict: return EditorGUIUtility.IconContent("Collab.FileConflict").image as Texture2D;
                case CommitChangeType.Added: return EditorGUIUtility.IconContent("Collab.FileAdded").image as Texture2D;
                case CommitChangeType.Modified: return EditorGUIUtility.IconContent("Collab.FileUpdated").image as Texture2D;
                case CommitChangeType.Deleted: return EditorGUIUtility.IconContent("Collab.FileDeleted").image as Texture2D;
                default: return EditorGUIUtility.IconContent("Collab.FileIgnored").image as Texture2D;
            }
        }

        private void DrawItem(GitFile file)
        {
            using(new EditorGUILayout.HorizontalScope())
            {
                Texture2D icon = GetChangeTypeIcon(GetChangeType(file));
                GUILayout.Label(new GUIContent(file.Path, icon), GitEditorStyles.GetSummaryLabelStyle(file));
            }
        }

        private string GetHeaderMessage()
        {
            int numFiles = this.files.Count;
            bool amend = Input.Command.Amending;

            return string.Format(
                "You are about to {0} {1} change(s) {2} <b>{3}</b>", 
                amend ? "amend" : "commit", 
                numFiles, 
                amend ? "to" : "on", 
                Input.Branch);
        }

        private enum CommitChangeType
        {
            Conflict = -1,
            Added = 0,
            Modified = 1,
            Deleted = 2,
            Unknown = int.MaxValue,
        }

        private class GitFileChangeTypeComparer : IComparer<GitFile>
        {
            public int Compare(GitFile first, GitFile second)
            {
                CommitChangeType firstState = GetChangeType(first);
                CommitChangeType secondState = GetChangeType(second);

                if(firstState == secondState)
                    return first.Path.CompareTo(second.Path);
                else
                    return firstState.CompareTo(secondState);
            }
        }
    }
}
