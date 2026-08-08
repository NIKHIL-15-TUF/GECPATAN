using GECPatan.Web.Models;
using GECPatan.Web.Models.Dtos;
using GECPatan.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace GECPatan.Web.Controllers
{
    public class ContactUsController : Controller
    {
        private readonly IContactApiService _contact;
        private readonly ILogger<ContactUsController> _logger;

        public ContactUsController(IContactApiService contact, ILogger<ContactUsController> logger)
        {
            _contact = contact;
            _logger = logger;
        }

        // GET /ContactUs
        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var settings = await _contact.GetSettingsAsync(ct) ?? new();

            var vm = new ContactUsPageViewModel
            {
                Settings = settings
            };

            ViewBag.Title = "Contact Us";
            return View(vm);
        }

        // POST /ContactUs/Submit — AJAX only, returns JSON.
        // The form is submitted with a standard multipart/form-urlencoded POST
        // (via FormData in JS, not a JSON fetch body) purely so the standard
        // MVC antiforgery token + default model binding both work without any
        // extra header wiring — see Index.cshtml's submit handler.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(ContactFormVM form, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                var firstError = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .FirstOrDefault();

                return Json(new ContactSubmitResultVM
                {
                    Success = false,
                    Message = firstError ?? "Please check the form and try again."
                });
            }

            var request = new ContactSubmitRequestDTO
            {
                FullName = form.FullName.Trim(),
                Email = form.Email.Trim(),
                Phone = string.IsNullOrWhiteSpace(form.Phone) ? null : form.Phone.Trim(),
                Category = form.Category.Trim(),
                Subject = form.Subject.Trim(),
                Message = form.Message.Trim()
            };

            var (success, message) = await _contact.SubmitAsync(request, ct);

            if (!success)
            {
                _logger.LogWarning("Contact Us submission failed for {Email}: {Message}", request.Email, message);
            }

            return Json(new ContactSubmitResultVM { Success = success, Message = message });
        }
    }
}
