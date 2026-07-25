using GECPatan.Web.Models;
using GECPatan.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace GECPatan.Web.Controllers
{
    public class CommitteeController : Controller
    {
        private readonly ICommitteeApiService _api;
        private readonly IConfiguration _configuration;

        public CommitteeController(ICommitteeApiService api, IConfiguration configuration)
        {
            _api = api;
            _configuration = configuration;
        }

        // GET /Committee/7
        [HttpGet("Committee/{id:int}")]
        public async Task<IActionResult> Index(int id, CancellationToken ct)
        {
            var committee = await _api.GetCommitteeDetailAsync(id, ct);
            if (committee is null)
            {
                return NotFound();
            }

            var activities = await _api.GetActivitiesAsync(id, ct);

            var vm = new CommitteeViewModel
            {
                ApiBaseUrl = _configuration["Api:BaseUrl"]?.TrimEnd('/') ?? string.Empty,
                Id = committee.Id,
                Title = committee.Title,
                About = committee.About,
                Tagline = committee.Tagline,
                Measures = committee.Measures,
                Message = committee.Message,
                TitleImagePath = committee.TitleImagePath,
                TitleImageCSSClass = committee.TitleImageCSSClass,
                MeasureImagePath = committee.MeasureImagePath,
                SubObjImagePath = committee.SubObjImagePath,
                BulletPointsImagePath = committee.BulletPointsImagePath,
                PageFlyerPath = committee.PageFlyerPath,
                BlogLink = committee.BlogLink,
                Link = committee.Link,
                Account = committee.Account,
                NationalTaskForce = committee.NationalTaskForce,
                ShowDocument = committee.ShowDocument,
                TableView = committee.TableView,

                TabAbout = string.IsNullOrWhiteSpace(committee.TabAbout) ? "About" : committee.TabAbout,
                TabVisionMission = string.IsNullOrWhiteSpace(committee.TabVisionMission) ? "Vision & Mission" : committee.TabVisionMission,
                TabObjectives = string.IsNullOrWhiteSpace(committee.TabObjectives) ? "Objectives" : committee.TabObjectives,
                TabMembers = string.IsNullOrWhiteSpace(committee.TabMembers) ? "Members" : committee.TabMembers,
                TabActivities = string.IsNullOrWhiteSpace(committee.TabActivities) ? "Activities" : committee.TabActivities,
                TabDocuments = string.IsNullOrWhiteSpace(committee.TabDocuments) ? "Documents" : committee.TabDocuments,
                TabLink = string.IsNullOrWhiteSpace(committee.TabLink) ? "Link" : committee.TabLink,

                Vision = committee.Visions,
                Mission = committee.Missions,
                Objectives = committee.Objectives,
                SubObjectives = committee.SubObjectives,
                Members = committee.Members,
                AdditionalMemberGroups = committee.AdditionalMemberGroups,
                Activities = activities,
                DynamicSections = committee.DynamicSections
            };

            return View(vm);
        }
    }
}