using Microsoft.OpenApi.Any;

namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class PagedResultDto<T>
    {
        public int TotalRecords { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public List<T> Data { get; set; } = new();
    }
}
