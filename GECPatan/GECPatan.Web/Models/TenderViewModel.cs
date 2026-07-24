using GECPatan.Web.Models.Dtos;

namespace GECPatan.Web.Models
{
    public class TenderViewModel
    {
        public List<TenderGroupDTO> Groups { get; set; } = new();
        public string? ApiBaseUrl { get; set; }
    }
}
