using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SFA.DAS.ApplyService.Domain.Apply;
using SFA.DAS.ApplyService.Web.Infrastructure;

namespace SFA.DAS.ApplyService.Web.Controllers
{
    [Authorize]
    public class ApplicationController : Controller
    {
        private readonly ApplicationApiClient _apiClient;

        public ApplicationController(ApplicationApiClient apiClient)
        {
            _apiClient = apiClient;
        }
        
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("/Application/{applicationId}/Pages/{pageId}")]
        public async Task<IActionResult> Page(string applicationId, string pageId)
        {
            var page = await _apiClient.GetPage(Guid.Parse(applicationId), pageId, Guid.Parse(User.FindFirstValue("UserId")));
            var pageVm = new PageViewModel(page);   
            
            return View("~/Views/Application/Pages/Index.cshtml", pageVm);
        }
        
        [HttpPost("/Application/{applicationId}/Pages/{pageId}")]
        public async Task<IActionResult> SaveAnswers(string applicationId, string pageId)
        {
            var userId = "1";

            var answers = new List<Answer>();

            foreach (var keyValuePair in HttpContext.Request.Form.Where(f => !f.Key.StartsWith("__")))
            {
                answers.Add(new Answer() {QuestionId = keyValuePair.Key, Value = keyValuePair.Value});
            }

            var updatePageResult = await _apiClient.UpdatePageAnswers(Guid.Parse(applicationId), Guid.Parse(User.FindFirstValue("UserId")), pageId, answers);

            if (updatePageResult.ValidationPassed)
            {
                var nextActions = updatePageResult.Page.Next;

                if (nextActions.Count == 1)
                {
                    var pageNext = nextActions[0];
                    if (pageNext.Action == "NextPage")
                    {
                        return RedirectToAction("Page", new {applicationId, pageId = pageNext.ReturnId});
                    }
                    
                    return pageNext.Action == "ReturnToSequence"
                        ? RedirectToAction("Sequence", "Sequence", new {sequenceId = pageNext.ReturnId})
                        : RedirectToAction("Index", "Sequence");
                }
                else
                {
                    foreach (var nextAction in nextActions)
                    {
                        if (nextAction.Condition.MustEqual == answers.Single(a => a.QuestionId == nextAction.Condition.QuestionId).Value)
                        {
                            return RedirectToAction("Index", new {pageId = nextAction.ReturnId});
                        }
                    }
                    return RedirectToAction("Index", "Sequence");
                }
            }
            else
            {
                foreach (var error in updatePageResult.ValidationErrors)
                {
                    ModelState.AddModelError(error.Key, error.Value);
                }
            }
            
            var pageVm = new PageViewModel(updatePageResult.Page);
            return View("~/Views/Application/Pages/Index.cshtml", pageVm);
        }
    }
}