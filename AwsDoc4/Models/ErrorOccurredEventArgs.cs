namespace AwsDoc4.Models
{
    public class ErrorOccurredEventArgs(string title, string message, Exception? ex) : EventArgs
    {
        public ErrorOccurredEventArgs(string title, string message) : this(title, message, null)
        {
        }

        public string Title => title;
        public string Message => message;
        public Exception? Exception => ex;
    }
}