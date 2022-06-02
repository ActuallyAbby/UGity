using System.Collections.Generic;
using System;

using UnityEditor.IMGUI.Controls;

using UnityEngine;

namespace Octothorpe.UGity.Editor.Persistence
{
    [Serializable]
    public class GitCommitState : ScriptableObject, ISerializationCallbackReceiver
    {
        public bool ShowHierarchy
        {
            get => this.showHierarchy;
            set => this.showHierarchy = value;
        }

        public HashSet<string> NotIncluded => this.filesNotIncludedSet;

        public string Message
        {
            get => this.message;
            set => this.message = value;
        }
        public bool Amend
        {
            get => this.amending;
            set => this.amending = value;
        }

        public TreeViewState TreeState => this.baseState;

        [SerializeField]
        private bool showHierarchy;
        [SerializeField]
        private List<string> filesNotIncluded = new List<string>();
        [SerializeField]
        private string message;
        [SerializeField]
        private bool amending;
        [SerializeField]
        private TreeViewState baseState = new TreeViewState();

        [NonSerialized]
        private HashSet<string> filesNotIncludedSet = new HashSet<string>();

        public void OnBeforeSerialize() => this.filesNotIncluded = new List<string>(this.filesNotIncludedSet);

        public void OnAfterDeserialize() => this.filesNotIncludedSet = new HashSet<string>(this.filesNotIncluded);
    }
}
