using System;

using Octothorpe.UGity.Client;
using Octothorpe.UGity.Editor.Util;

using UnityEditor;

using UnityEngine;

namespace Octothorpe.UGity.Editor
{
    public static class GitEditorStyles
    {
        public static readonly Texture2D CommitWindowIcon = EditorUtil.LoadIcon("check-bold-color.png");
        public static readonly Texture2D PushWindowIcon = EditorUtil.LoadIcon("arrow-top-right.png");
        public static readonly Texture2D PullWindowIcon = EditorUtil.LoadIcon("arrow-bottom-left.png");
        public static readonly Texture2D CommitIcon = EditorUtil.LoadIcon("git-commit-vertical.png");
        public static readonly Texture2D BranchIcon = EditorUtil.LoadIcon("git-branch.png");
        public static readonly Texture2D RestoreIcon = EditorUtil.LoadIcon("file-restore.png");
        public static readonly Texture2D DiffIcon = EditorUtil.LoadIcon("file-diff.png");
        public static readonly Texture2D CompareIcon = EditorUtil.LoadIcon("git-compare.png");
        public static readonly Texture2D GitIcon = EditorUtil.LoadIcon("git-color.png");
        
        public static readonly Font FontMonospaced = EditorUtil.LoadFont("Inconsolata-Regular.ttf");

        public static GUIStyle ConfirmButton => confirmButton.Value;
        public static GUIStyle CommitSummaryItem => commitSummaryItem.Value;
        public static GUIStyle Header => header.Value;
        public static GUIStyle MonospacedLabel => monospacedLabel.Value;
        public static GUIStyle Placeholder => placeholder.Value;
        public static GUIStyle Label => label.Value;
        public static GUIStyle StatusBarButton => statusBarButton.Value;
        public static GUIStyle TextArea => textArea.Value;
        public static GUIStyle TerminalButton => terminalButton.Value;
        public static GUIStyle Toggle => toggle.Value;
        public static GUIStyle WarningBox => warningBox.Value;

        public static Texture2D RefreshIcon => refreshIcon.Value;
        public static Texture2D RemoveIcon => removeIcon.Value;
        public static Texture2D AddIcon => addIcon.Value;
        public static Texture2D HierarchyIcon => hierarchyIcon.Value;

        public static Color ColorUntracked => new Color(0.8f, 0.5f, 0.5f);
        public static Color ColorAdded => new Color(0.5f, 0.8f, 0.5f);
        public static Color ColorModified => new Color(0.5f, 0.6f, 1.0f);
        public static Color ColorDeleted => new Color(0.5f, 0.5f, 0.5f);
        public static Color ColorUnmerged => new Color(1.0f, 0.8f, 0.4f);
        public static Color ColorDefault => new Color(1.0f, 1.0f, 1.0f);

        private static readonly Lazy<Texture2D> refreshIcon = LazyIconContent("TreeEditor.Refresh");
        private static readonly Lazy<Texture2D> removeIcon = LazyIconContent("Toolbar Minus");
        private static readonly Lazy<Texture2D> addIcon = LazyIconContent("Toolbar Plus");
        private static readonly Lazy<Texture2D> hierarchyIcon = LazyIconContent("UnityEditor.HierarchyWindow");
        
        private static readonly Lazy<GUIStyle> confirmButton = new Lazy<GUIStyle>(() => GUI.skin.button.Customize().WithMargin(8, 8, 8, 8).WithFixedHeight(32f));

        private static readonly Lazy<GUIStyle> commitSummaryItem = new Lazy<GUIStyle>(() => GUI.skin.label.Customize()
            .WithFixedHeight(32f)
            .WithMargin(4, 4, 8, 8));

        private static readonly Lazy<GUIStyle> header = new Lazy<GUIStyle>(() => EditorStyles.helpBox.Customize()
            .WithFontSize(13)
            .WithFontStyle(FontStyle.Bold)
            .WithAlignment(TextAnchor.MiddleLeft)
            .WithRichText(true)
            .WithStretchWidth(true)
            .WithMargin(8, 8, 8, 8)
            .WithPadding(4, 4, 4, 4)
            .WithFixedHeight(32f));

        private static readonly Lazy<GUIStyle> monospacedLabel = new Lazy<GUIStyle>(() => GUI.skin.label.Customize()
            .WithFont(FontMonospaced)
            .WithAlignment(TextAnchor.MiddleCenter));
        
        private static readonly Lazy<GUIStyle> placeholder = new Lazy<GUIStyle>(() => EditorStyles.textArea.Customize()
            .WithBackground(GUIState.Normal, null)
            .WithPadding(2, 2, 2, 2)
            .WithAlignment(TextAnchor.UpperLeft));

        private static readonly Lazy<GUIStyle> label = new Lazy<GUIStyle>(() => EditorStyles.label.Customize().WithRichText(true));

        private static readonly Lazy<GUIStyle> statusBarButton = new Lazy<GUIStyle>(() => new GUIStyle("ToolbarDropDownLeft").Customize()
            .WithMargin(0, 0, 0, 0)
            .WithAlignment(TextAnchor.MiddleLeft)
            .WithBackground(GUIState.Normal, EditorUtil.CreateColorTexture(new Color(0.098f, 0.098f, 0.098f)))
            .WithBackground(GUIState.Hover, EditorUtil.CreateColorTexture(new Color(0.235f, 0.235f, 0.235f)))
            .WithBackground(GUIState.Active, EditorUtil.CreateColorTexture(new Color(0.313f, 0.313f, 0.313f))));

        private static readonly Lazy<GUIStyle> terminalButton = new Lazy<GUIStyle>(() => ConfirmButton.Customize()
            .WithFont(FontMonospaced)
            .WithFontSize(14)
            .WithAlignment(TextAnchor.MiddleLeft));

        private static readonly Lazy<GUIStyle> textArea = new Lazy<GUIStyle>(() => GUI.skin.textArea.Customize().WithMargin(8, 8, 4, 4).WithPadding(4, 4, 4, 4));

        private static readonly Lazy<GUIStyle> toggle = new Lazy<GUIStyle>(() => GUI.skin.toggle.Customize().WithMargin(8, 8, 4, 4));

        private static readonly Lazy<GUIStyle> warningBox = new Lazy<GUIStyle>(() => EditorStyles.helpBox.Customize()
            .WithAlignment(TextAnchor.MiddleLeft)
            .WithFontSize(13)
            .WithFontStyle(FontStyle.Bold)
            .WithPadding(4, 4, 4, 4)
            .WithStretchWidth(true)
            .WithRichText(true));

        public static GUIStyle GetLabelStyle(GitFile file) => GetFileColor(file).GetLabelStyle();

        public static GUIStyle GetSummaryLabelStyle(GitFile file) => CommitSummaryItem.Customize().WithTextColor(GUIState.Normal, GetFileColor(file));

        public static Color GetFileColor(GitFile file)
        {
            if(file.HasMergeConflict)
                return ColorUnmerged;
            if(file.StateInIndex == GitState.Added)
                return ColorAdded;
            else if(file.StateInIndex == GitState.Deleted)
                return ColorDeleted;
            else if(file.StateInIndex == GitState.Modified || file.StateInTree == GitState.Modified)
                return ColorModified;
            else if(file.IsUntracked)
                return ColorUntracked;
            else
                return ColorDefault;
        }

        private static Lazy<Texture2D> LazyIconContent(string name) => new Lazy<Texture2D>(() => EditorGUIUtility.IconContent(name).image as Texture2D);
    }
}
