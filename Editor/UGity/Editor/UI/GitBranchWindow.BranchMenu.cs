using Octothorpe.UGity.Client;
using Octothorpe.UGity.Util;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Octothorpe.UGity.Editor.UI
{
    public partial class GitBranchWindow
    {
        private static class BranchMenu
        {
            public delegate void BranchMenuAction(IGitClient client, string functionName, string head, string selectedBranch);
            public delegate void BranchMenuPrompt<T>(IGitClient client, string head, string selectedBranch, T args);
            public delegate void BranchMenuDialog(IGitClient client, string head, string selectedBranch);

            private static readonly List<BranchMenuItem> globalMenuFunctions = new List<BranchMenuItem>
            {
                new BranchMenuItem
                {
                    Name = "Create Branch",
                    FormatString = "➕ New Branch",
                    Action = PromptAction<string>(Create, "Create", "Branch Name"),
                    RefreshOnClick = true,
                },
            };

            private static readonly List<BranchMenuItem> menuFunctions = new List<BranchMenuItem>
            {
                new BranchMenuItem
                {
                    Name = "Create Branch",
                    FormatString = "➕ New Branch From",
                    ShowOnCurrentBranch = true,
                    EnableOnCurrentBranch = true,
                    AddSeparator = true,
                    Action = PromptAction<(string, bool)>(Create, "Create", "Branch Name", "Inherit upstream branch from {0}"),
                    ActionData = null,
                    RefreshOnClick = true,
                },
                new BranchMenuItem
                {
                    Name = "Checkout",
                    FormatString = "Checkout",
                    ShowOnCurrentBranch = false,
                    AddSeparator = true,
                    Action = Checkout,
                    RefreshOnClick = true,
                },
                new BranchMenuItem
                {
                    Name = "Rebase",
                    FormatString = "Rebase '{1}' onto '{0}'",
                    ShowOnCurrentBranch = false,
                    EnableOnDetachedHead = false,
                    Action = Rebase,
                },
                new BranchMenuItem
                {
                    Name = "Merge",
                    FormatString = "Merge '{0}' into '{1}'",
                    ShowOnCurrentBranch = false,
                    AddSeparator = true,
                    Action = DialogAction(Merge, "Are you sure you want to merge '{0}' into '{1}'?", "Merge"),
                },
                new BranchMenuItem
                {
                    Name = "Rename",
                    FormatString = "Rename",
                    ShowOnCurrentBranch = true,
                    EnableOnCurrentBranch = true,
                    Action = PromptAction<string>(Rename, "Rename", "New Branch Name"),
                    RefreshOnClick = true,
                },
                new BranchMenuItem
                {
                    Name = "Delete",
                    FormatString = "Delete",
                    ShowOnCurrentBranch = true,
                    EnableOnCurrentBranch = false,
                    Action = DialogAction(Delete, "Are you sure you want to delete branch '{0}'?", "Delete"),
                    RefreshOnClick = true,
                },
            };

            public static GenericMenu GenerateMenu(GitBranchWindow window, string head, bool isDetachedHead)
            {
                GenericMenu menu = new GenericMenu();
                
                // Add global menu functions
                foreach(BranchMenuItem item in globalMenuFunctions)
                {
                    menu.AddItem(new GUIContent(item.FormatString), false, DoAction(window, head, item), item.ActionData);
                }

                // Add a separator between the global functions and the branch menus
                menu.AddSeparator("");

                // Add branch submenus
                foreach(string branch in window.Client.GetBranches())
                {
                    // If HEAD is detached, it will be shown in the branch list as (HEAD detached at ...), so skip that one
                    if(branch.StartsWith("(")) continue;

                    // Replace the slash in the upstream string with an alternative char to avoid Unity creating a submenu
                    string upstream = window.Client.GetUpstream(branch);
                    if(upstream != null)
                        upstream = upstream.Replace("/", " \u2215 ");

                    // Is this branch the current one?
                    bool isCurrentBranch = (branch == head);

                    // Text that will be shown on the menu item
                    string name = $"{branch}\t{upstream}";

                    // Populate the submenu for this branch
                    foreach(BranchMenuItem item in menuFunctions)
                    {
                        if(isCurrentBranch && !item.ShowOnCurrentBranch) continue;
                        if(isDetachedHead && !item.ShowOnDetachedHead) continue;

                        // Create the path string for the generic menu item
                        string itemText = string.Format(item.FormatString, branch, head);
                        string path = string.Concat(name, "/", itemText);

                        // Add an item as enabled or disabled depending on the context
                        bool showEnabled = 
                            (item.EnableOnDetachedHead || !isDetachedHead) &&
                            (item.EnableOnCurrentBranch || !isCurrentBranch);
                        
                        if(showEnabled)
                            menu.AddItem(new GUIContent(path), false, DoAction(window, head, item), branch);
                        else
                            menu.AddDisabledItem(new GUIContent(path));

                        // Add a separator after the item if specified
                        if(item.AddSeparator)
                            menu.AddSeparator(name + "/");
                    }
                }

                return menu;
            }

            private static BranchMenuAction PromptAction<T>(BranchMenuPrompt<T> action, string confirmText, params string[] labels)
            {
                return (window, functionName, head, branch) =>
                {
                    for(int i = 0; i < labels.Length; i++)
                    {
                        labels[i] = string.Format(labels[i], branch, head);
                    }
                    
                    T result = Prompt.Create<T>(functionName, confirmText, out bool cancelled, labels);
                    if(!cancelled)
                        action(window, head, branch, result);
                };
            }
            
            private static BranchMenuAction DialogAction(BranchMenuDialog action, string textFormat, string okText)
            {
                return (window, functionName, head, branch) =>
                {
                    string text = string.Format(textFormat, branch, head);

                    bool confirm = EditorUtility.DisplayDialog(functionName, text, okText, "Cancel");
                    if(confirm)
                    {
                        action(window, head, branch);
                    }
                };
            }

            private static GenericMenu.MenuFunction2 DoAction(GitBranchWindow window, string head, BranchMenuItem item)
            {
                return (data) =>
                {
                    try
                    {
                        item.Action?.Invoke(window.Client, item.Name, head, data?.ToString());
                        if(item.RefreshOnClick)
                        {
                            window.RefreshMenu();
                        }
                    }
                    catch(GitClientException e)
                    {
                        EditorUtility.DisplayDialog(item.Name + " failed", e.Message, "Close");
                    }
                };
            }

            private static void Create(IGitClient client, string head, string selectedBranch, string branchName) => Create(client, head, selectedBranch, (branchName, false));

            private static void Create(IGitClient client, string head, string selectedBranch, (string branchName, bool inherit) args)
            {
                string name = args.branchName;
                name = window.Client.ValidateBranchName(name);

                GitCommand command = Git.Checkout;

                // TODO: Make GitCommand options preserve order when added
                if(args.inherit)
                    command.WithArgument("--track=inherit");

                command.WithArgument("-b").WithArgument(name);
                
                if(selectedBranch != null)
                    command.WithArgument(selectedBranch);
                
                window.Client.Execute(command);
            }

            private static void Checkout(IGitClient client, string functionName, string head, string selectedBranch)
            {
                GitCommandResult result = client.Execute(Git.Checkout.WithArgument(selectedBranch));
                if(result.ExitCode != 0)
                    throw new GitClientException(result);
            }

            // Rebase current branch ONTO branch
            private static void Rebase(IGitClient client, string functionName, string head, string selectedBranch)
            {
                client.Execute(Git.Rebase.WithArgument(selectedBranch));
            }

            // Merge branch into current branch
            private static void Merge(IGitClient client, string head, string selectedBranch)
            {
                client.Execute(Git.Merge.WithArgument(selectedBranch));
            }

            private static void Rename(IGitClient client, string head, string selectedBranch, string newName)
            {
                newName = window.Client.ValidateBranchName(newName);

                window.Client.Execute(Git.Branch
                    .WithOption("-m")
                    .WithArgument(selectedBranch)
                    .WithArgument(newName));
            }

            private static void Delete(IGitClient client, string head, string selectedBranch)
            {
                GitCommandResult result = client.Execute(Git.Branch.WithOption("-d").WithArgument(selectedBranch));
                if(result.ExitCode != 0)
                    throw new GitClientException(result);
            }

            private class BranchMenuItem
            {
                /// <summary>
                /// Name of the operation. What this property is used for depends on the action
                /// </summary>
                public string Name { get; set; }
                /// <summary>
                /// Format string for the menu item's text. 
                /// {0} will be replaced with the current branch name, 
                /// {1} will be replaced with the branch name being acted on.
                /// </summary>
                public string FormatString { get; set; }
                /// <summary>
                /// Should this menu item be displayed in the submenu for the current branch?
                /// </summary>
                public bool ShowOnCurrentBranch { get; set; } = true;
                /// <summary>
                /// If this menu item is displayed in the submenu for the current branch, should it be enabled?
                /// </summary>
                public bool EnableOnCurrentBranch { get; set; } = true;
                /// <summary>
                /// Should this menu item be displayed when in a detached HEAD state?
                /// </summary>
                public bool ShowOnDetachedHead { get; set; } = true;
                /// <summary>
                /// If this menu item is displayed when in a detached HEAD state, should it be enabled?
                /// </summary>
                public bool EnableOnDetachedHead { get; set; } = true;
                /// <summary>
                /// Should the menu be refreshed after this item's action is executed?
                /// </summary>
                public bool RefreshOnClick { get; set; } = false;
                /// <summary>
                /// Should a separator be added after this menu item?
                /// </summary>
                public bool AddSeparator { get; set; } = false;
                /// <summary>
                /// Action to be performed when this menu item is selected.
                /// </summary>
                public BranchMenuAction Action { get; set; }
                /// <summary>
                /// Data to send to the action when it is used as a global function. Does nothing for a branch item
                /// </summary>
                public object ActionData { get; set; }

            }
        }
    }
}
