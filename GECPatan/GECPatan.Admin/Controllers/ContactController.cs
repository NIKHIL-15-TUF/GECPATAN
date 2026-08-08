using GECPatan.Core.Data;
using GECPatan.Core.Models.Domain;
using GECPatan.Admin.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GECPatan.Admin.Controllers
{
    [Authorize(Roles = "SuperAdmin,ContentEditor")]
    public class ContactController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ContactController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Contact Information";

            var settings = await _context.SiteSettings
                .Where(s => s.Group == "Contact")
                .ToDictionaryAsync(s => s.Key, s => s.Value);

            var vm = new ContactVM
            {
                CollegeName = settings.GetValueOrDefault("Contact_CollegeName"),
                Address = settings.GetValueOrDefault("Contact_Address"),
                Phone1 = settings.GetValueOrDefault("Contact_Phone1"),
                Phone2 = settings.GetValueOrDefault("Contact_Phone2"),
                Email1 = settings.GetValueOrDefault("Contact_Email1"),
                Email2 = settings.GetValueOrDefault("Contact_Email2"),
                OfficeHours = settings.GetValueOrDefault("Contact_OfficeHours"),
                MapEmbedUrl = settings.GetValueOrDefault("Contact_MapEmbedUrl"),

                SupportEmail = settings.GetValueOrDefault("Contact_SupportEmail"),
                SenderEmail = settings.GetValueOrDefault("Contact_SenderEmail"),

                SuccessMessage = settings.GetValueOrDefault("Contact_SuccessMessage"),
                FailureMessage = settings.GetValueOrDefault("Contact_FailureMessage"),
                AllowedCategories = settings.GetValueOrDefault("Contact_AllowedCategories"),

                FacebookUrl = settings.GetValueOrDefault("Contact_Facebook"),
                TwitterUrl = settings.GetValueOrDefault("Contact_Twitter"),
                YoutubeUrl = settings.GetValueOrDefault("Contact_Youtube"),
                LinkedinUrl = settings.GetValueOrDefault("Contact_Linkedin"),
                InstagramUrl = settings.GetValueOrDefault("Contact_Instagram")
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(ContactVM model)
        {
            ViewData["Title"] = "Contact Information";
            if (!ModelState.IsValid) return View(model);

            await UpsertSetting("Contact_CollegeName", model.CollegeName);
            await UpsertSetting("Contact_Address", model.Address);
            await UpsertSetting("Contact_Phone1", model.Phone1);
            await UpsertSetting("Contact_Phone2", model.Phone2);
            await UpsertSetting("Contact_Email1", model.Email1);
            await UpsertSetting("Contact_Email2", model.Email2);
            await UpsertSetting("Contact_OfficeHours", model.OfficeHours);
            await UpsertSetting("Contact_MapEmbedUrl", model.MapEmbedUrl);

            await UpsertSetting("Contact_SupportEmail", model.SupportEmail);
            await UpsertSetting("Contact_SenderEmail", model.SenderEmail);

            await UpsertSetting("Contact_SuccessMessage", model.SuccessMessage);
            await UpsertSetting("Contact_FailureMessage", model.FailureMessage);
            await UpsertSetting("Contact_AllowedCategories", model.AllowedCategories);

            await UpsertSetting("Contact_Facebook", model.FacebookUrl);
            await UpsertSetting("Contact_Twitter", model.TwitterUrl);
            await UpsertSetting("Contact_Youtube", model.YoutubeUrl);
            await UpsertSetting("Contact_Linkedin", model.LinkedinUrl);
            await UpsertSetting("Contact_Instagram", model.InstagramUrl);

            await _context.SaveChangesAsync();
            TempData["Success"] = "Contact information updated.";
            return RedirectToAction(nameof(Index));
        }

        private async Task UpsertSetting(string key, string? value)
        {
            var s = await _context.SiteSettings.FirstOrDefaultAsync(x => x.Key == key);
            if (s == null)
                _context.SiteSettings.Add(new SiteSetting { Key = key, Value = value, Group = "Contact" });
            else
                s.Value = value;
        }
    }
}