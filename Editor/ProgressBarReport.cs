namespace UnityToCustomEngineExporter.Editor
{
    public struct ProgressBarReport
    {
        public string Message;

        public ProgressBarReport(string message)
        {
            Message = message;
        }

        public static implicit operator ProgressBarReport(string message)
        {
            return new ProgressBarReport(message);
        }

        public override string ToString()
        {
            return Message ?? base.ToString();
        }
    }
}