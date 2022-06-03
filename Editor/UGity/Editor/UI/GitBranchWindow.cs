using Octothorpe.UGity.Util;

using UnityEditor;

using UnityEngine;

namespace Octothorpe.UGity.Editor.UI
{
    public partial class GitBranchWindow : GitEditorUtilityWindow<GitBranchWindow>
    {
        private const float OVERLAY_WIDTH = 120f;
        private const float OVERLAY_HEIGHT = 18f;
        
        private const float OVERLAY_X_OFFSET = 300f;
        private const float OVERLAY_Y_OFFSET = 2f;

        private float HideAtHeight => this.lastRect.yMin - 20f;

        protected override Texture Icon { get; }
        protected override string Title { get; set; } = "UGity - This should never be visible";
        protected override bool HasHeader { get; } = false;

        private static GitBranchWindow window;

        private bool showWindow;
        
        private GenericMenu popup;
        private GUIContent buttonContent;
        private Rect lastRect;

        private Vector2 overlaySize;
        private Vector2 overlayOffset;

        private bool isDirty;

        [InitializeOnLoadMethod]
        public static new void Show()
        {
            window = CreateInstance<GitBranchWindow>();
            window.position = new Rect(0, 0, OVERLAY_WIDTH, OVERLAY_HEIGHT);
            window.overlayOffset = new Vector2(OVERLAY_X_OFFSET, OVERLAY_Y_OFFSET);
            window.overlaySize = new Vector2(OVERLAY_WIDTH, OVERLAY_HEIGHT);
            window.maxSize = window.overlaySize;
            window.titleContent = new GUIContent(window.Title);
            window.ShowPopup();

            EditorApplication.update -= KeepWindowAlive;
            EditorApplication.update += KeepWindowAlive;
        }
        
        protected override void OnInitialize() => this.isDirty = true;

        protected override void OnDraw()
        {
            if(this.isDirty)
            {
                RefreshMenu();
                this.isDirty = false;
            }
            
            if(Event.current.type == EventType.Layout)
            {
                // Position of the cursor in screen space
                Vector2 mousePos = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);

                // If the mouse is not over an EditorWindow, and is above HideAtHeight, mark the window as hidden
                if(mouseOverWindow == null && mousePos.y < HideAtHeight)
                {
                    this.showWindow = false;
                    position = Rect.zero;
                }
                // Otherwise, the window will be shown
                else
                {
                    this.showWindow = true;
                }

                if(this.showWindow)
                {
                    Rect editorRect = EditorGUIUtility.GetMainWindowPosition();

                    // Position at the bottom-right corner of the editor window
                    Vector2 cornerPos = editorRect.position + editorRect.size;
                    // Position where the overlay will be placed
                    Vector2 overlayPos = cornerPos - this.overlayOffset - this.overlaySize;

                    position = new Rect(overlayPos, this.overlaySize);
                    this.lastRect = position;
                }
            }
            
            using(new GUILayout.HorizontalScope())
            {
                if(GUILayout.Button(this.buttonContent, GitEditorStyles.StatusBarButton, GUILayout.Width(position.width)))
                {
                    this.popup.DropDown(GUILayoutUtility.GetLastRect());
                }
            }
        }

        public void OnInspectorUpdate()
        {
            if(this != window)
            {
                this.Close();
                DestroyImmediate(this);
            }
            else
            {
                Repaint();                
            }
        }

        private static void KeepWindowAlive()
        {
            if(window == null)
                Show();
        }

        private void RefreshMenu()
        {
            string head = Client.GetBranchName();
            bool isDetachedHead = (head == null);
            head ??= Client.GetHeadHash();
            
            this.buttonContent = new GUIContent(head, isDetachedHead ? GitEditorStyles.CommitIcon : GitEditorStyles.BranchIcon);
            this.popup = BranchMenu.GenerateMenu(this, head, isDetachedHead);
        }
    }
}
