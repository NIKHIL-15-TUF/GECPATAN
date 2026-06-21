using GECPatan.Core.Data;
using GECPatan.Core.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace GECPatan.Admin.Data
{
    public static class DisclosureNarrativeSeeder
    {
        // Call this once at app startup (Program.cs)
        // Ensures all 15 strict-schema rows exist.
        // Safe to call repeatedly - only inserts missing keys.
        public static async Task SeedAsync(ApplicationDbContext context)
        {
            var existingKeys = await context.DisclosureNarratives
                .Select(n => n.SectionKey)
                .ToListAsync();

            int order = 0;
            foreach (var key in DisclosureSectionKeys.AllKeys)
            {
                if (!existingKeys.Contains(key))
                {
                    context.DisclosureNarratives.Add(new DisclosureNarrative
                    {
                        SectionKey = key,
                        SectionTitle = DisclosureSectionKeys.DefaultTitles[key],
                        HtmlContent = null,
                        DisplayOrder = order,
                        IsVisible = true
                    });
                }
                order++;
            }

            await context.SaveChangesAsync();
        }
    }
}