using GECPatan.Web.Models.Dtos;

namespace GECPatan.Web.Models
{
    public class NewsLetterViewModel
    {
        public string ApiBaseUrl { get; set; } = string.Empty;
        public List<NewsLetterDTO> Letters { get; set; } = new();
       // public bool TableView { get; set; } = false;
    }
}
