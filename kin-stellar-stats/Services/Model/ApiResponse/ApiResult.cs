namespace Kin.Horizon.Api.Poller.Services.Model.ApiResponse {
    public class ApiResult
    {
        public bool Success { get; }

        public ApiResult(bool success)
        {
            Success = success;
        }
    }
}