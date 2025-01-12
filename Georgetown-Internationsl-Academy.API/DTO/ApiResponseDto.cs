namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class ApiResponseDto<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public string? ErrorCode { get; set; } 

        public static ApiResponseDto<T> CreateSuccess(T data, string message = "Operation successful.")
        {
            return new ApiResponseDto<T> { Success = true, Message = message, Data = data };
        }

        public static ApiResponseDto<T> CreateFailure(string message, string? errorCode = null)
        {
            return new ApiResponseDto<T> { Success = false, Message = message, ErrorCode = errorCode };
        }
    }
}
