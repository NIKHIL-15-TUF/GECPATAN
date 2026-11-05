using GECPatan.Web.Models.Dtos;

namespace GECPatan.Web.Models
{
    public class ActivityListViewModel
    {
        public List<ActivityDTO> Activities { get; set; } = new();

        public List<int> Years { get; set; } = new();

        public int? SelectedYear { get; set; }

        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 20;

        public int Total { get; set; }

        public int TotalPages =>
            (int)Math.Ceiling((double)Total / PageSize);

        public string ApiBaseUrl { get; set; } = string.Empty;
    }

    public class ActivityDetailViewModel
    {
        public ActivityDTO Activity { get; set; } = new();

        public string ApiBaseUrl { get; set; } = string.Empty;
    }
}