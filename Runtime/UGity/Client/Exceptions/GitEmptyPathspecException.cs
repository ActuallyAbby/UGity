namespace Octothorpe.UGity.Client
{
    public class GitEmptyPathspecException : GitClientException
    {
        public GitEmptyPathspecException(string message) : base(message) { }
    }
}
