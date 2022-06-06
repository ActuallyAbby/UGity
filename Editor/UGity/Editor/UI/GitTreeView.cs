using System.Collections.Generic;

using Octothorpe.UGity.Client;

using UnityEditor;
using UnityEditor.IMGUI.Controls;

using UnityEngine;

using Object = UnityEngine.Object;

using static Octothorpe.UGity.Editor.Util.EditorUtil;
using System;
using Octothorpe.UGity.Editor.Persistence;

namespace Octothorpe.UGity.Editor.UI
{
    public class GitTreeView : TreeView
    {
        public int FileCount => this.idToItemMap.Count;

        public bool ShowHierarchy
        {
            get => this.commitState.ShowHierarchy;
            set {
                if(value == ShowHierarchy) return;
                this.commitState.ShowHierarchy = value;
                Repopulate();
            }
        }

        public bool CanAddSelected { get; private set; }
        public bool CanRestoreSelected { get; private set; }
        public bool CanRemoveSelected { get; private set; }
        public bool CanDiffSelected { get; private set; }

        private TreeViewItem root;
        private GitTreeItem trackedRoot;
        private GitTreeItem untrackedRoot;
        private GitCommitState commitState;

        private Dictionary<string, GitTreeItem> cache = new Dictionary<string, GitTreeItem>();
        private Dictionary<int, TreeViewItem> idToItemMap = new Dictionary<int, TreeViewItem>();

        public GitTreeView(GitCommitState commitState) : base(commitState.TreeState)
        {
            this.commitState = commitState;
            depthIndentWidth = 20f;
        }

        protected override TreeViewItem BuildRoot()
        {
            SetExpanded(1, true);
            SelectionChanged(GetSelection());

            return this.root;
        }

        protected override bool DoesItemMatchSearch(TreeViewItem treeItem, string search)
        {
            GitTreeItem item = treeItem as GitTreeItem;
            if(item == null || !item.IsFile) return false;

            string pathFormatted = item.FilePath.ToLowerInvariant().Replace('\\', '/');
            string searchFormatted = search.ToLowerInvariant().Replace('\\', '/');

            return pathFormatted.Contains(searchFormatted);
        }

        protected override void DoubleClickedItem(int id)
        {
            GitTreeItem item = FindItem(id, rootItem) as GitTreeItem;
            if(item == null)
                base.DoubleClickedItem(id);

            Object asset = AssetDatabase.LoadAssetAtPath<Object>(item.FilePath);
            EditorUtility.FocusProjectWindow();
            EditorGUIUtility.PingObject(asset);
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            Rect rect = args.rowRect;
            CenterRectUsingSingleLineHeight(ref rect);

            DrawRow(rect, args.item as GitTreeItem, args.row, args.isRenaming);
        }

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            CanAddSelected = false;
            CanRestoreSelected = true;
            CanRemoveSelected = false;
            CanDiffSelected = (selectedIds.Count > 0);

            if(selectedIds.Count == 0)
                return;

            foreach(int id in selectedIds)
            {
                if(FindItem(id) is GitTreeItem item)
                {
                    // If a file is untracked (or not modified), disable restore
                    if(item.File.IsUntracked || !GitStateFlags.Modified.HasFlag(item.File.StateFlags))
                        CanDiffSelected = false;

                    // If a file is untracked (and not modified in the index), disable restore
                    if(item.File.IsUntracked && item.File.StateInIndex == GitState.Unmodified)
                        CanRestoreSelected = false;

                    // If a file is untracked OR modified locally, enable add
                    if(item.File.IsUntracked || item.File.StateInTree != GitState.Unmodified)
                        CanAddSelected = true;

                    // If a file is tracked, enable remove
                    if(!item.File.IsUntracked)
                        CanRemoveSelected = true;

                    if(CanAddSelected && CanRemoveSelected) return;
                }
            } 
        }

        protected override void SingleClickedItem(int id)
        {
            if(id != -1) return;

            List<int> newSelection = new List<int>();

            if(Event.current.shift || Event.current.control)
            {
                foreach(int selectedId in GetSelection())
                {
                    if(selectedId == -1) continue;
                    newSelection.Add(selectedId);
                }
            }

            SetSelection(newSelection);
        }

        public TreeViewItem FindItem(int id)
        {
            this.idToItemMap.TryGetValue(id, out TreeViewItem item);
            return item;
        }

        public HashSet<GitFile> GetIncludedFiles()
        {
            HashSet<GitFile> files = new HashSet<GitFile>();
            foreach(string path in GetIncludedPaths())
            {
                if(this.cache.ContainsKey(path))
                {
                    files.Add(this.cache[path].File);
                }
            }

            return files;
        }

        public HashSet<string> GetIncludedPaths()
        {
            HashSet<string> paths = new HashSet<string>();
            foreach(string path in this.cache.Keys)
            {
                if(!this.commitState.NotIncluded.Contains(path))
                {
                    paths.Add(path);
                }
            }

            return paths;
        }

        public HashSet<GitFile> GetSelectedFiles()
        {
            HashSet<GitFile> paths = new HashSet<GitFile>();

            foreach(int index in GetSelection())
            {
                if(FindItem(index) is GitTreeItem item)
                {
                    paths.Add(item.File);

                    if(item.MetaFileItem != null)
                        paths.Add(item.MetaFileItem.File);
                }
            }

            return paths;
        }

        public HashSet<string> GetSelectedPaths()
        {
            HashSet<string> paths = new HashSet<string>();

            foreach(int index in GetSelection())
            {
                if(FindItem(index) is GitTreeItem item)
                {
                    paths.Add(item.FilePath);

                    if(item.MetaFileItem != null)
                        paths.Add(item.MetaFileItem.FilePath);
                }
            }

            return paths;
        }

        public void SetIncluded(int id, bool included) => SetIncludedInternal(FindItem(id, this.root) as GitTreeItem, included, true);

        public void SetIncluded(GitTreeItem item, bool included) => SetIncludedInternal(item, included, true);

        public void SetIncludedInternal(GitTreeItem item, bool included, bool includeChildren)
        {
            if(item == null) return;

            // Select the current item's state
            SetIncludedNoRecurse(item, included);

            // Traverse downwards, updating all children recursively
            if(includeChildren)
                SetIncludedDownward(item);

            // Traverse upwards, updating each parent
            GitTreeItem curParent = item.parent;
            while(curParent != null)
            {
                // When the child included == false, set the parent included = false
                // When the child included == true, set the parent included = true IF all of its children are selected
                SetIncludedNoRecurse(curParent, included ? IsImplicitlyIncluded(curParent) : false);
                curParent = curParent.parent;
            }      

            void SetIncludedDownward(GitTreeItem curItem)
            {
                if(curItem.children == null) return;
                foreach(GitTreeItem child in curItem.children)
                {
                    SetIncludedNoRecurse(child, included);
                    SetIncludedDownward(child);
                }
            }

            void SetIncludedNoRecurse(GitTreeItem target, bool value)
            {
                target.IsIncluded = value;

                if(!target.IsFile) return;

                if(value)
                {
                    this.commitState.NotIncluded.Remove(target.FilePath);
                    if(target.MetaFileItem != null)
                        this.commitState.NotIncluded.Remove(target.MetaFileItem.FilePath);
                }
                else
                {
                    this.commitState.NotIncluded.Add(target.FilePath);
                    if(target.MetaFileItem != null)
                        this.commitState.NotIncluded.Add(target.MetaFileItem?.FilePath);
                }   
            }
        }

        public void Populate(IEnumerable<GitFile> files)
        {
            if(this.trackedRoot == null)
                this.trackedRoot = new GitTreeItem(1, "Changes");

            if(this.untrackedRoot == null)
                this.untrackedRoot = new GitTreeItem(2, "Untracked Files");

            this.trackedRoot.children = null;
            this.untrackedRoot.children = null;

            this.root = new TreeViewItem(0, -1, "root");
            this.root.AddChild(this.trackedRoot);
            this.root.AddChild(new GitTreeItem());   

            this.idToItemMap.Clear();

            int nextId = 3;

            // Re-cache all items to remove ones that no longer exist
            Dictionary<string, GitTreeItem> newCache = new Dictionary<string, GitTreeItem>();
            Dictionary<string, GitFile> metaFiles = new Dictionary<string, GitFile>();

            foreach(GitFile file in files)
            {
                string path = file.Path;

                if(path.EndsWith(".meta"))
                {
                    string assetPath = path.Substring(0, path.Length - 5);
                    metaFiles.Add(assetPath, file);
                    continue;
                }

                bool isIncluded = !this.commitState.NotIncluded.Contains(path);

                GitTreeItem newItem = new GitTreeItem
                {
                    id = nextId++,
                    FilePath = path,
                    File = file,
                    IsIncluded = isIncluded,
                    IsFile = true,
                };

                this.cache[path] = newItem;
                newCache[path] = newItem;
                this.idToItemMap[newItem.id] = newItem;

                if(file.IsUntracked)
                    AddChild(this.untrackedRoot, newItem, ref nextId);
                else
                    AddChild(this.trackedRoot, newItem, ref nextId);
            }

            this.cache = newCache;
            
            foreach(KeyValuePair<string, GitFile> meta in metaFiles)
            {
                string assetFilePath = meta.Key;
                GitFile metaFile = meta.Value;

                if(!this.cache.TryGetValue(assetFilePath, out GitTreeItem assetItem)) continue;

                GitTreeItem metaItem = new GitTreeItem
                {
                    id = assetItem.id,
                    FilePath = metaFile.Path,
                    File = metaFile,
                    IsFile = true,
                    IsMetaFile = true,
                };

                assetItem.MetaFileItem = metaItem;
                this.cache[metaFile.Path] = metaItem;
                newCache[metaFile.Path] = metaItem;

                if(assetItem.IsIncluded)
                    this.commitState.NotIncluded.Remove(metaFile.Path);
            }

            if(this.untrackedRoot.children != null)
                this.root.AddChild(this.untrackedRoot);

            SetupDepthsFromParentsAndChildren(this.root);
            Reload();
        }

        protected void DrawRow(Rect rect, GitTreeItem item, int index, bool isRenaming)
        {
            if(item.displayName == null)
            {
                EditorGUI.DrawRect(rect, default);
                return;
            }

            GUIContent fileLabel = new GUIContent(item.displayName);
            // TODO: Initialize these at construction instead
            GUIContent warningLabel = CreateIconContent("console.warnicon", null,
                "This file has changes staged for commit but has been deleted locally. Use the rollback button to discard this file");

            EditorStyles.toggle.CalcMinMaxWidth(GUIContent.none, out float toggleWidth, out _);
            EditorStyles.label.CalcMinMaxWidth(fileLabel, out float fileWidth, out _);

            // If the item has an icon, use it for the label
            Texture icon = GitTreeItem.GetDefaultIcon(item);
            if(icon != null)
            {
                fileLabel = new GUIContent(fileLabel.text, icon);
                fileWidth += 16;
            }

            Rect originalRect = rect;

            CutRect(ref rect, GetContentIndent(item));
            Rect toggleRect = CutRect(ref rect, toggleWidth);
            Rect fileRect = CutRect(ref rect, fileWidth);
            Rect warningRect = CutRect(ref rect, 16f);

            // Draw the checkbox and save the value to the item
            using(var scope = new EditorGUI.ChangeCheckScope())
            {
                GUIStyle toggleStyle;
                if(!item.IsIncluded && item.HasSelectedChild)
                    toggleStyle = new GUIStyle("ToggleMixed");
                else
                    toggleStyle = new GUIStyle("Toggle");

                bool included = EditorGUI.Toggle(toggleRect, item.IsIncluded, toggleStyle);

                if(scope.changed)
                    SetIncluded(item, included);
            }
            
            // Draw the file/directory name
            EditorGUI.LabelField(fileRect, fileLabel, GitEditorStyles.GetLabelStyle(item.File));

            if(item.File.StateInTree == GitState.Deleted && item.File.StateInTree != GitState.Unmodified)
                EditorGUI.LabelField(warningRect, warningLabel);

            // In flattened view, draw the smallest fitting file path right-aligned to the window
            if(!ShowHierarchy)
                DrawLabelTruncated(rect, fileRect.x + fileRect.width, originalRect.width, item.Directory, Color.gray.GetLabelStyle(), '/', TruncateMode.Left);
        }

        private void AddChild(GitTreeItem root, GitTreeItem child, ref int startId)
        {
            if(!ShowHierarchy)
            {
                root.AddChild(child);
                // TODO: This can be optimized more if there are performane concerns later
                SetIncludedInternal(child, child.IsIncluded, false);
                return;
            }

            if(root.IsFile)
                throw new NotSupportedException("Can not add a hierarchal child to a file");

            bool makeRemainingDirs = false;
            GitTreeItem current = root;
            foreach(string dir in child.Directory.Split('/'))
            {
                if(string.IsNullOrEmpty(dir)) continue;

                bool found = false;

                if(!makeRemainingDirs && current.children != null)
                {
                    // Iterate over all children in the current element to find one with a name matching dir
                    foreach(TreeViewItem c in current.children)
                    {
                        // If the child is not a GitTreeItem, skip it
                        GitTreeItem gitChild = c as GitTreeItem;
                        if(gitChild == null) continue;

                        // If the child is a directory and has a matching name, set it as the current item and stop searching
                        if(gitChild.IsDirectory && gitChild.displayName == dir)
                        {
                            current = gitChild;
                            found = true;
                            break;
                        }
                    }
                }

                // The current dir does not have an item under this root, create one
                if(!found)
                {
                    GitTreeItem dirItem = new GitTreeItem
                    {
                        id = startId++,
                        displayName = dir,
                        IsDirectory = true,
                    };

                    current.AddChild(dirItem);
                    current = dirItem;
                    makeRemainingDirs = true;
                }
            }

            current.AddChild(child);
            SetIncludedInternal(child, child.IsIncluded, false);
        }

        private bool IsImplicitlyIncluded(GitTreeItem target)
        {
            if(target.IsIncluded) return true;
            if(target.children == null) return false;
            foreach(GitTreeItem child in target.children)
            {
                if(!child.IsIncluded) return false;
            }

            return (target.IsIncluded = true);
        }

        private void Repopulate()
        {
            List<GitFile> files = new List<GitFile>();
            foreach(GitTreeItem item in this.cache.Values)
            {
                files.Add(item.File);
            }

            Populate(files);
        }
    }
}
