using UnityEditor;

using UnityEngine;

namespace Octothorpe.UGity.Editor.UI
{
    public abstract partial class GitEditorWindow : EditorWindow
    {
        protected abstract string Title { get; set; }
        protected abstract bool HasHeader { get; }

        protected bool IsInitialized { get; private protected set; }

        private Rect headerRect;
        
        protected abstract void OnInitialize();

        protected abstract void OnDraw();

        protected virtual void OnDrawHeader() { }

        protected virtual void OnDisable() => IsInitialized = false;

        protected virtual void OnGUI()
        {
            //GUI.skin = GitEditorStyles.Skin;

            if(HasHeader)
                DrawHeader();

            this.OnDraw();
        }

        private void DrawHeader()
        {
            GUILayout.Label(GUIContent.none, GitEditorStyles.Header);
            if(Event.current.type == EventType.Repaint)
                this.headerRect = GUILayoutUtility.GetLastRect();

            using(new GUILayout.AreaScope(this.headerRect))
            {
                GUILayout.FlexibleSpace();
                using(new GUILayout.HorizontalScope())
                {
                    this.OnDrawHeader();
                }

                GUILayout.FlexibleSpace();
            }
        }
    }
}
