namespace Smartera.BLL.Dtos.Common
{
    public class RequestFilter
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchValue { get; set; }
        public string? SortColumn { get; set; }
        public string? SortDirection { get; set; } = "asc";
        public int? CategoryId { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public double? MinRating { get; set; }
        public double? MaxRating { get; set; }
        public string[]? Statuses { get; set; }
        public DateOnly? DateFilter { get; set; }
    }
}
