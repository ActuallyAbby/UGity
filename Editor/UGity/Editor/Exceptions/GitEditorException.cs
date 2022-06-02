using System;

namespace Octothorpe.UGity.Editor.Exceptions
{
    public class GitEditorException : Exception
    {
        public GitEditorException(string message) : base(message) { }
    }
}
