namespace Nomaro.API.DTO
{
    public class PagingRequestDto
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        public string? SortBy { get; set; } = "AppliedOn"; // AppliedOn, FromDate, ToDate
        public string? SortOrder { get; set; } = "DESC";
    }
}

