using System;
using System.Runtime.Serialization;

using Octothorpe.UGity.Editor.Util;

using UnityEngine;

namespace Octothorpe.UGity.Editor.UI
{
    public abstract class GitEditorUtilityWindow<TSelf> : GitEditorWindow where TSelf : GitEditorUtilityWindow<TSelf>
    {
        protected abstract Texture Icon { get; }

        protected virtual Type[] DockNextTo { get; }

        protected GitEditorClient Client => GitEditorClient.Instance;

        private static bool IsStaticallyInitialized { get; set; }

        private bool lostFocus;
        
        protected sealed override void OnDisable()
        {
            base.OnDisable();
            this.OnUninitialize();
        }

        protected sealed override void OnGUI()
        {
            if(!IsInitialized)
            {
                this.OnInitialize();
                IsInitialized = true;
                titleContent = new GUIContent(Title, Icon);
                this.OnPostInitialize();
            }

            if(!IsStaticallyInitialized)
            {
                this.OnFirstInitialize();
                IsStaticallyInitialized = true;
            }

            
            base.OnGUI();
        }

        protected void OnFocus()
        {
            if(!IsInitialized) return;
            // Ignore OnFocus calls that happen during widow creation
            if(!this.lostFocus) return;

            this.OnRefocus();
        }

        protected void OnLostFocus() => this.lostFocus = true;

        public static TSelf Open(bool centered = true)
        {
            TSelf window;
            Type[] dockNextTo = Static<TSelf>.Instance.DockNextTo;

            if(dockNextTo == null || dockNextTo.Length == 0)
                window = GetWindow<TSelf>();
            else
                window = GetWindow<TSelf>(dockNextTo);

            if(centered)
                EditorUtil.CenterWindow(window);

            window.OnOpen();
            return window;
        }

        public static void OpenModal<T>(bool centered = true) where T : Modal => CreateInstance<T>().ShowModalUtility();

        protected virtual void OnOpen() { }
        
        protected virtual void OnFirstInitialize() { }

        protected virtual void OnPostInitialize() { }
        
        protected virtual void OnUninitialize() { }

        protected virtual void OnRefocus() { }
    }

    /// <summary>
    /// Funky utility class used to fetch the values of read-only (non-backed) instance properties 
    /// from windows in a static context without triggering Unity messages >:)
    /// </summary>
    internal class Static<T> where T : GitEditorUtilityWindow<T>
    {
        public static T Instance
        {
            get {
                if(instance == null)
                    instance = (T) FormatterServices.GetUninitializedObject(typeof(T));

                return instance;
            }
        }

        private static T instance;
    }
}
