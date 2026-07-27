using GECPatan.Web.Models;
using GECPatan.Web.Models.Dtos;

namespace GECPatan.Web.Models
{
	public class MenuViewModel
	{
		public string ApiBaseUrl { get; set; } = string.Empty;
		public List<MenuItemDTO> Items { get; set; } = new();
	}
}