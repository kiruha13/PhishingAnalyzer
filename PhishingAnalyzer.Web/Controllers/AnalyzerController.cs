using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using PhishingAnalyzer.Web.Models;
using PhishingAnalyzer.Web.Services;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace PhishingAnalyzer.Web.Controllers
{
    [Authorize]
    public class AnalyzerController : Controller
    {
        private readonly IAnalysisService _analysisService;
        private readonly IAnalysisHistoryService _historyService;
        private readonly ILogger<AnalyzerController> _logger;

        public AnalyzerController(
            IAnalysisService analysisService,
            IAnalysisHistoryService historyService,
            ILogger<AnalyzerController> logger)
        {
            _analysisService = analysisService;
            _historyService = historyService;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Analyze(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                _logger.LogWarning("Empty URL provided");
                return BadRequest("URL is required");
            }

            try
            {
                // Ensure URL has proper format
                if (!url.StartsWith("http://") && !url.StartsWith("https://"))
                {
                    url = "https://" + url;
                }

                _logger.LogInformation("Analyzing URL: {Url}", url);
                var result = await _analysisService.AnalyzeUrlAsync(url);
                
                if (result == null)
                {
                    _logger.LogWarning("Analysis returned null result for URL: {Url}", url);
                    return View("Error", "Analysis failed to return results");
                }

                // Save to history
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                await _historyService.SaveAnalysisAsync(
                    url,
                    userId!,
                    result.ScreenshotPath,
                    result.ToString(),
                    result.RiskScore,
                    result.IsPhishing
                );

                return View("Result", result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing URL: {Url}", url);
                return View("Error", $"Analysis failed: {ex.Message}");
            }
        }

        public async Task<IActionResult> History()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var history = await _historyService.GetUserHistoryAsync(userId!);
            return View(history);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteHistory(int id)
        {
            await _historyService.DeleteAnalysisAsync(id);
            return RedirectToAction(nameof(History));
        }
    }
} 