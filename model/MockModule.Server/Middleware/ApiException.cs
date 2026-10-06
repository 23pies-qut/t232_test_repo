using MockModule.Server.Models;

namespace MockModule.Server.Middleware
{
    /// <summary>
    /// An expected error (invalid input, not a member, not found ...). Thrown by services and turned into JSON by ErrorHandlingMiddleware.
    /// When Trace is set (failed upload), the frontend can show the step at which it failed.
    /// </summary>
    public class ApiException : Exception
    {
        public int StatusCode { get; }
        public string Code { get; }
        public IReadOnlyList<Step>? Trace { get; }

        public ApiException(int statusCode, string code, string message, IReadOnlyList<Step>? trace = null) : base(message)
        {
            StatusCode = statusCode;
            Code = code;
            Trace = trace;
        }
    }
}
