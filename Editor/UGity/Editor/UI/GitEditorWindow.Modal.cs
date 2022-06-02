using Octothorpe.UGity.Editor.Exceptions;
using Octothorpe.UGity.Editor.Util;

using System;

using UnityEngine;

namespace Octothorpe.UGity.Editor.UI
{
    public abstract partial class GitEditorWindow
    {
        public abstract class Modal : GitEditorWindow { }

        public abstract class Modal<TSelf, TIn, TOut> : GitEditorWindow where TSelf : Modal<TSelf, TIn, TOut>
        {

            protected ref TIn Input => ref this.input;
            protected ref TOut Output => ref this.output;

            private TIn input;
            private TOut output;

            protected sealed override void OnGUI()
            {
                if(!IsInitialized)
                {
                    this.Close();
                    throw new GitEditorException("Modal was not initialized");
                }
                else
                {
                    base.OnGUI();
                }
            }

            public static TSelf Create() => CreateInstance<TSelf>();

            public TOut GetResult(TIn input)
            {
                // Don't allow the modal to be opened when it is already open
                if(IsInitialized) return Output;

                this.input = input;
                this.OnInitialize();

                if(Title != null)
                    titleContent = new GUIContent(Title);

                IsInitialized = true;

                EditorUtil.CenterWindow(this);
                base.ShowModalUtility();
                IsInitialized = false;

                return Output;
            }

            public new void ShowModalUtility() => throw new NotImplementedException("Please use " + nameof(GetResult) + "() to initialize and show the modal");
        }
    }
}
