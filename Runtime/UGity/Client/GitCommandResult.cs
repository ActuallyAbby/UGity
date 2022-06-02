using System.Collections;
using System.Collections.Generic;

namespace Octothorpe.UGity.Client
{
    public class GitCommandResult : IEnumerable<string>
    {
        public int ExitCode { get; private set; }
        public string Output { get; private set; }
        public string Error { get; private set; }

        public string[] Lines => Output.Split('\n');

        internal static T Create<T>(int exitCode, string output, string error) where T : GitCommandResult, new()
        {
            T result = new T();
            result.ExitCode = exitCode;
            result.Output = output;
            result.Error = error;

            return result;
        }

        public IEnumerator<string> GetEnumerator() => ((IEnumerable<string>) Lines).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
