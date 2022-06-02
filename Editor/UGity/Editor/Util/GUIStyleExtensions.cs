using UnityEngine;

namespace Octothorpe.UGity.Editor.Util
{
    public enum GUIState
    {
        Active,
        Focused,
        Hover,
        Normal,
        ActiveOn,
        FocusedOn,
        HoverOn,
        NormalOn
    }

    public static class GUIStyleExtensions
    {
        public static GUIStyle Customize(this GUIStyle baseStyle) => new GUIStyle(baseStyle);

        public static GUIStyle WithAlignment(this GUIStyle style, TextAnchor alignment)
        {
            style.alignment = alignment;
            return style;
        }
        
        public static GUIStyle WithBorder(this GUIStyle style, RectOffset border)
        {
            style.border = border;
            return style;
        }

        public static GUIStyle WithClipping(this GUIStyle style, TextClipping clipping)
        {
            style.clipping = clipping;
            return style;
        }

        public static GUIStyle WithContentOffset(this GUIStyle style, Vector2 contentOffset)
        {
            style.contentOffset = contentOffset;
            return style;
        }

        public static GUIStyle WithFixedHeight(this GUIStyle style, float fixedHeight)
        {
            style.fixedHeight = fixedHeight;
            return style;
        }

        public static GUIStyle WithFixedWidth(this GUIStyle style, float fixedWidth)
        {
            style.fixedWidth = fixedWidth;
            return style;
        }

        public static GUIStyle WithFont(this GUIStyle style, Font font)
        {
            style.font = font;
            return style;
        }

        public static GUIStyle WithFontStyle(this GUIStyle style, FontStyle fontStyle)
        {
            style.fontStyle = fontStyle;
            return style;
        }

        public static GUIStyle WithFontSize(this GUIStyle style, int fontSize)
        {
            style.fontSize = fontSize;
            return style;
        }

        public static GUIStyle WithImagePosition(this GUIStyle style, ImagePosition imagePosition)
        {
            style.imagePosition = imagePosition;
            return style;
        }

        public static GUIStyle WithMargin(this GUIStyle style, int left, int right, int top, int bottom)
        {
            style.margin = new RectOffset(left, right, top, bottom);
            return style;
        }

        public static GUIStyle WithOverflow(this GUIStyle style, RectOffset overflow)
        {
            style.overflow = overflow;
            return style;
        }

        public static GUIStyle WithPadding(this GUIStyle style, int left, int right, int top, int bottom)
        {
            style.padding = new RectOffset(left, right, top, bottom);
            return style;
        }

        public static GUIStyle WithRichText(this GUIStyle style, bool richText)
        {
            style.richText = richText;
            return style;
        }

        public static GUIStyle WithStretchHeight(this GUIStyle style, bool stretchHeight)
        {
            style.stretchHeight = stretchHeight;
            return style;
        }

        public static GUIStyle WithStretchWidth(this GUIStyle style, bool stretchWidth)
        {
            style.stretchWidth = stretchWidth;
            return style;
        }

        public static GUIStyle WithWordWrap(this GUIStyle style, bool wordWrap)
        {
            style.wordWrap = wordWrap;
            return style;
        }

        public static GUIStyle WithBackground(this GUIStyle style, GUIState state, Texture2D background)
        {
            GetState(style, state).background = background;
            return style;
        }
        
        public static GUIStyle WithTextColor(this GUIStyle style, GUIState state, Color textColor)
        {
            GetState(style, state).textColor = textColor;
            return style;
        }

        private static GUIStyleState GetState(GUIStyle style, GUIState state)
        {
            switch(state)
            {
                case GUIState.Active:
                    return style.active;
                case GUIState.Focused:
                    return style.focused;
                case GUIState.Hover:
                    return style.hover;
                case GUIState.Normal:
                    return style.normal;
                case GUIState.ActiveOn:
                    return style.onActive;
                case GUIState.FocusedOn:
                    return style.onFocused;
                case GUIState.HoverOn:
                    return style.onHover;
                case GUIState.NormalOn:
                    return style.onNormal;
                default:
                    return style.normal;
            }
        }
    }
}
