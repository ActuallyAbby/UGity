using Octothorpe.UGity.Client;

using UnityEditor;
using UnityEditor.IMGUI.Controls;

using UnityEngine;

namespace Octothorpe.UGity.Editor.UI
{
    [System.Serializable]
    public class GitTreeItem : TreeViewItem
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Hiding a Unity property that does not adhere to naming conventions")]
        public new GitTreeItem parent => base.parent as GitTreeItem;

        public GitTreeItem MetaFileItem
        {
            get => this.metaFileItem;
            set {
                this.metaFileItem = value;
                this.metaFileItem.isIncluded = this.isIncluded;
            }
        }
        public GitFile File { get; set; }
        public string FilePath
        {
            get => this.path;
            set {
                int fileIndex = value.LastIndexOf('/') + 1;

                this.path = value;
                FileName = value.Substring(fileIndex);
                Directory = value.Substring(0, fileIndex);

                if(displayName == null)
                    displayName = FileName;
            }
        }
        public string FileName { get; private set; }
        public string Directory { get; private set; }
        public bool IsIncluded
        {
            get => this.isIncluded;
            set {
                if(IsMetaFile) return;
                this.isIncluded = value;
                
                if(MetaFileItem != null)
                    MetaFileItem.isIncluded = value;
            }
        }
        public bool IsFile { get; set; }
        public bool IsDirectory { get; set; }
        public bool IsMetaFile { get; set; }

        public bool HasSelectedChild
        {
            get {
                if(children == null) return false;

                foreach(GitTreeItem child in children)
                {
                    if(child.IsIncluded) return true;
                }

                return false;
            }
        }

        [SerializeField]
        private bool isIncluded;

        private GitTreeItem metaFileItem;
        private string path;

        public GitTreeItem() : this(-1, null) { }

        public GitTreeItem(int id, string displayName) : base(id, 0, displayName) { }

        public static Texture GetDefaultIcon(GitTreeItem item)
        {
            if(item.IsDirectory)
                return EditorGUIUtility.IconContent("Folder Icon").image as Texture2D;
            else
                return AssetDatabase.GetCachedIcon(item.FilePath) as Texture2D;
        }
    }
}
