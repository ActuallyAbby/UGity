# [Unreleased]
## Editor
### Fixed
- Fixed(?) issue with "UGity - This should never be visible" windows appearing after re-compile `(#7)`

### Added
- Added background tasks with progress bars for pushing and pulling `(#11)`

## Runtime/Backend
### Fixed
- `GitBranchWindow`: Extra windows now also call `DestroyImmediate` upon self-destruct

### Added
- Added `IAsyncGitClient`
- Added `GitProgressReporter` - Uses `IAsyncGitClient` to track the progress of commands such as `push` and `pull` asynchronously in the background
- `GitPullWindow`: Implemented async pull function with progress bar
- `GitPushWindow`: Implemented async push function with progress bar
- `IGitClient`: Added delegates used by `GitClient` in this file at the namespace level
- `IGitClient`: Added events from `GitClient` to the base interface

### Changed
- `IGitClient`: Improved documentation
- `GitClient`: Implemented `IAsyncGitClient`
- `GitConsoleWindow`: Updated `LogCommandOutput` to match new delegate parameters
- `GitCommandTimeoutExeption` -> `GitCommandTimeoutException`

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

[Unreleased]: https://github.com/wiizerdofwiierd/UGity/compare/v0.7.2...HEAD
[0.7.2]: https://github.com/wiizerdofwiierd/UGity/compare/v0.7.1...v0.7.2
[0.7.1]: https://github.com/wiizerdofwiierd/UGity/compare/v0.7.0...v0.7.1
[0.7.0]: https://github.com/wiizerdofwiierd/UGity/releases/tag/v0.7.0
