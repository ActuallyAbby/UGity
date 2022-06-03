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
### Git Menu
![UGity - Git Menu](https://user-images.githubusercontent.com/6188803/171821112-41bad790-76cc-44d8-908c-82e5a6db1f2e.png)

### Commit Window
![UGity - Commit Window](https://user-images.githubusercontent.com/6188803/171821132-60c9d437-9583-4f01-8e3b-a568f5876b76.png)

### Commit Confirmation Dialog
![UGity - Commit Confirmation Dialog](https://user-images.githubusercontent.com/6188803/171821365-1e284614-8d27-480d-8222-20160e3fabd3.png)

### Push Window
*Pushing to an existing branch*

![UGity - Push Window - Pushing to an existing branch](https://user-images.githubusercontent.com/6188803/171822280-3b74afa5-e1b1-4991-baaa-984b917db32a.png)

*Pushing to a new remote branch*

![UGity - Push Window - Pushing to a new remote branch](https://user-images.githubusercontent.com/6188803/171823846-13f46969-e40a-4065-88b4-5ee08d3528c1.png)

### Pull Window
![UGity - Pull Window](https://user-images.githubusercontent.com/6188803/171824003-9fdca30a-336c-438e-a010-372765b5e4e8.png)

### Git Console
![UGity - Git Console](https://user-images.githubusercontent.com/6188803/171824743-32152434-d92a-4cff-b138-748239dc3245.png)

## Known Issues
See [open issues](https://github.com/wiizerdofwiierd/UGity/issues?q=label%3Abug)