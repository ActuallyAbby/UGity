using UnityEditor;

using UnityEngine;

namespace Octothorpe.UGity.Editor.UI
{
    public class GitInitModal : GitEditorWindow.Modal
    {
        protected override string Title { get; set; }
        protected override bool HasHeader { get; } = false;

        protected override void OnInitialize() { }
        
        protected override void OnDraw() { }

    }
}
