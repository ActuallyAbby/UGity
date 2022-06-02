using System;
using System.Collections.Generic;

using Octothorpe.UGity.Editor.Util;

using UnityEditor;

using UnityEngine;

using Object = UnityEngine.Object;

namespace Octothorpe.UGity.Editor.Persistence
{
    public class GitEditorState : ScriptableObject, ISerializationCallbackReceiver
    {
        public const string STATE_FILE = EditorUtil.PACKAGE_DIR + "state.asset";
        private const HideFlags HIDE_FLAGS = HideFlags.HideInHierarchy;

        public bool AlwaysAdd
        {
            get => this.alwaysAddNewAssets;
            set => this.alwaysAddNewAssets = value;
        }

        public HashSet<string> UntrackedFiles => this.untrackedFilesSet;

        [SerializeField]
        private bool alwaysAddNewAssets;
        [SerializeField]
        private List<string> untrackedFiles = new List<string>();

        [NonSerialized]
        private HashSet<string> untrackedFilesSet = new HashSet<string>();

        public static GitEditorState Load()
        {
            GitEditorState state = LoadHiddenAssetAtPath<GitEditorState>(STATE_FILE);
            if(state == null)
            {
                AssetDatabase.CreateAsset(CreateInstance<GitEditorState>(), STATE_FILE);
                AssetDatabase.SaveAssets();

                state = AssetDatabase.LoadAssetAtPath<GitEditorState>(STATE_FILE);
                state.hideFlags = HIDE_FLAGS;
            }

            return state;
        }

        public T LoadSubState<T>() where T : ScriptableObject
        {
            string path = AssetDatabase.GetAssetPath(this);

            T subState = LoadHiddenAssetAtPath<T>(path);
            if(subState != null) return subState;

            subState = CreateInstance<T>();
            subState.name = typeof(T).Name;
            subState.hideFlags = HIDE_FLAGS;

            AssetDatabase.AddObjectToAsset(subState, this);
            AssetDatabase.SaveAssets();

            return subState;
        }

        public void OnBeforeSerialize() => this.untrackedFiles = new List<string>(this.untrackedFilesSet);

        public void OnAfterDeserialize() => this.untrackedFilesSet = new HashSet<string>(this.untrackedFiles);

        private static T LoadHiddenAssetAtPath<T>(string assetPath) where T : Object
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            foreach(Object asset in assets)
            {
                if(asset is T found) return found;
            }

            return null;
        }
    }
}
