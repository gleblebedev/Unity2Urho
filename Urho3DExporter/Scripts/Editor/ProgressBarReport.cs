namespace Urho3DExporter
{
    public struct ProgressBarReport
    {
        public string Message;

        public ProgressBarReport(string message)
        {
            Message = message;
        }

        public override string ToString()
        {
            return Message ?? base.ToString();
        }
    }
}