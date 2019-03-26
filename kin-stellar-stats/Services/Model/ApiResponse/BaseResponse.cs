namespace Kin.Horizon.Api.Poller.Services.Model.ApiResponse
{
    public class BaseResponse
    {
        public ApiError ApiError { get; }


        protected BaseResponse() { }

        public BaseResponse(ApiError apiError)
        {
            ApiError = apiError;
        }
    }
}