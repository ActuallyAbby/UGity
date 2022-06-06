# [Unreleased]
## Runtime/Backend
### Changed
- `GitEditorStyles`: Reorganized members and added some new icons

# [0.8.0] - 2022-06-05
## Editor
### Fixed
- Fixed issue where commit message box would lose focus; tidied up visuals `(#8)`
- Fixed issue where errors in the Git Console were not displayed in the proper style `(#6)`

### Added
- Added background tasks with progress bars for pushing and pulling `(#11)`
- Added two additional options to the success prompt post-commit: 'Continue to Push' and 'Add Tag' `(#12)`
- Added 'Show diff' button to the commit window toolbar `(#3)`

## Runtime/Backend
### Fixed
- `GitBranchWindow`: Extra windows now also call `DestroyImmediate` upon self-destruct
- `EditorUtil`: Fixed `AddPlaceholder` placeholder visually conflicting with its textarea

### Added
- Added `IAsyncGitClient`
- Added `GitProgressReporter` - Uses `IAsyncGitClient` to track the progress of commands such as `push` and `pull` asynchronously in the background
- `GitPullWindow`: Implemented async pull function with progress bar
- `GitPushWindow`: Implemented async push function with progress bar
- `IGitClient`: Added delegates used by `GitClient` in this file at the namespace level
- `IGitClient`: Added events from `GitClient` to the base interface
- `GitConsoleWindow`: Added distinct text styles for user input, info, and error entries
- `StringExtensions`: Added `Enquote()`
- `Git`: Added `Tag`
- `GitEditorClient`: Added `OpenDiffTool()` and `OpenMergeTool()`
- `GitClientExtensions`: Added `GetUnmergedFiles()`
- `GitCommitWindow`: Added 'Show diff' button to the toolbar
- `GitTreeView`: Added check to `SelectionChanged()` to determine if diff is applicable to the selected

### Changed
- `IGitClient`: Improved documentation
- `GitClient`: Implemented `IAsyncGitClient`
- `GitConsoleWindow`: Updated `LogCommandOutput` to match new delegate parameters
- `GitCommandTimeoutExeption` -> `GitCommandTimeoutException`
- `GitEditorStyles`: Changed placeholder style as to not conflict as much on top of a text area
- `GitConsoleWindow`: Moved leftover GUI properties to the correct partial class
- `GitConsoleWindow`: Updated `OnFirstInitialize()` to construct styles via the chaining customization extensions
- `GitCommand`:  Default `WithOption` behaviour now automatically enquotes values containing a whitespace character
- `GitCommitWindow`: Success prompt now provides the additional post-commit actions 'Continue to Push' and 'Add Tag'

### Removed
- `GitClient`: Removed delegates from the file
- `GitEditorClient`: Removed `Pull()`

# [0.7.2] - 2022-06-03
## Editor
### Fixed
- Fixed an issue where moving files would un-stage them
- Fixed an issue where moving files would not move the associated meta file
- Fixed an issue where error dialogs failed to display
- Fixed an issue where untracked files would not be added during commit even when selected
- Fixed an issue where no dialog would be displayed when failing to delete a branch
- Fixed minor typo in the push window

### Added
- `Git > Push`: Added 'Push tags' option

## Runtime/Backend
### Fixed
- `GitFileWatcher`: Fixed `OnWillMoveAsset` un-staging every file by default, and not moving meta files
- `GitClient`: Fixed reversed logic for `Execute` and `TryExecute`
- `GitCommitWindow`: Fixed incorrect conditions for adding/resetting files in `Commit`
- `GitBranchWindow`: Fixed delete function silently failing
- `GitPushWindow`: Corrected label text (*commit* -> *commit(s)*)

### Added
- `GitPushWindow`: Added 'Push tags' toggle

# [0.7.1] - 2022-06-02
## Editor
### Fixed
- Fixed issue where committing would fail due to missing files

### Added
- When committing, an error dialog is now displayed if a fatal errors occurs

### Changed
- Improved message for prompt that appears when moving an asset to a location where it would be ignored

## Runtime/Backend
### Added
- Added `GitCommandTimeoutExeption`
- `IGitClient`: Added `TryExecute` methods  
- `IGitClient`: Added documentation
- `GitClient`: Implemented `TryExecute` methods
- `GitCommitWindow`: Added an error dialog for when a `GitClientException` is thrown while executing the commit command 

### Changed
- `GitFileWatcher`: Improved format for ignore prompt string
- `GitClient`: Now throws `GitCommandTimeoutException` (instead of `GitClientException`) when timing out  
- `GitClientExtensions`: Replaced `try/catch` blocks with `IGitClient.TryExecute`
- `GitCommitWindow`: Committing now ignores exceptions thrown from adding/resetting files

### Removed
- `GitFileWatcher`: Removed checks for files being outside of the git working directory
- `GitFileWatcher`: Removed unused `using` directive

# [0.7.0] - 2022-06-02
### Initial Push

[Unreleased]: https://github.com/wiizerdofwiierd/UGity/compare/v0.8.0...HEAD
[0.8.0]: https://github.com/wiizerdofwiierd/UGity/compare/v0.7.2...v0.8.0
[0.7.2]: https://github.com/wiizerdofwiierd/UGity/compare/v0.7.1...v0.7.2
[0.7.1]: https://github.com/wiizerdofwiierd/UGity/compare/v0.7.0...v0.7.1
[0.7.0]: https://github.com/wiizerdofwiierd/UGity/releases/tag/v0.7.0
