namespace MeetUpPlaner.Services
{
    public class OperationResult
    {
        public bool Succeeded { get; set; }
        public IEnumerable<string> Errors { get; set; } = Enumerable.Empty<string>();

        public static OperationResult Success() => new OperationResult { Succeeded = true };

        public static OperationResult Failure(params string[] errors) =>
            new OperationResult { Succeeded = false, Errors = errors ?? Enumerable.Empty<string>() };
    }

    public class OperationResult<T> : OperationResult
    {
        public T? Payload { get; private set; }

        public static OperationResult<T> Success(T payload) =>
            new OperationResult<T> { Succeeded = true, Payload = payload };

        public new static OperationResult<T> Failure(params string[] errors) =>
            new OperationResult<T> { Succeeded = false, Errors = errors ?? Enumerable.Empty<string>() };
    }
}