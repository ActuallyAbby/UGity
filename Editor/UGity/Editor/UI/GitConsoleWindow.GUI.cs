using Octothorpe.UGity.Client;
using Octothorpe.UGity.Editor.Util;
using Octothorpe.UGity.Util;

using UnityEditor;

using UnityEngine;

namespace Octothorpe.UGity.Editor.UI
{
    public partial class GitConsoleWindow : GitEditorUtilityWindow<GitConsoleWindow>
    {
        private const float TOOLBAR_BTN_PADDING = 4f;

        protected override string Title { get; set; } = "Git Console";
        protected override Texture Icon => GitEditorStyles.GitIcon;
        protected override bool HasHeader { get; } = false;

        private Rect windowRect;
        private Rect toolbarRect;
        private Rect inputRect;

        private Vector2 scroll;
        private string input;
        private int selectStart;
        private int selectEnd;
        private bool isMaxScroll;
        private bool resetInput;
        
        protected override void OnDraw()
        {
            EventType eventType = Event.current.type;

            if(eventType == EventType.Layout)
            {
                this.windowRect.Set(0, 0, position.width, position.height);
                this.toolbarRect = EditorUtil.CutRect(ref this.windowRect, borrowedHeight: 20f);
                this.inputRect = EditorUtil.CutRect(ref this.windowRect, borrowedHeight: -24f);
            }

            DrawToolbar(eventType, this.toolbarRect);
            DrawLog(eventType, this.windowRect);
            DrawInput(eventType, this.inputRect);
        }

        private void DrawToolbar(EventType eventType, Rect rect)
        {
            if(eventType == EventType.Repaint)
                EditorStyles.toolbar.Draw(rect, GUIContent.none, 0);

            if(Button("Clear", EditorStyles.toolbarButton))
                Clear();

            if(Button("Filters", EditorStyles.toolbarDropDown))
                DrawFiltersMenu(rect);   

            bool Button(string text, GUIStyle style)
            {
                GUIContent content = new GUIContent(text);
                float width = style.CalcSize(content).x + (TOOLBAR_BTN_PADDING * 2);
                return GUI.Button(EditorUtil.CutRect(ref rect, width), content, style);
            }
        }

        private void DrawFiltersMenu(in Rect parent)
        {
            GenericMenu menu = new GenericMenu();

            menu.AddItem(new GUIContent("All"), Filter == Entry.DisplayFlags.All, () =>
            {
                if(Filter == Entry.DisplayFlags.All)
                    Filter = Entry.DisplayFlags.NonHidden;
                else
                    Filter = Entry.DisplayFlags.All;
            });

            menu.AddSeparator(null);

            foreach(Entry.DisplayFlags flag in default(Entry.DisplayFlags).Values())
            {
                // Flag must must represent a unique value (power of two)
                if(!flag.IsUniqueFlag()) continue;

                string displayName = ObjectNames.NicifyVariableName(flag.ToString());
                bool isOn = Filter.HasFlags(flag);

                menu.AddItem(new GUIContent(displayName), isOn, () => Filter ^= flag);
            }

            menu.DropDown(parent);
        }

        private void DrawLog(EventType eventType, in Rect viewRect)
        {
            if(eventType.IsNone(EventType.Repaint, EventType.MouseDown, EventType.MouseDrag, EventType.ScrollWheel)) return;

            Rect logRect = new Rect(position.x, position.y, position.width, LineHeight * VisibleRowCount);

            float maxScroll = Mathf.Max(0, logRect.height - viewRect.height);
            if(this.isMaxScroll)
                this.scroll.y = maxScroll;

            using(var view = new GUI.ScrollViewScope(viewRect, this.scroll, logRect, GUIStyle.none, GUI.skin.verticalScrollbar))
            {
                float newX = view.scrollPosition.x;
                float newY = view.scrollPosition.y;

                if(this.scroll.x != newX || this.scroll.y != newY)
                {
                    this.isMaxScroll = (newY == maxScroll);
                    this.scroll = new Vector2(newX, newY);
                }

                if(eventType == EventType.Repaint)
                {
                    CalcVisibleRows(logRect, viewRect.height, out int min, out int max);
                    if(min == -1) return;

                    for(int row = min;row <= max;row++)
                    {
                        Rect rowRect = GetRectForRow(logRect, row);
                        int i = RowToEntryIndex(row);

                        GUIContent content = new GUIContent(entries[i].Text);
                        //Entry result = entries[i]; // TODO: Allow viewing of details within the log window

                        GUIStyle background = i % 2 == 0 ? EvenRowStyle : OddRowStyle;
                        background.Draw(rowRect, GUIContent.none, 0, row >= this.selectStart && row <= this.selectEnd);

                        GUIStyle foreground = InfoText;
                        rowRect.height = foreground.CalcSize(content).y;
                        foreground.Draw(rowRect, content, 0);
                    }
                }
                else if(eventType == EventType.MouseDown)
                {
                    int newStart = -1;
                    int newEnd = -1;

                    int row = GetRowAtPos(logRect, Event.current.mousePosition);

                    int entryIndex = RowToEntryIndex(row);
                    if(entryIndex != -1)
                    {
                        Entry entry = entries[entryIndex];

                        newStart = (row - entry.SelectRange.x);
                        newEnd = (row + entry.SelectRange.y);
                    }

                    if(newStart != this.selectStart || newEnd != this.selectEnd)
                    {
                        this.selectStart = newStart;
                        this.selectEnd = newEnd;

                        Repaint();
                    }
                }
            }
        }

        private void DrawInput(EventType eventType, in Rect rect)
        {
            const string controlName = "GitConsoleInput";

            if(eventType == EventType.Layout) return;

            GUI.SetNextControlName(controlName);
            using(var scope = new EditorGUI.ChangeCheckScope())
            {
                this.input = EditorGUI.TextField(rect, this.input, InputStyle);
                if(scope.changed)
                    this.historyPointer = this.commandHistory.Last;
            }

            EditorGUI.LabelField(rect, CARET, InputPrefix);

            // If enter is pressed, submit the command
            if(Event.current.keyCode == KeyCode.Return)
            {
                this.historyPointer = this.commandHistory.Last;
                string command = this.input;
                this.input = "";

                EditorGUI.FocusTextInControl(controlName);

                // If the string was empty, do not execute anything
                if(string.IsNullOrWhiteSpace(command)) return;

                ExecuteGitCommand(command);
            }
            // If the reset flag is set, re-focus the input field and un-set the flag
            else if(this.resetInput)
            {
                this.resetInput = false;
                EditorGUI.FocusTextInControl(controlName);
            }
            // If the up arrow is pressed, move the the "previous" element in the history
            else if(Event.current.keyCode == KeyCode.UpArrow)
            {
                if(this.historyPointer?.Previous != null)
                {
                    this.historyPointer = this.historyPointer.Previous;
                    this.input = this.historyPointer.Value;
                    this.resetInput = true;

                    EditorGUI.FocusTextInControl(null);
                }
            }
            // If the down arrow is pressed, move to the "next" element in the history
            else if(Event.current.keyCode == KeyCode.DownArrow)
            {
                if(this.historyPointer?.Next != null)
                {
                    this.historyPointer = this.historyPointer.Next;
                    this.input = this.historyPointer.Value;
                    this.resetInput = true;

                    EditorGUI.FocusTextInControl(null);
                }
            }
            // Prevent headaches when the user types 'git ' out of habit :)
            else if(this.input.StartsWith("git "))
            {
                this.input = this.input.Substring(4);
                EditorGUI.FocusTextInControl(null);
                this.resetInput = true;
            }
        }

        // Initializes all of the UI elements and styles. TODO: Clean this up
        protected override void OnFirstInitialize()
        {
            git = new GitClient(Application.dataPath);

            InfoText = new GUIStyle(EditorStyles.label);
            InfoText.font = GitEditorStyles.FontMonospaced;
            InfoText.fontSize = 0;
            InfoText.richText = true;
            InfoText.padding = new RectOffset(4, 4, 0, 0);

            ErrorText = new GUIStyle(EditorStyles.label);
            ErrorText.font = GitEditorStyles.FontMonospaced;
            ErrorText.fontSize = 0;
            ErrorText.richText = true;
            ErrorText.padding = new RectOffset(4, 4, 0, 0);

            EvenRowStyle = "CN EntryBackEven";
            EvenRowStyle.font = GitEditorStyles.FontMonospaced;
            OddRowStyle = "CN EntryBackOdd";
            OddRowStyle.font = GitEditorStyles.FontMonospaced;

            InputPrefix = new GUIStyle(EditorStyles.label);
            InputPrefix.font = GitEditorStyles.FontMonospaced;
            InputPrefix.fontSize = 16;
            float caretWidth = InputPrefix.CalcSize(new GUIContent(CARET)).x;
            InputPrefix.fixedWidth = caretWidth;
            InputPrefix.alignment = TextAnchor.MiddleLeft;
            InputPrefix.padding = new RectOffset(4, 0, 0, 0);

            InputStyle = new GUIStyle(EditorStyles.textField);
            InputStyle.font = GitEditorStyles.FontMonospaced;
            InputStyle.fontSize = 16;
            RectOffset padding = InputStyle.padding;
            padding.left = (int) caretWidth;
            InputStyle.padding = padding;
            InputStyle.alignment = TextAnchor.MiddleLeft;
        }
    }
}
