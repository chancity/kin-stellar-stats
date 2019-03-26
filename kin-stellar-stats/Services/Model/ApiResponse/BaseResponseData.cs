namespace Kin.Horizon.Api.Poller.Services.Model.ApiResponse {
    public class BaseResponseData<T> : BaseResponse where T : class  
    {
        public T Data { get; }

        public BaseResponseData(T data)
        {
            Data = data;
        }
    }
}