namespace Kin.Horizon.Api.Poller.Services.Model.ApiResponse {
    public class ApiError
    {
        public string Message { get; }

        public string Stacktrace { get; }

        protected ApiError() { }

        public ApiError(string message, string stacktrace)
        {
            Message = message;
            Stacktrace = stacktrace;
        }
    }
}