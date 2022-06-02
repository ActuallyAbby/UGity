using System.Collections.Generic;

using Octothorpe.UGity.Client;
using Octothorpe.UGity.Util;

using UnityEditor;

using UnityEngine;

namespace Octothorpe.UGity.Editor.UI
{
    public class GitPullWindow : GitEditorUtilityWindow<GitPullWindow>
    {
        protected override string Title { get; set; }
        protected override Texture Icon => GitEditorStyles.PullWindowIcon;
        protected override bool HasHeader { get; } = true;

        private bool ShowAdvanced
        {
            get => this.showAdvanced;
            set {
                if(value == this.showAdvanced) return;
                this.showAdvanced = value;

                UpdateSources();
                UpdateCommand();
            }
        }

        private string DefaultSource
        {
            get {
                if(ShowAdvanced) 
                    return string.Concat(this.upstreamRemote, "/", this.upstreamBranch);
                else
                    return this.upstreamRemote;
            }
        }

        // The current local branch
        private string localBranch;
        
        // The default remote and branch to merge into when not specified
        private string upstreamRemote;
        private string upstreamBranch;

        private List<string> pullSources;
        private int selectedSource;
        
        private bool showAdvanced;

        private GenericMenu popupMenu;
        private GUIContent popupPrefixLabel;
        private GUIContent noRemotesLabel;
        private GUIContent[] popupLabels;

        private Dictionary<string, GitPullOptionData> options;
        private GitPullCommand command;
        
        protected override void OnInitialize()
        {
            this.localBranch = Client.GetBranchName();
            Title = "Pull on " + this.localBranch;

            string upstream = Client.GetUpstream();
            if(upstream != null)
            {
                string[] split = upstream.Split('/');
                this.upstreamRemote = split[0];
                this.upstreamBranch = split[1];
            }
            //this.branchDefaultRemote = Client.GetConfigOption(string.Concat("branch.", this.localBranch, ".remote"));
            //this.branchDefaultMerge = Client.GetConfigOption(string.Concat("branch.", this.localBranch, ".merge"));
            
            this.popupPrefixLabel = new GUIContent("Pull From");
            this.noRemotesLabel = new GUIContent("(no remotes defined)");
            
            UpdateSources();
            PopulateOptions();

        }
        
        protected override void OnDrawHeader()
        {
            GUILayout.Space(8f);
            
            // Draw the popup menu label
            EditorGUILayout.PrefixLabel(this.popupPrefixLabel);
            
            // Get a rect for the popup
            Rect rect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight, EditorStyles.popup);

            bool isRemoteAvailable = (this.pullSources.Count > 1);

            using(new EditorGUI.DisabledScope(!isRemoteAvailable))
            {
                GUIContent label = isRemoteAvailable ? this.popupLabels[this.selectedSource] : this.noRemotesLabel;

                // Rather than drawing a standard popup, we'll draw a button that opens a GenericMenu when clicked
                if(GUI.Button(rect, label, EditorStyles.popup))
                {
                    this.popupMenu.DropDown(rect);
                }
            }

            // Draw the 'Advanced' toggle
            ShowAdvanced = EditorGUILayout.ToggleLeft("Advanced", ShowAdvanced);
            
            GUILayout.Space(8f);
        }

        protected override void OnDraw()
        {
            GUILayout.FlexibleSpace();
            
            foreach(KeyValuePair<string, GitPullOptionData> option in this.options)
            {
                GitPullOptionData data = option.Value;
                if(data.IsDefault) continue;
                
                using(var scope = new EditorGUI.ChangeCheckScope())
                {
                    using(new EditorGUI.DisabledScope(!data.IsEnabled))
                        option.Value.IsSelected = EditorGUILayout.ToggleLeft(new GUIContent(option.Key, option.Value.Description), option.Value.IsSelected);

                    if(scope.changed)
                        UpdateOptions();
                }
            }

            bool isRemoteAvailable = (this.pullSources.Count > 1);
            
            if(!isRemoteAvailable)
                EditorGUILayout.HelpBox("No remotes defined", MessageType.Error, true);

            using(new EditorGUI.DisabledScope(!isRemoteAvailable))
                if(GUILayout.Button(new GUIContent(GitConsoleWindow.CARET + this.command.ToString()), GitEditorStyles.TerminalButton))
                    this.Close();
        }
        
        private bool GetBoolOption(string name)
        {
            string result = Client.GetConfigOption(name);
            return result == null ? false : bool.Parse(result);
        }
        
        private void UpdateSources()
        {
            // If the 'Advanced' toggle is on, populate sources using all remote branches
            if(ShowAdvanced)
                this.pullSources = new List<string>(Client.GetRemoteBranches());
            // Otherwise, show remotes only
            else
                this.pullSources = new List<string>(Client.GetRemotes());

            this.pullSources.Insert(0, "--all");

            UpdateMenu(true);
        }

        private void UpdateMenu(bool setDefault)
        {
            this.popupMenu = new GenericMenu();

            // If setDefault is true, attempt to set the selected source to the default
            string defaultSource = DefaultSource;
            if(setDefault)
                this.selectedSource = this.pullSources.IndexOf(defaultSource, 1);

            // If the index is out of bounds, set it to 1 if there are at least two sources, otherwise 0
            if(this.selectedSource < 0 || this.selectedSource >= this.pullSources.Count)
                this.selectedSource = (this.pullSources.Count > 1) ? 1 : 0;

            // Allocate an array for the popup menu labels
            this.popupLabels = new GUIContent[this.pullSources.Count];
            
            for(int i = 0; i < this.pullSources.Count; i++)
            {
                int newIndex = i;
                
                // Effective index, offset to account for the additional element at index 0
                bool isDefault = (this.pullSources[i] == defaultSource);

                GUIContent itemLabel = new GUIContent(this.pullSources[i] + (isDefault ? " (default)" : ""));
                this.popupMenu.AddItem(itemLabel, (i == this.selectedSource), () => OnMenuClick(newIndex));
                this.popupLabels[i] = itemLabel;

                // Add a separator after the first item
                if(i == 0)
                    this.popupMenu.AddSeparator("");
            }

            void OnMenuClick(int index)
            {
                this.selectedSource = index;
                UpdateCommand();
                UpdateMenu(false);
            }
        }
        
        private void PopulateOptions()
        {
            bool defaultRebase = GetBoolOption("pull.rebase");
            bool defaultStash = defaultRebase ? GetBoolOption("rebase.autoStash") : GetBoolOption("merge.autoStash");
            
            this.options = new Dictionary<string, GitPullOptionData>()
            {
                ["--rebase"] = new GitPullOptionData("Rebase the current branch on top of the upstream branch after fetching.", "--rebase=false", defaultRebase, defaultRebase, "--ff-only", "--no-ff", "--squash", "--no-commit"),
                ["--rebase=false"] = new GitPullOptionData("Merge the upstream branch into the current branch. This option only appears when your config's 'pull.rebase' option is set to true", "--rebase", !defaultRebase, !defaultRebase),
                ["--autostash"] = new GitPullOptionData("Automatically create a temporary stash entry before the operation begins, and apply it after the operation ends.", "--no-autostash", defaultStash, defaultStash),
                ["--no-autostash"] = new GitPullOptionData("Overrides --autostash.", "--autostash", !defaultStash, !defaultStash),
                ["--ff-only"] = new GitPullOptionData("Only update to the new history if there is no divergent local history.", null, false, false, "--rebase", "--no-ff", "--squash"),
                ["--no-ff"] = new GitPullOptionData("When merging rather than rebasing, specifies how a merge is handled when the merged-in history is already a descendant of the current history.", null, false, false, "--rebase", "--ff-only", "--squash"),
                ["--squash"] = new GitPullOptionData("Produce the working tree and index state as if a real merge happened (except for the merge information), but do not actually make a commit.", "--no-squash", false, false, "--rebase", "--commit", "--ff-only", "--no-ff"),
                ["--no-squash"] = new GitPullOptionData("Overrides --squash.", "--squash", false, true),
                //["--commit"] = new GitPullOptionData("Perform the merge and commit the result. Overrides --no-commit.", "--no-commit", false, true, "--no-commit"),
                ["--no-commit"] = new GitPullOptionData("Perform the merge and stop just before creating a merge commit, to give the user a chance to inspect and further tweak the merge result before committing.", "--commit", false, false, "--rebase"),
                ["--no-verify"] = new GitPullOptionData("By default, the pre-merge and commit-msg hooks are run. When --no-verify is given, these are bypassed.", null, false, false),
            };

            UpdateOptions();
        }

        private void ConfirmPull()
        {
            this.Close();
            
            Client.OnOutputLine += OnOutput;
            Client.OnErrorLine += OnError;

            try
            {
                Client.Execute(this.command.WithOption("--porcelain"), timeout: 20000);
            }
            catch(GitFatalErrorException e)
            {
                EditorUtility.DisplayDialog("Push Failed", e.Message, "Ok");
            }

            void OnOutput(string line)
            {
                Debug.Log(line);
            }

            void OnError(string line)
            {
                Debug.LogWarning(line);
            }
        }

        private void UpdateOptions()
        {
            foreach(GitPullOptionData option in this.options.Values)
            {
                option.IsEnabled = true;
            }
            
            // Set any negated options' values to the opposite of their counterpart
            foreach(KeyValuePair<string, GitPullOptionData> option in this.options)
            {
                GitPullOptionData data = option.Value;

                if(!data.IsDefault && data.NegatedOption != null && this.options.ContainsKey(data.NegatedOption))
                {
                    this.options[data.NegatedOption].IsSelected = !data.IsSelected;
                }
            }
            
            foreach(KeyValuePair<string, GitPullOptionData> option in this.options)
            {
                GitPullOptionData data = option.Value;
                
                if(data.IsSelected)
                {   
                    foreach(string incompatible in data.IncompatibleWith)
                    {
                        if(!this.options.ContainsKey(incompatible)) continue;
                        
                        this.options[incompatible].IsSelected = false;
                        this.options[incompatible].IsEnabled = false;

                        if(data.IsDefault) continue;
                        
                        // If the incompatible option has a negated option, set that option as disabled
                        string negated = this.options[incompatible].NegatedOption;
                        if(negated == null) continue;
                        if(!this.options.ContainsKey(negated)) continue;

                        this.options[negated].IsEnabled = false;
                    }
                }
            }
            
            UpdateCommand();
        }

        private void UpdateCommand()
        {
            this.command = new GitPullCommand();
            
            // If the selected index is 1, set the --all flag instead a remote
            if(this.selectedSource == 0)
            {
                this.command.WithOption("--all");
            }
            else if(this.pullSources.Count > 0)
            {
                string[] split = this.pullSources[this.selectedSource].Split('/');
                string remote = split[0];
                
                // If showing advanced settings, OR if the selected remote is not the default remote, explicitly set the remote
                if(ShowAdvanced || remote != this.upstreamRemote)
                    this.command.WithRemote(split[0]);

                // If showing advanced settings, explicitly set the branch
                if(ShowAdvanced)
                    this.command.WithRefspec(this.localBranch, split[1]);
            }

            foreach(KeyValuePair<string, GitPullOptionData> option in this.options)
            {
                if(option.Value.IsSelected && !option.Value.IsDefault)
                    this.command.WithOption(option.Key);
            }
        }

        private class GitPullOptionData
        {
            public string Description { get; }
            public string NegatedOption { get; }
            public bool IsSelected { get; set; }
            public bool IsDefault { get; set; }
            public bool IsEnabled { get; set; }
            public ISet<string> IncompatibleWith { get; }

            public GitPullOptionData(string description, string negatedOption, bool isSelected, bool isHidden, params string[] incompatibleWith)
            {
                Description = description;
                NegatedOption = negatedOption;
                IsSelected = isSelected;
                IsDefault = isHidden;
                IsEnabled = true;
                IncompatibleWith = new HashSet<string>(incompatibleWith);
            }
        }
    }
}
