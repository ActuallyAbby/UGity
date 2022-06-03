using System;
using System.Collections.Generic;

using Octothorpe.UGity.Client;
using Octothorpe.UGity.Editor.Exceptions;
using Octothorpe.UGity.Util;

using UnityEditor;

using UnityEngine;

namespace Octothorpe.UGity.Editor.UI
{
    public partial class GitConsoleWindow : GitEditorUtilityWindow<GitConsoleWindow>
    {
        public const string CARET = "> git ";

        protected override Type[] DockNextTo => DockNextToTypes;

        private static GUIStyle InfoText { get; set; }
        private static GUIStyle ErrorText { get; set; }
        private static GUIStyle EvenRowStyle { get; set; }
        private static GUIStyle OddRowStyle { get; set; }
        private static GUIStyle InputPrefix { get; set; }
        private static GUIStyle InputStyle { get; set; }

        private Entry.DisplayFlags Filter
        {
            get => this.displayFlags;
            set => SetFilter(value);
        }

        private float LineHeight { get; set; } = EditorGUIUtility.singleLineHeight;

        private int HistoryLength
        {
            get => this.historyLength + 1;
            set => this.historyLength = value;
        }

        private int NextVisibleRow => ++this.lastVisibleRow;

        private int VisibleRowCount => Filter == Entry.DisplayFlags.All ? entries.Count : this.rowToEntry.Count;

        /// <summary>
        /// Commands that are frequently executed, or not useful to know about. 
        /// These will be hidden in the console unless the 'Hidden' filter is enabled
        /// </summary>
        private static readonly HashSet<string> HiddenCommands = new HashSet<string>
        {
            "branch",
            "status",
            "config",
            "check-ignore",
            "check-ref-format",
            "for-each-ref",
            "rev-parse",
            "symbolic-ref",
            "log",
            "remote"
        };

        /// <summary>
        /// EditorWindow types to dock the console window next to when it is first opened
        /// </summary>
        private static readonly Type[] DockNextToTypes = new Type[]
        {
            Type.GetType("UnityEditor.ProjectBrowser,UnityEditor.dll"),
            Type.GetType("UnityEditor.ConsoleWindow,UnityEditor.dll"),
            typeof(GitCommitWindow),
        };

        private static readonly object logLock = new object();

        private static HashSet<GitConsoleWindow> windows;
        private static GitClient git;
        private static List<Entry> entries = new List<Entry>();
        private static Dictionary<IGitCommand, int> unmatchedEntries = new Dictionary<IGitCommand, int>();

        private Dictionary<int, int> rowToEntry;
        private Entry.DisplayFlags displayFlags;
        private int lastVisibleRow;

        private LinkedList<string> commandHistory;
        private LinkedListNode<string> historyPointer;
        private int historyLength = 20;     

        public static void LogCommand(string gitOptions, IGitCommand command)
        {
            Entry.DisplayFlags flags = Entry.DisplayFlags.SystemCommands;
            if(HiddenCommands.Contains(command.Name))
                flags |= Entry.DisplayFlags.Hidden;

            string message = string.Concat("git ", gitOptions, command.ToString());
            int entryIndex = LogInternal(message, Entry.EntryType.Info, flags, true);
            unmatchedEntries.Add(command, entryIndex);
        }

        public static void LogCommandOutput(IGitCommand command, bool completed, GitCommandResult result)
        {
            if(!unmatchedEntries.TryGetValue(command, out int entryIndex))
                throw new GitEditorException("Can not log output for an unlogged command");

            entries[entryIndex].SetDetails(result);
        }

        protected override void OnInitialize()
        {
            if(windows == null)
                windows = new HashSet<GitConsoleWindow>();

            windows.Add(this);

            this.rowToEntry = new Dictionary<int, int>();
            this.lastVisibleRow = -1;

            // We want to keep an empty element at index 0 for navigating out of the history
            this.commandHistory = new LinkedList<string>();
            this.commandHistory.AddFirst("");

            this.input = "";
            this.isMaxScroll = true;

            Filter = Entry.DisplayFlags.NonHidden;
        }

        private static int LogInternal(string message, Entry.EntryType type, Entry.DisplayFlags flags, bool hasTimestamp)
        {
            string[] lines = message.Split('\n');
            if(lines.Length == 0) return -1;

            int firstInsertionIndex = -1;

            // the chance of this being called asynchronously is low but never zero ¯\_(ツ)_/¯   ....unless??
            lock(logLock)
            {
                for(int i = 0, r = lines.Length - 1; i < lines.Length; i++, r--)
                {
                    entries.Add(new Entry(lines[i], null, hasTimestamp, type, flags, new Vector2Int(i, r)));

                    if(i == 0)
                        firstInsertionIndex = entries.Count - 1;

                    if(windows == null)
                        break;

                    foreach(GitConsoleWindow window in windows)
                    {
                        // This entry will be visible in this window
                        if(window.HasDisplayFlags(flags))
                        {
                            // If we don't update the rowToView mapping, the new entries
                            // will not be visible until Filter() is called again
                            int nextVisibleRow = window.NextVisibleRow;
                            window.rowToEntry[nextVisibleRow] = entries.Count - 1;

                            // Mark the window as needing to be repainted so the new entries are drawn
                            window.Repaint();
                        }
                    }
                }   
            }

            return firstInsertionIndex;
        }

        private static void Clear()
        {
            entries.Clear();
            unmatchedEntries.Clear();

            if(windows != null)
            {
                foreach(GitConsoleWindow window in windows)
                {
                    window.rowToEntry.Clear();
                    window.lastVisibleRow = -1;
                    window.Repaint();
                }
            }
        }

        private Rect GetRectForRow(Rect viewRect, int rowIndex) => new Rect(viewRect.x, viewRect.y + (LineHeight * rowIndex), viewRect.width, LineHeight);

        private int GetRowAtPos(Rect logRect, Vector2 pos)
        {
            int row = -1;
            if(logRect.Contains(pos))
            {
                float distanceFromStart = pos.y - logRect.y + this.scroll.y;
                row = (int) (distanceFromStart / LineHeight);
            }

            return row;
        }

        private int RowToEntryIndex(int rowIndex)
        {
            if(rowIndex == -1 || Filter == Entry.DisplayFlags.All)
                return rowIndex;
            else if(this.rowToEntry.ContainsKey(rowIndex))
                return this.rowToEntry[rowIndex];
            else
                return -1;
        }

        private bool HasDisplayFlags(Entry.DisplayFlags flags) => Filter.HasFlags(flags);

        private void CalcVisibleRows(Rect logRect, float viewHeight, out int min, out int max)
        {
            if(entries.Count == 0)
            {
                min = -1;
                max = -1;
                return;
            }

            Vector2 startPos = new Vector2(logRect.x, logRect.y);
            Vector2 endPos = new Vector2(logRect.x, startPos.y + viewHeight);

            min = GetRowAtPos(logRect, startPos);
            max = GetRowAtPos(logRect, endPos);

            if(max == -1 || max >= VisibleRowCount)
                max = VisibleRowCount - 1;
        }

        // TODO: the log list is static but command history is not, what am I doing??? pls fix
        private void ExecuteGitCommand(string command)
        {
            LogInternal(CARET + command, Entry.EntryType.Input, Entry.DisplayFlags.UserInput, true);
            if(this.commandHistory.Count == HistoryLength)
                this.commandHistory.RemoveLast();

            this.commandHistory.AddFirst(command);

            GitCommandResult result;
            try
            {
                result = git.Execute(new GitCommand(command));
            }
            catch(GitFatalErrorException e)
            {
                LogInternal(e.Message, Entry.EntryType.Error, Entry.DisplayFlags.UserOutput, false);
                return;
            }

            if(!string.IsNullOrEmpty(result.Error))
                LogInternal(result.Error, Entry.EntryType.Error, Entry.DisplayFlags.UserOutput, false);
            else if(!string.IsNullOrEmpty(result.Output))
                LogInternal(result.Output, Entry.EntryType.Info, Entry.DisplayFlags.UserOutput, false);
        }

        private void SetFilter(Entry.DisplayFlags flags)
        {
            this.displayFlags = flags;

            this.rowToEntry.Clear();
            this.lastVisibleRow = -1;

            if(this.displayFlags == Entry.DisplayFlags.All) return;          

            // rI: Row index, absolute index of the row without regard for filters
            // eI: Element index, actual index of the entry within the list
            for(
                int rI = 0, eI = 0;
                eI < entries.Count;
                rI++)
            {
                while(!HasDisplayFlags(entries[eI].Flags))
                {
                    eI += entries[eI].SelectRange.y + 1;
                    if(eI >= entries.Count - 1) return;
                }          

                this.lastVisibleRow = rI;
                this.rowToEntry[rI] = eI++;
            }
        }

        private struct Entry
        {
            public string Text { get; }
            public string Details { get; private set; }
            public bool ShowTimestamp { get; private set; }
            public EntryType Type { get; private set; }
            public DisplayFlags Flags { get; private set; }
            public Vector2Int SelectRange { get; }
            public DateTime Timestamp { get; }

            public string FormattedText
            {
                get {
                    if(this.formattedText == null)
                        UpdateFormattedText();

                    return this.formattedText;
                }
            }

            private string formattedText;

            public enum EntryType
            {
                Info,
                Warning,
                Error,
                Input,
            }

            [Flags]
            public enum DisplayFlags
            {
                None = 0,
                SystemCommands = 1,
                UserInput = 2,
                UserOutput = 4,
                Hidden = 8,
                All = ~None,

                NonHidden = ~Hidden,
            }

            public Entry(string text, string details, bool showTimestamp, EntryType type, DisplayFlags flags, Vector2Int selectRange)
            {
                Text = text;
                Details = details;
                ShowTimestamp = showTimestamp;
                Type = type;
                Flags = flags;
                SelectRange = selectRange;
                Timestamp = DateTime.Now;

                this.formattedText = null;
            }

            public Entry(string text, bool showTimestamp, Vector2Int selectRange) : this(text, null, showTimestamp, EntryType.Info, DisplayFlags.None, selectRange) { }

            public void SetDetails(GitCommandResult result)
            {
                if(result == null) return;
                
                bool hasErrorOutput = !string.IsNullOrEmpty(result.Error);

                Details = (hasErrorOutput ? result.Output : result.Error);
                Type = (hasErrorOutput ? EntryType.Info : EntryType.Error);

                UpdateFormattedText();
            }

            private static string GetColor(EntryType type)
            {
                switch(type)
                {
                    case EntryType.Info: return "white";
                    case EntryType.Error: return "red";
                    case EntryType.Input: return "silver";
                    default: return "reset";
                }
            }

            private void UpdateFormattedText()
            {
                string formatted = string.Format("<color={0}>{1}</color>", GetColor(Type), Text);
                if(ShowTimestamp)
                    formatted = string.Concat(Timestamp.ToString("[HH:mm:ss]"), " ", formatted);

                this.formattedText = formatted;
            }
        }
    }
}
