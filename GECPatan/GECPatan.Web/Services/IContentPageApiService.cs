using System.Text.Json;
using GECPatan.Web.Models.ViewModels;

namespace GECPatan.Web.Services
{
    public interface IContentPageApiService
    {
        Task<ContentPageDetailsVM?> GetBySlugAsync(string slug);
    }

    public class ContentPageApiService : IContentPageApiService
    {
        private readonly HttpClient _http;
        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public ContentPageApiService(HttpClient http)
        {
            _http = http; // BaseAddress set via AddHttpClient<> in Program.cs (Api:BaseUrl)
        }

        public async Task<ContentPageDetailsVM?> GetBySlugAsync(string slug)
        {
            var response = await _http.GetAsync($"api/pages/{slug}");
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            var wrapper = JsonSerializer.Deserialize<ApiResponseDTO<ContentPageApiDTO>>(json, JsonOpts);

            if (wrapper == null || !wrapper.Success || wrapper.Data == null) return null;

            var dto = wrapper.Data;

            return new ContentPageDetailsVM
            {
                Id = dto.Id,
                Title = dto.Title,
                Slug = dto.Slug,
                HtmlContent = dto.HtmlContent,
                Images = dto.Images
                    .OrderBy(i => i.DisplayOrder)
                    .Select(i => new ContentPageImageVM
                    {
                        ImageUrl = ResolveApiFileUrl(i.ImageUrl),
                        Caption = i.Caption,
                        DisplayOrder = i.DisplayOrder
                    }).ToList(),
                DynamicSections = dto.DynamicSections
                    .OrderBy(s => s.DisplayOrder)
                    .Select(s => new ContentPageDynamicSectionVM
                    {
                        Id = s.Id,
                        Title = s.Title,
                        SectionType = s.SectionType,
                        HtmlContent = s.HtmlContent,
                        FilePath = string.IsNullOrEmpty(s.FilePath) ? null : ResolveApiFileUrl(s.FilePath),
                        FileName = s.FileName,
                        DisplayOrder = s.DisplayOrder,
                        Files = s.Files
                            .OrderBy(f => f.DisplayOrder)
                            .Select(f => new ContentPageDynamicSectionFileVM
                            {
                                FilePath = ResolveApiFileUrl(f.FilePath),
                                Title = f.Title,
                                FileType = f.FileType,
                                DisplayOrder = f.DisplayOrder
                            }).ToList()
                    }).ToList()
            };
        }

        // ASSUMPTION: placeholder — replace with your real ResolveApiFileUrl()
        // helper if one already exists elsewhere in GECPatan.Web.
        private string ResolveApiFileUrl(string path)
        {
            if (string.IsNullOrEmpty(path)) return path;
            if (path.StartsWith("http://") || path.StartsWith("https://")) return path;
            var baseUrl = _http.BaseAddress?.ToString().TrimEnd('/') ?? string.Empty;
            return $"{baseUrl}/{path.TrimStart('/')}";
        }
    }

    // ASSUMPTION: if you already have a shared ApiResponse<T> / DTO set,
    // delete these and reference those instead to avoid duplicates.
    public class ApiResponseDTO<T>
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }
    }

    public class ContentPageApiDTO
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? HtmlContent { get; set; }
        public List<ContentPageImageApiDTO> Images { get; set; } = new();
        public List<DynamicSectionApiDTO> DynamicSections { get; set; } = new();
    }

    public class ContentPageImageApiDTO
    {
        public string ImageUrl { get; set; } = string.Empty;
        public string? Caption { get; set; }
        public int DisplayOrder { get; set; }
    }

    public class DynamicSectionApiDTO
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string SectionType { get; set; } = string.Empty;
        public string? HtmlContent { get; set; }
        public string? FilePath { get; set; }
        public string? FileName { get; set; }
        public int DisplayOrder { get; set; }
        public List<DynamicSectionFileApiDTO> Files { get; set; } = new();
    }

    public class DynamicSectionFileApiDTO
    {
        public string FilePath { get; set; } = string.Empty;
        public string? Title { get; set; }
        public string? FileType { get; set; }
        public int DisplayOrder { get; set; }
    }
}