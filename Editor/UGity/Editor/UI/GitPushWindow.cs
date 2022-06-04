using System.Collections.Generic;

using Octothorpe.UGity.Client;
using Octothorpe.UGity.Editor.Util;
using Octothorpe.UGity.Util;

using UnityEditor;

using UnityEngine;

namespace Octothorpe.UGity.Editor.UI
{
    public class GitPushWindow : GitEditorUtilityWindow<GitPushWindow>
    {
        protected override string Title { get; set; } = "Push Changes";
        protected override Texture Icon => GitEditorStyles.PushWindowIcon;
        protected override bool HasHeader { get; } = true;

        private Vector2 scroll;
        
        private int SelectedRemote
        {
            get => this.selectedRemote;
            set {
                if(value != SelectedRemote)
                    SetRemote(value);
            }
        }

        private bool PushTags
        {
            get => this.pushTags;
            set {
                if(value != PushTags)
                    SetPushTags(value);
            }
        }
        
        private bool SetUpstream
        {
            get => this.setUpstream;
            set {
                if(value != this.setUpstream)
                    SetUpstreamOnPush(value);
            }
        }

        private string RemoteBranch
        {
            get => this.remoteBranch;
            set {
                if(value != this.remoteBranch)
                    SetBranch(value);
            }
        }

        private string Destination => string.Concat(this.remotes[SelectedRemote], "/", this.remoteBranch);

        private static GUIStyle LabelNoMargin
        {
            get {
                if(labelNoMargin == null)
                {
                    labelNoMargin = new GUIStyle(GUI.skin.label);
                    labelNoMargin.margin.left = 0;
                    labelNoMargin.margin.right = 0;
                    labelNoMargin.richText = true;
                }
                
                return labelNoMargin;
            }
        }

        private static GUIStyle labelNoMargin;

        private string[] remotes;
        private int selectedRemote;
        
        private string localBranch;
        private string upstream;
        private string remoteBranch;
        private bool setUpstream;
        private bool pushTags;
        private bool remoteBranchExists;
        private IReadOnlyCollection<CommitInfo> commits;

        private GenericMenu remotesMenu;

        private GUIContent[] remoteLabels;
        private GUIContent headerContent;
        private GUIContent upstreamDelimiter;
        private GUIContent newIcon;
        private GUIContent pushTagsLabel;
        private GUIContent setUpstreamLabel;
        private GUIContent buttonContent;
        private GUIContent subHeaderContent;
        private GUIContent commitIcon;
        private float remotesFieldWidth;

        private GitPushCommand command;

        protected override void OnOpen() => minSize = new Vector2(600f, 250f);

        protected override void OnInitialize()
        {
            this.commits = new List<CommitInfo>(GetUnpushedCommits());
            if(this.commits.Count == 0)
            {
                this.Close();
                EditorUtility.DisplayDialog("No Changes", "There are no changes to push.", "Close");
            }

            this.remotes = Client.GetRemotes();
            this.localBranch = Client.GetBranchName();
            this.remoteBranch = GetUpstreamBranch(out this.selectedRemote) ?? this.localBranch;
        
            this.command = Git.Push;

            SetRemote(this.selectedRemote);
            SetBranch(this.remoteBranch);
            SetUpstreamOnPush(!this.remoteBranchExists);

            this.remoteLabels = EditorUtil.CreateContent(this.remotes);
            this.headerContent = new GUIContent(string.Format("<b>{0}</b> →", this.localBranch));
            this.upstreamDelimiter = new GUIContent("/");
            this.newIcon = EditorUtil.CreateIconContent("PackageBadgeNew");
            this.pushTagsLabel = new GUIContent("Push tags");
            this.commitIcon = new GUIContent(GitEditorStyles.CommitIcon);
            this.remotesFieldWidth = EditorUtil.GetMaximumWidth(this.remoteLabels, EditorStyles.popup);
            UpdateDynamicContent();
        }
        
        protected override void OnDrawHeader()
        {
            EditorGUIUtility.fieldWidth = 0;
            
            GUILayout.Space(4f);
            GUILayout.Label(this.headerContent, LabelNoMargin);
            
            if(this.remotes.Length > 1)
                SelectedRemote = EditorGUILayout.Popup(SelectedRemote, this.remoteLabels, GUILayout.Width(this.remotesFieldWidth));
            else
                GUILayout.Label(this.remoteLabels[0], LabelNoMargin);
            
            GUILayout.Label(this.upstreamDelimiter, LabelNoMargin);
            
            RemoteBranch = EditorGUILayout.DelayedTextField(RemoteBranch, GUILayout.Width(120f));

            // If the remote branch does not exist, draw a 'new' icon next to the branch name
            if(!this.remoteBranchExists)
                GUILayout.Label(this.newIcon, LabelNoMargin);
            
            GUILayout.FlexibleSpace();
        }
                    
        protected override void OnDraw()
        {
            GUILayout.Label(this.subHeaderContent, GitEditorStyles.Label);

            EditorGUI.indentLevel++;

            using(var scope = new EditorGUILayout.ScrollViewScope(this.scroll))
            {
                foreach(CommitInfo commit in this.commits)
                {
                    using(new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField(this.commitIcon, GUILayout.Width(32f), GUILayout.Height(14f));
                        GUILayout.Label(commit.ShortHash, GitEditorStyles.MonospacedLabel);
                        GUILayout.Label(commit.Subject, EditorStyles.boldLabel);

                        GUILayout.FlexibleSpace();
                    }
                }

                this.scroll = scope.scrollPosition;
            }

            EditorGUI.indentLevel--;
            
            GUILayout.FlexibleSpace();

            PushTags = EditorGUILayout.ToggleLeft(this.pushTagsLabel, PushTags, GitEditorStyles.Label);
            
            using(new EditorGUI.DisabledScope(this.upstream == Destination))
                SetUpstream = EditorGUILayout.ToggleLeft(this.setUpstreamLabel, SetUpstream, GitEditorStyles.Label);

            using(new EditorGUI.DisabledScope(this.remoteBranch == null))
            {
                if(GUILayout.Button(this.buttonContent, GitEditorStyles.TerminalButton))
                {
                    this.Close();
                    StartPush();
                }
            }
        }

        // Thanks to https://stackoverflow.com/a/3338774 for this one :)
        private IEnumerable<CommitInfo> GetUnpushedCommits() => Client.Execute(Git.Log.WithOption("--branches").WithOption("--not").WithOption("--remotes"));
        
        private string GetUpstreamBranch(out int remoteIndex)
        {
            this.upstream = Client.GetUpstream()?.Trim();
            
            // If an upstream branch is set, find and set the remote
            if(this.upstream != null)
            {
                string[] split = this.upstream.Split('/');

                // Find the index of the remote with a matching name
                for(int i = 0; i < this.remotes.Length; i++)
                {
                    if(this.remotes[i] == split[0])
                    {
                        remoteIndex = i;
                        return split[1];
                    }
                }
            }

            remoteIndex = 0;
            return null;
        }

        private bool RemoteHasBranch(int remoteIndex, string branch)
        {
            string target = string.Concat(this.remotes[remoteIndex], "/", branch);
            
            foreach(string remoteBranch in Client.Execute(Git.Branch.WithOption("-r")))
            {
                if(remoteBranch.Trim().Equals(target))
                    return true;
            }

            return false;
        }
        
        private async void StartPush()
        {
            string taskName = $"Pushing {this.localBranch} → {Destination}";
            
            using(var task = new GitProgressReporter(Client, GitProgressReporter.Task.Push, taskName))
            {
                try
                {
                    await Client.ExecuteAsync(this.command.WithOption("--progress"), token: task.Token);
                }
                catch(GitClientException e)
                {
                    EditorUtility.DisplayDialog("Push Failed", e.Message, "Close");
                }
                finally
                {
                    task.Complete();
                }
            }
        }

        private void SetRemote(int index)
        {
            this.selectedRemote = index;
            this.remoteBranchExists = RemoteHasBranch(index, this.remoteBranch);
            this.command.WithRemote(this.remotes[index]);
            
            if(this.remotes.Length > 1)
            {
                this.remotesMenu = new GenericMenu();

                for(int i = 0; i < this.remotes.Length; i++)
                {
                    int currentIndex = i;

                    if(i == this.selectedRemote)
                        this.remotesMenu.AddDisabledItem(new GUIContent(this.remotes[i]));
                    else
                        this.remotesMenu.AddItem(new GUIContent(this.remotes[i]), false, () => SetRemote(currentIndex));

                    if(i == 0)
                        this.remotesMenu.AddSeparator("");
                }
            }

            if(IsInitialized)
                UpdateDynamicContent();
        }
        
        private void SetBranch(string name)
        {
            if(string.IsNullOrWhiteSpace(name)) return;
            
            try
            {
                string normalizedName = Client.Execute(new GitCommand("check-ref-format")
                    .WithOption("--branch")
                    .WithArgument(name, true)).Output;
                
                this.remoteBranch = normalizedName;
                this.remoteBranchExists = RemoteHasBranch(this.selectedRemote, this.remoteBranch);
                this.command.SetRefspec(this.localBranch, this.remoteBranch);

                if(IsInitialized)
                    UpdateDynamicContent();
            }
            catch(GitFatalErrorException e)
            {
                EditorUtility.DisplayDialog("Invalid Name", e.Message, "Ok");
            }
        }

        private void SetPushTags(bool pushTags)
        {
            this.pushTags = pushTags;

            if(pushTags)
                this.command.WithOption("--tags");
            else
                this.command.SetOption("--tags", null);

            if(IsInitialized)
                UpdateDynamicContent();
        }
        
        private void SetUpstreamOnPush(bool setUpstream)
        {
            this.setUpstream = setUpstream;

            if(setUpstream)
                this.command.WithOption("-u");
            else
                this.command.SetOption("-u", null);

            if(IsInitialized)
                UpdateDynamicContent();
        }
        
        private void UpdateDynamicContent()
        {
            string text = string.Format(
                "You are about to push <b>{0}</b> commit(s) on {1}remote branch <b>{2}</b>:",
                this.commits.Count,
                this.remoteBranchExists ? "" : "<color=#4fbb48>new</color> ",
                this.remoteBranch);

            this.subHeaderContent = new GUIContent(text);
            this.buttonContent = new GUIContent(GitConsoleWindow.CARET + this.command.ToString());

            string setUpstreamText = string.Format("Set branch <b>{0}</b> to track remote branch <b>{1}</b>", this.localBranch, Destination);
            this.setUpstreamLabel = new GUIContent(setUpstreamText);
        }
    }
}
