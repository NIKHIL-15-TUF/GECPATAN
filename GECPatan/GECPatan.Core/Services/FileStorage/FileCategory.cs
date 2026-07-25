namespace GECPatan.Core.Services.FileStorage
{
    /// <summary>
    /// Every upload must declare which category it belongs to. The category
    /// drives which extensions/content-types/magic-bytes are accepted and
    /// what the maximum file size is. Add new categories here rather than
    /// loosening an existing one, so validation rules stay auditable in one place.
    /// </summary>
    public enum FileCategory
    {
        /// <summary>Photos: gallery images, profile photos, banners, sliders.</summary>
        Image,

        /// <summary>Office/PDF documents: notices, disclosures, tenders, timetables.</summary>
        Document
    }
}
