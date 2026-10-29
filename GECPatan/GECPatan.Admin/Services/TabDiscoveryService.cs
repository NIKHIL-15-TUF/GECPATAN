using System.Net;
using System.Text.RegularExpressions;
using GECPatan.Admin.Models;
using Microsoft.Extensions.Caching.Memory;

namespace GECPatan.Admin.Services
{
    /// <summary>Tabs = null means the page could not be read; Error says why.</summary>
    public record TabDiscoveryResult(IReadOnlyList<TabOption>? Tabs, string? Error = null);

    public interface ITabDiscoveryService
    {
        /// <summary>
        /// Reads the tabs a public page really shows, straight from its live HTML.
        /// Returns null if the page could not be read (not configured, down, 404),
        /// an empty list if the page loaded but has no tabs.
        /// </summary>
        Task<TabDiscoveryResult> DiscoverAsync(string path, CancellationToken ct = default);
    }

    public class TabDiscoveryService : ITabDiscoveryService
    {
        private static readonly Regex ButtonRx = new(
            @"<button\b(?<attrs>[^>]*)>(?<inner>.*?)</button>",
            RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex IsTabRx = new(
            @"class\s*=\s*""[^""]*\btablinks\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // openTab(event, 'Labs')   -- Department, Committee, Club, Facility pages
        private static readonly Regex OpenTabRx = new(
            @"openTab\s*\(\s*[A-Za-z_]+\s*,\s*['""](?<id>[^'""]+)['""]",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // data-tab="2024"          -- Achievements year tabs
        private static readonly Regex DataTabRx = new(
            @"data-tab\s*=\s*""(?<id>[^""]+)""", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex TagRx = new("<[^>]+>", RegexOptions.Compiled);
        private static readonly Regex SpaceRx = new(@"\s+", RegexOptions.Compiled);

        private static readonly TimeSpan CacheFor = TimeSpan.FromSeconds(30);

        private readonly IHttpClientFactory _http;
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _config;
        private readonly ILogger<TabDiscoveryService> _logger;

        public TabDiscoveryService(IHttpClientFactory http, IMemoryCache cache,
            IConfiguration config, ILogger<TabDiscoveryService> logger)
        {
            _http = http; _cache = cache; _config = config; _logger = logger;
        }

        public async Task<TabDiscoveryResult> DiscoverAsync(string path, CancellationToken ct = default)
        {
            var baseUrl = _config["WebSite:BaseUrl"];
            if (string.IsNullOrWhiteSpace(baseUrl))
                return new(null, "WebSite:BaseUrl is not set in Admin appsettings");
            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri) ||
                (baseUri.Scheme != Uri.UriSchemeHttp && baseUri.Scheme != Uri.UriSchemeHttps))
                return new(null, "WebSite:BaseUrl must be a full URL like https://localhost:7120");

            var url = baseUri.GetLeftPart(UriPartial.Authority) + path;
            if (_cache.TryGetValue(url, out IReadOnlyList<TabOption>? cached) && cached != null)
                return new(cached);

            try
            {
                var client = _http.CreateClient("WebSite");
                using var res = await client.GetAsync(url, ct);
                if (!res.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Tab discovery: {Url} returned {Status}", url, (int)res.StatusCode);
                    return new(null, $"{path} returned HTTP {(int)res.StatusCode}");
                }

                var tabs = Parse(await res.Content.ReadAsStringAsync(ct));
                _cache.Set(url, tabs, CacheFor);
                return new(tabs);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Tab discovery: could not read {Url}", url);
                var why = ex.InnerException?.Message ?? ex.Message;
                return new(null, $"could not reach {baseUri.GetLeftPart(UriPartial.Authority)} ({why})");
            }
        }

        private static IReadOnlyList<TabOption> Parse(string html)
        {
            var result = new List<TabOption>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (Match m in ButtonRx.Matches(html))
            {
                var attrs = WebUtility.HtmlDecode(m.Groups["attrs"].Value);
                if (!IsTabRx.IsMatch(attrs)) continue;

                var id = OpenTabRx.Match(attrs) is { Success: true } o ? o.Groups["id"].Value
                       : DataTabRx.Match(attrs) is { Success: true } d ? d.Groups["id"].Value
                       : null;
                if (string.IsNullOrWhiteSpace(id) || !seen.Add(id)) continue; // desktop + mobile bars repeat each tab

                var label = SpaceRx.Replace(WebUtility.HtmlDecode(TagRx.Replace(m.Groups["inner"].Value, "")), " ").Trim();
                result.Add(new TabOption(id, string.IsNullOrEmpty(label) ? id : label));
            }
            return result;
        }
    }
}