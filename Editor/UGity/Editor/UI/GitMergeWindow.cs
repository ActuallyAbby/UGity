using System.Collections.Generic;

using Octothorpe.UGity.Client;
using Octothorpe.UGity.Editor.Util;

using UnityEditor;

using UnityEngine;

namespace Octothorpe.UGity.Editor.UI
{
    public class GitMergeWindow : GitEditorUtilityWindow<GitMergeWindow>
    {
        protected override Texture Icon => GitEditorStyles.CompareIcon;
        protected override string Title { get; set; } = "Resolve Conflicts";
        protected override bool HasHeader { get; } = true;

        //protected override Type[] DockNextTo => new Type[] { typeof(GitCommitWindow) };

        private GUIContent headerLabel;
        private GUIContent acceptOursLabel;
        private GUIContent acceptTheirsLabel;
        private GUIContent openLabel;
        private Dictionary<GitFile, (GUIContent, GUIContent)> unmergedFiles;

        protected override void OnInitialize()
        {
            UpdateFiles();
        }

        protected override void OnRefocus()
        {
            UpdateFiles();
        }

        protected override void OnDrawHeader() => GUILayout.Label(this.headerLabel, GitEditorStyles.Label);

        protected override void OnDraw()
        {
            EditorGUI.indentLevel++;

            foreach(KeyValuePair<GitFile, (GUIContent, GUIContent)> pair in this.unmergedFiles)
            {
                GitFile file = pair.Key;
                (GUIContent fileLabel, GUIContent pathLabel) = pair.Value;

                using(new GUILayout.HorizontalScope())
                {
                    GUILayout.Label(fileLabel, GitEditorStyles.GetLabelStyle(file), GUILayout.Height(16f));
                    GUILayout.FlexibleSpace();

                    if(GUILayout.Button(this.acceptOursLabel))
                    {
                        Client.Execute(Git.Checkout.WithOption("--ours").WithPathspec(file.Path));
                    }

                    if(GUILayout.Button(this.acceptTheirsLabel))
                    {
                        Client.Execute(Git.Checkout.WithOption("--theirs").WithPathspec(file.Path));
                    }

                    if(GUILayout.Button(this.openLabel))
                    {
                        Client.OpenMergeTool(file.Path);
                    }
                }

                using(new GUILayout.HorizontalScope())
                {
                    GUILayout.Label(pathLabel, Color.gray.GetLabelStyle());
                    GUILayout.FlexibleSpace();

                    string oursState = (file.StateInIndex == GitState.Unmerged) ? "Unmodified" : file.StateInIndex.ToString();
                    string theirsState = (file.StateInTree == GitState.Unmerged) ? "Unmodified" : file.StateInTree.ToString();

                    GUILayout.Label("<b>Ours: </b>" + oursState, GitEditorStyles.GetStateColor(file.StateInIndex).GetLabelStyle());
                    GUILayout.Label("<b>Theirs: </b>" + theirsState, GitEditorStyles.GetStateColor(file.StateInTree).GetLabelStyle());
                }

                GUILayout.Space(EditorGUIUtility.singleLineHeight);
            }

            EditorGUI.indentLevel--;
        }

        private void UpdateFiles()
        {
            this.unmergedFiles = new Dictionary<GitFile, (GUIContent, GUIContent)>();

            foreach(GitFile file in Client.Execute(Git.Status))
            {
                string path = file.Path;
                int filenameIndex = file.Path.LastIndexOf('/') + 1;

                Texture icon = AssetDatabase.GetCachedIcon(path);
                GUIContent nameContent = new GUIContent(path.Substring(filenameIndex), icon);
                GUIContent pathContent = new GUIContent(path.Substring(0, filenameIndex));

                if(file.HasMergeConflict)
                {
                    this.unmergedFiles.Add(file, (nameContent, pathContent));
                }
            }

            this.headerLabel = new GUIContent($"There are <b>{this.unmergedFiles.Count}</b> file(s) with merge conflicts");
            this.acceptOursLabel = new GUIContent("Accept Ours");
            this.acceptTheirsLabel = new GUIContent("Accept Theirs");
            this.openLabel = new GUIContent("Open", "Open this file in the default merge tool");
        }
    }
}
