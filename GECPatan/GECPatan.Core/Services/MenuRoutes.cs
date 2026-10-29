using System;
using System.Collections.Generic;

namespace GECPatan.Core.Services
{
    /// <summary>
    /// The one place that turns a menu item into a public URL path.
    /// Used by the API (to build menu links) and by Admin (to read a page's
    /// tabs), so both always agree.
    /// </summary>
    public static class MenuRoutes
    {
        // Pages whose real URL is NOT "/{Controller}/{Action}" (attribute routes).
        private static readonly Dictionary<string, string> Special =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["Achievement/Index"] = "/Achievements",
                ["Department/List"] = "/Department"
            };

        public static string InternalPath(string controller, string action)
        {
            var key = $"{controller}/{action}";
            return Special.TryGetValue(key, out var p) ? p : $"/{key}";
        }

        // Dynamic pages that live at /{Type}/{id}. Null = unknown type.
        public static string? DynamicPath(string? type, int id) => type switch
        {
            "Department" => $"/Department/{id}",
            "Committee" => $"/Committee/{id}",
            "Facility" => $"/Facilities/{id}",
            "Club" => $"/Clubs/{id}",
            "Document" => $"/documents/{id}",
            _ => null
        };
    }
}
