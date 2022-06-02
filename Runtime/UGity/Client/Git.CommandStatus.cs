using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Octothorpe.UGity.Client
{
    public static partial class Git
    {
        public static GitStatusCommand Status => new GitStatusCommand();
    }

    public class GitStatusCommand : GitCommand<GitStatusCommand, GitStatusResult>
    {
        private const string BRANCH_INFO_PATTERN = @"^(?:No commits yet on )?(\w+)(?:\.\.\.(\w+)\/(\w+))?(?: \[[\w ]+\])?$";
        private static readonly Regex BranchInfoRegex = new Regex(BRANCH_INFO_PATTERN, RegexOptions.Compiled);

        public GitStatusCommand() : base("status") => WithOption("-z").WithOption("-b").WithOption("--no-renames");

        public override void ParseExtendedResult(in GitStatusResult result)
        {
            string branchName = null;
            string remoteName = null;
            string remoteBranchName = null;
            Dictionary<string, GitFile> files = new Dictionary<string, GitFile>();

            foreach(string line in result.Output.Split('\0'))
            {
                if(string.IsNullOrEmpty(line)) continue;

                char stateX = line[0];
                char stateY = line[1];

                string path = line.Substring(3);

                // When the -b flag is provided (and --porcelain=1), the first line of the output will read any of the following:
                // ## {branch}...{tracking} {info}      - There are commits, and the branch is tracking a remote branch. {info} may or may not display
                // ## {branch}                          - There are commits, but no upstream branch is set
                // ## No commits yet on {branch}        - There are no commits
                // ## HEAD (no branch)                  - Detached HEAD (This will not match the regex)
                if(stateX == '#')
                {
                    Match match = BranchInfoRegex.Match(path);

                    // Match did not succeed, we are in a detached HEAD state. Do not set the branch or upstream
                    if(!match.Success) continue;

                    // Set values for each group that exists
                    match.Groups.TryGetValue(1, out branchName);
                    match.Groups.TryGetValue(2, out remoteName);
                    match.Groups.TryGetValue(3, out remoteBranchName);
                }
                else
                {
                    GitState stateInIndex = GitFileStateHelper.CharToState(stateX);
                    GitState stateInTree = GitFileStateHelper.CharToState(stateY);

                    if(files.ContainsKey(path))
                        files[path] = new GitFile(path, files[path].StateInTree, files[path].StateInIndex, stateX == '?', stateX == '!');
                    else
                        files[path] = new GitFile(path, stateInTree, stateInIndex, stateX == '?', stateX == '!');
                }
            }

            result.BranchName = branchName;
            result.RemoteName = remoteName;
            result.RemoteBranchName = remoteBranchName;
            result.Changes = files;
        }
    }

    public class GitStatusResult : GitCommandResult, IEnumerable<GitFile>
    {
        public string BranchName { get; internal set; }
        public string RemoteName { get; internal set; }
        public string RemoteBranchName { get; internal set; }

        public string Upstream => string.Concat(RemoteName, '/', RemoteBranchName);

        public int Count => Changes.Count;

        internal Dictionary<string, GitFile> Changes { get; set; }

        public GitFile? First
        {
            get {
                IEnumerator<GitFile> enumerator = Changes.Values.GetEnumerator();
                if(enumerator.MoveNext())
                    return enumerator.Current;

                return null;
            }
        }   

        public GitFile this[string file] => Changes[file];

        public new IEnumerator<GitFile> GetEnumerator() => Changes.Values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public struct GitFile
    {
        public string Path { get; }
        public GitState StateInIndex { get; }
        public GitState StateInTree { get; }
        public GitStateFlags StateFlags { get; }
        public bool IsUntracked { get; }
        public bool IsIgnored { get; }

        public bool HasMergeConflict =>
            (StateInTree == GitState.Unmerged) ||   // Modified by them
            (StateInIndex == GitState.Unmerged) ||  // Modified by us
            (StateFlags == GitStateFlags.Added) ||  // Both added
            (StateFlags == GitStateFlags.Deleted);  // Both deleted

        public GitFile(string path, GitState stateInTree, GitState stateInIndex, bool isUntracked, bool isIgnored)
        {
            Path = path;

            StateInTree = stateInTree;
            StateInIndex = stateInIndex;
            StateFlags = GitFileStateHelper.EncodeState(stateInIndex, stateInTree);

            IsUntracked = isUntracked;
            IsIgnored = isIgnored;
        }

        public override string ToString()
        {
            char indexState = GitFileStateHelper.StateToChar(StateInIndex);
            char treeState = GitFileStateHelper.StateToChar(StateInTree);
            if(IsUntracked)
                treeState = '?';
            else if(IsIgnored)
                treeState = '!';

            return string.Format("[{0}{1}] {2}", indexState, treeState, Path);
        }

        public override int GetHashCode() => 467214278 + EqualityComparer<string>.Default.GetHashCode(Path);

        public override bool Equals(object obj) => obj is GitFile file && Path == file.Path;
    }

    public enum GitState : uint
    {
        Unmodified = 0,

        Modified    = GitStateFlags.Modified,
        TypeChanged = GitStateFlags.TypeChanged,
        Added       = GitStateFlags.Added,
        Deleted     = GitStateFlags.Deleted,
        Renamed     = GitStateFlags.Renamed,
        Copied      = GitStateFlags.Copied,
        Unmerged    = GitStateFlags.Unmerged,
    }

    [Flags]
    public enum GitStateFlags : uint
    {
        Unmodified           = 0,

        ModifiedInTree      = 0b00_00_00_00_00_00_01_00,
        ModifiedInIndex     = 0b00_00_00_00_00_00_10_00,
        Modified            = ModifiedInTree | ModifiedInIndex,

        TypeChangedInTree   = 0b00_00_00_00_00_01_00_00,
        TypeChangedInIndex  = 0b00_00_00_00_00_10_00_00,
        TypeChanged         = TypeChangedInTree | TypeChangedInIndex,

        AddedInTree         = 0b00_00_00_00_01_00_00_00,
        AddedInIndex        = 0b00_00_00_00_10_00_00_00,
        Added               = AddedInTree | AddedInIndex,

        DeletedInTree       = 0b00_00_00_01_00_00_00_00,
        DeletedInIndex      = 0b00_00_00_10_00_00_00_00,
        Deleted             = DeletedInTree | DeletedInIndex,

        RenamedInTree       = 0b00_00_01_00_00_00_00_00,
        RenamedInIndex      = 0b00_00_10_00_00_00_00_00,
        Renamed             = RenamedInTree | RenamedInIndex,

        CopiedInTree        = 0b00_01_00_00_00_00_00_00,
        CopiedInIndex       = 0b00_10_00_00_00_00_00_00,
        Copied              = CopiedInTree | CopiedInIndex,

        UnmergedInTree      = 0b01_00_00_00_00_00_00_00,
        UnmergedInIndex     = 0b10_00_00_00_00_00_00_00,
        Unmerged            = UnmergedInTree | UnmergedInIndex,
    }

    internal static class GitFileStateHelper
    {
        private const uint TREE_BITS  = 0b01_01_01_01_01_01_01_01;
        private const uint INDEX_BITS = 0b10_10_10_10_10_10_10_10;

        private static Dictionary<char, GitState> CharToStateMap
        {
            get {
                if(!initialized)
                    InitMap();

                return charToStateMap;
            }
        }

        private static Dictionary<GitState, char> StateToCharMap
        {
            get {
                if(!initialized)
                    InitMap();

                return stateToCharMap;
            }
        }

        private static readonly Dictionary<char, GitState> charToStateMap = new Dictionary<char, GitState>();
        private static readonly Dictionary<GitState, char> stateToCharMap = new Dictionary<GitState, char>();
        private static bool initialized;

        public static GitStateFlags EncodeState(GitState treeState, GitState indexState)
        {
            uint flags = 0;
            flags |= ((uint) treeState & TREE_BITS);
            flags |= ((uint) indexState & INDEX_BITS);

            return (GitStateFlags) flags;
        }

        public static void DecodeState(GitStateFlags flags, out GitState treeState, out GitState indexState)
        {
            uint flagValue = (uint) flags;

            uint treeFlag = flagValue & ~INDEX_BITS;    // NOT w/ flag and index bits, this unsets all index flags
            treeFlag |= (treeFlag << 1);                // OR with flag bit-shifted once to the left to get two bits representing the GitFileState
            treeState = (GitState) treeFlag;

            uint indexFlag = flagValue & ~TREE_BITS;
            indexFlag |= (indexFlag >> 1);
            indexState = (GitState) indexFlag;
        }

        public static GitState CharToState(char c)
        {
            CharToStateMap.TryGetValue(c, out GitState state);
            return state;
        }

        public static char StateToChar(GitState state)
        {
            StateToCharMap.TryGetValue(state, out char c);
            return c;
        }

        private static void InitMap()
        {
            AddMapEntries(GitState.Unmodified, ' ');
            AddMapEntries(GitState.Modified, 'M');
            AddMapEntries(GitState.TypeChanged, 'T');
            AddMapEntries(GitState.Added, 'A');
            AddMapEntries(GitState.Deleted, 'D');
            AddMapEntries(GitState.Renamed, 'R');
            AddMapEntries(GitState.Copied, 'C');
            AddMapEntries(GitState.Unmerged, 'U');

            initialized = true;

            void AddMapEntries(GitState state, char c)
            {
                charToStateMap[c] = state;
                stateToCharMap[state] = c;
            }
        }
    }

    internal static class GroupCollectionExtensions
    {
        public static void TryGetValue(this GroupCollection groups, int index, out string value)
        {
            if(index >= groups.Count)
                value = null;
            else
                value = groups[index].Value;
        }
    }
}
