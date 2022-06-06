using System;
using System.Collections.Generic;

using Octothorpe.UGity.Client;
using Octothorpe.UGity.Editor.Util;
using Octothorpe.UGity.Util;

using UnityEditor;

using UnityEngine;

namespace Octothorpe.UGity.Editor.UI
{
    public class GitMergeWindow : GitEditorUtilityWindow<GitMergeWindow>
    {
        protected override Texture Icon => GitEditorStyles.CompareIcon;
        protected override string Title { get; set; } = "Resolve Conflicts";
        protected override bool HasHeader { get; } = true;
        
        protected override void OnInitialize() { }

        protected override void OnDrawHeader() { }

        protected override void OnDraw() { }

    }
}
