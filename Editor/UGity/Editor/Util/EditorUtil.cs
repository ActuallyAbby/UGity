using System;
using System.Collections;
using System.Text;

using UnityEditor;

using UnityEngine;

namespace Octothorpe.UGity.Editor.Util
{
    public static class EditorUtil
    {
        internal const string PACKAGE_DIR = "Packages/cc.octothorpe.ugity/Editor/";

        private static readonly GUILayoutOption[] flexibleSpaceOptions = new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true) };
        
        private static readonly StringBuilder builder = new StringBuilder();

        public enum TruncateMode
        { 
            /// <summary>
            /// Truncate text starting from the left side of the label, and align it to the right
            /// </summary>
            Left = 1, 
            /// <summary>
            /// Truncate text starting from the right side of the label, and align it to the left
            /// </summary>
            Right = -1 
        }
        
        public static GUIContent[] CreateContent(string[] text)
        {
            GUIContent[] contents = new GUIContent[text.Length];
            for(int i = 0; i < text.Length; i++)
            {
                contents[i] = new GUIContent(text[i]);
            }
            
            return contents;
        }

        public static GUIContent CreateIconContent(string icon, string text = null, string tooltip = null)
        {
            GUIContent content = new GUIContent(EditorGUIUtility.IconContent(icon));
            content.text = text;
            content.tooltip = tooltip;

            return content;
        }

        public static GUIStyle GetLabelStyle(this Color color)
        {
            GUIStyle colorStyle = new GUIStyle(GitEditorStyles.Label);
            colorStyle.normal.textColor = color;
            return colorStyle;
        }

        public static Texture2D CreateColorTexture(Color color)
        {
            Texture2D texture = new Texture2D(16, 16);
            for(int i = 0; i < 16; i++)
            {
                for(int j = 0; j < 16; j++)
                {
                    texture.SetPixel(i, j, color);
                }
            }
            //texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        public static Rect GetFlexibleSpace() => EditorGUILayout.GetControlRect(flexibleSpaceOptions);

        public static Rect Inset(this Rect rect, RectOffset margins)
        {
            return Inset(rect, margins.left, margins.right, margins.top, margins.bottom);
        }

        public static Rect Inset(this Rect rect, int left = 0, int right = 0, int top = 0, int bottom = 0)
        {
            Rect newRect = new Rect(rect);

            newRect.x += left;
            newRect.width -= (left + right);
            
            newRect.y += top;
            newRect.height -= (top + bottom);

            return newRect;
        }

        public static Rect[] Divide(this Rect rect, params GUIContent[] content)
        {
            float[] widths = new float[content.Length];
            for(int i = 0; i < content.Length; i++)
            {
                widths[i] = GUI.skin.label.CalcSize(content[i]).x;
            }

            return Divide(rect, widths);
        }

        public static Rect[] Divide(this Rect rect, params float[] widths)
        {
            // Allocate an array for the new rects
            Rect[] newRects = new Rect[widths.Length + 1];

            // For each width, calculate the new rect
            for(int i = 0; i < widths.Length; i++)
            {
                newRects[i] = new Rect(rect);
                newRects[i].width = widths[i];
                rect.width -= widths[i];
                rect.x += widths[i];

                // If this is not the last rect, shift the rect to the right
                //if(i < widths.Length - 1)
                //    newRects[i].x += widths[i];
            }

            newRects[widths.Length] = rect;

            return newRects;
        }

        /// <summary>
        /// Create a rect that is a subdivision of another
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="borrowedWidth"></param>
        /// <param name="height"></param>
        /// <param name="borrowedHeight"></param>
        /// <param name="width"></param>
        /// <returns></returns>
        public static Rect SubRect(Rect parent, float borrowedWidth = 0f, float height = 0f, float borrowedHeight = 0f, float width = 0f)
        {
            Rect copy = parent;
            return CutRect(ref copy, borrowedWidth, height, borrowedHeight, width);
        }

        /// <summary>
        /// Cuts one Rect by another, modifying the original rect so that it does not contain the Rect cut from it<br/>
        /// <br/>
        /// The size of the new rect depends on the format of <c>borrowedWidth</c> or <c>borrowedHeight</c>:
        /// <list type="bullet">
        ///     <item><description>If the value is positive <b>AND</b> greater than 1, the cut will be made on the left/top of <c>parent</c></description></item>
        ///     <item><description>If the value is negative <b>AND</b> less than -1, the cut will be made on the right/bottom of <c>parent</c></description></item>
        ///     <item><description>If the value is within -1 and +1, it will be interpreted as a percentage of the width of <c>parent</c>, and then use the above rules</description></item>
        /// </list>
        /// <br/>
        /// To cut without modifying the parent rect, use <see cref="SubRect(Rect, float, float, float, float)"/>
        /// </summary>
        /// <param name="parent">Rect to cut</param>
        /// <param name="borrowedWidth">Width taken by the new rect. This is removed from <c>parent</c></param>
        /// <param name="height">Height of the new rect. This does <i>not</i> affect <c>parent</c></param>
        /// <param name="borrowedHeight">Height taken by the new rect. This is removed from <c>parent</c></param>
        /// <param name="width">Width of the new rect. This does <i>not</i> affect <c>parent</c></param>
        /// <returns>The rect which was cut out</returns>
        public static Rect CutRect(ref Rect parent, float borrowedWidth = 0f, float height = 0f, float borrowedHeight = 0f, float width = 0f)
        {
            if(height == 0f)
                height = parent.height;

            if(width == 0f)
                width = parent.width;

            Rect subRect = new Rect(parent.x, parent.y, width, height);

            if(borrowedWidth != 0f)
                CutHorizontal(ref parent, ref subRect, borrowedWidth);
            else if(borrowedHeight != 0f)
                CutVertical(ref parent, ref subRect, borrowedHeight);

            return subRect;
        }

        public static string GetAssetObjectPath(string path) => !path.EndsWith(".meta") ? path : path.Substring(0, path.Length - 5);

        public static float GetMaximumWidth(GUIContent[] content, GUIStyle style)
        {
            float maxWidth = 0f;
            foreach(GUIContent c in content)
            {
                float width = style.CalcSize(c).x;
                if(width > maxWidth)
                    maxWidth = width;
            }

            return maxWidth;
        }
        
        public static bool DrawLabelTruncated(Rect rect, float leftBound, float rightBound, string text, GUIStyle style, char? separator = ' ', TruncateMode mode = TruncateMode.Left)
        {
            const string symbol = "...";
            const int symbolLength = 3;
            const float padding = 4f;

            // We know that a null or empty string will fit, return true and don't even draw anything
            if(string.IsNullOrEmpty(text))
                return true;

            // Create a GUIContent from the text and calculate the initial rect size
            GUIContent content = new GUIContent(text);
            RecalcRect();

            // If the text does not fit, begin truncating
            bool textFits = IsWithinBounds();
            if(!textFits)
            {
                string[] segments;
                if(separator.HasValue)
                {
                    segments = text.Split(separator.Value);
                }
                // If a null separator is provided, split into characters
                else
                {
                    segments = new string[text.Length];
                    for(int c = 0; c < text.Length; c++)
                    {
                        segments[c] = text[c].ToString();
                    }
                }

                // Start building the text and insert the truncation symbol on the appropriate end
                builder.Clear()
                    .Append(text)
                    .Insert(mode == TruncateMode.Left ? 0 : text.Length, symbol);

                int startIndex = (mode == TruncateMode.Left ? 0 : segments.Length - 1);
                int endIndex = (segments.Length - 1) - startIndex;

                int i = startIndex;

                // Keep truncating the text as long as it does not fit within the given bounds
                while(i != endIndex && !textFits)
                {
                    int segmentLength = segments[i].Length;

                    // Index to remove the next segment from, depending on the truncate mode
                    int removeFrom = (mode == TruncateMode.Left)
                        ? symbolLength
                        : builder.Length - segmentLength - symbolLength - 1;

                    // Remove the next segment from the string
                    builder.Remove(removeFrom, segmentLength + 1);

                    // Set the text content to the newly truncated string and recalculate the rect size
                    content.text = builder.ToString();
                    RecalcRect();

                    // Update our loop variables
                    i += (int) mode;
                    textFits = IsWithinBounds();
                }
            }

            // If the text still does not fit, nullify the text content
            if(!textFits)
                content.text = null;

            EditorGUI.LabelField(rect, content, style);
            return textFits;

            bool IsWithinBounds() => leftBound <= rect.x && rect.x + rect.width <= rightBound;
            void RecalcRect()
            {
                rect.width = EditorStyles.label.CalcSize(content).x;

                switch(mode)
                {
                    case TruncateMode.Left: rect.x = (rightBound - rect.width - padding); break;
                    case TruncateMode.Right: rect.x = leftBound; break;
                }
            }
        }
        
        public static string AddPlaceholder(string textArea, string placeholderText)
        {
            if(!string.IsNullOrEmpty(textArea)) return textArea;
            if(Event.current.type == EventType.Repaint)
            {
                // Add a fake 1px margin so the highlighted border on the original text area is not clipped
                Rect rect = GUILayoutUtility.GetLastRect().Inset(1, 1, 1, 1);
                
                // Disabling the control was the only way I could get it to not steal focus
                using(new EditorGUI.DisabledScope(true))
                {
                    GitEditorStyles.Placeholder.Draw(rect, new GUIContent(placeholderText), GUIUtility.GetControlID(FocusType.Passive));
                }
            }

            return textArea;
        }

        public static void CenterWindow(EditorWindow window)
        {
            Rect position = window.position;

            int posX = (int) (Screen.currentResolution.width - position.width) / 2;
            int posY = (int) (Screen.currentResolution.height - position.height) / 2;

            window.position = new Rect(posX, posY, position.width, position.height);
        }

        // Thanks to user Mikilo on the Unity forums for this solution :)
        // https://forum.unity.com/threads/using-unitywebrequest-in-editor-tools.397466/#post-4485181
        public static void StartCoroutine(IEnumerator update, Action onTerminate = null)
        {
            EditorApplication.CallbackFunction closureCallback = null;

            closureCallback = () =>
            {
                try
                {
                    if(update.MoveNext() == false)
                        EditorApplication.update -= closureCallback;
                }
                catch(Exception e)
                {
                    Debug.LogException(e);
                    EditorApplication.update -= closureCallback;
                }
                finally
                {
                    onTerminate?.Invoke();
                }
            };

            EditorApplication.update += closureCallback;
        }

        //internal static GUISkin LoadSkin(string name) => AssetDatabase.LoadAssetAtPath<GUISkin>(PACKAGE_DIR + "Assets/Skins/" + name + ".guiskin");
        
        internal static Texture2D LoadIcon(string filename) => AssetDatabase.LoadAssetAtPath<Texture2D>(PACKAGE_DIR + "Assets/Textures/" + filename);

        internal static Font LoadFont(string filename) => AssetDatabase.LoadAssetAtPath<Font>(PACKAGE_DIR + "Assets/Fonts/" + filename);

        private static void CutHorizontal(ref Rect parent, ref Rect child, float borrowedWidth)
        {
            // If the width is within [-1, 1], parse it as a % of the parent's width
            if(borrowedWidth <= +1f && borrowedWidth >= -1f)
                borrowedWidth = (parent.width * borrowedWidth);

            // Remove the desired width from the parent rect
            float absWidth = Mathf.Abs(borrowedWidth);
            parent.width -= absWidth;

            // If a non-zero width is specified, use it. Otherwise, keep the parent's width
            if(borrowedWidth != 0f)
                child.width = absWidth;

            // If the width is positive, the new rect will be placed to the left of the parent. Otherwise, place it to the right
            if(borrowedWidth > 0f)
                parent.x += borrowedWidth;
            else if(borrowedWidth < 0f)
                child.x += parent.width;
        }

        private static void CutVertical(ref Rect parent, ref Rect child, float borrowedHeight)
        {
            // If the height is within [-1, 1], parse it as a % of the parent's height
            if(borrowedHeight <= +1f && borrowedHeight >= -1f)
                borrowedHeight = (parent.height * borrowedHeight);

            // Remove the desired height from the parent rect
            float absHeight = Mathf.Abs(borrowedHeight);
            parent.height -= absHeight;

            // If a non-zero height is specified, use it. Otherwise, keep the parent's height
            if(borrowedHeight != 0f)
                child.height = absHeight;

            // If the height is positive, the new rect will be placed above the parent. Otherwise, place it below
            if(borrowedHeight > 0f)
                parent.y += absHeight;
            else if(borrowedHeight < 0f)
                child.y += parent.height;
        }
    }
}
