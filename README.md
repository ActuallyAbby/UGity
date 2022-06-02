# UGity

UGity is a Unity package that provides `git` integration both in-editor as well as during runtime.  

*This package is actively under development!
If you encounter any problems or would like to make a feature request, please [create an issue](https://github.com/wiizerdofwiierd/UGity/issues/new/choose)!*

You can take a look at the currently planned features [here](https://github.com/wiizerdofwiierd/UGity/issues?q=label%3Aenhancement+label%3Aplanned)

## Installing
**Unity 2020.2 or later is required!**

You can install the package via the git URL: `git://github.com/wiizerdofwiierd/UGity.git`  
Not sure how? Follow [these instructions](https://docs.unity3d.com/2020.2/Documentation/Manual/upm-ui-giturl.html) from the Unity User Manual

## Current Features
- Initialization:
    - Initialize a git repository in your project directory
- Changes:
    - View changed files
    - Stage or un-stage files
    - Restore files to the `HEAD` state
- Committing:
    - Commit changes
    - Choose files to include or exclude in a commit
    - Amend the last commit
- Pushing:
    - Push to the upstream branch
    - Push to a different remote or remote branch
    - Automatic creation of new remote branches
- Pulling:
    - Pull into the current branch from the configured upstream branch
    - Pull into the current branch from any remote branch
    - Select flags to use for pull
- Branches:
    - View branches
    - Checkout a branch
    - Rename a branch
    - Delete a branch
- Extras:
    - Execute commands and view the output in-editor with the Git Console

## NOT Current Features (yet)
- **No in-editor diff tool, nor the ability to view the diff for a file (This is a top priority!)**
- No assistance with merging or interactive rebasing
- No viewing of commit history
- No specific handling of submodules

## Screenshots
(Coming soon)

## Known Issues
See [open issues](https://github.com/wiizerdofwiierd/UGity/issues?q=label%3Abug)