namespace LFramework.Runtime
{
    public readonly struct NetworkRequestFailure
    {
        public NetworkRequestFailure(int code, string message = null, object userData = null)
        {
            Code = code;
            Message = message;
            UserData = userData;
        }

        public int Code { get; }

        public string Message { get; }

        public object UserData { get; }

        public override string ToString()
        {
            return string.IsNullOrEmpty(Message) ? Code.ToString() : $"{Code}:{Message}";
        }
    }
}
