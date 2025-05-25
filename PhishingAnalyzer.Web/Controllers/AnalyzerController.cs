using Microsoft.AspNetCore.Mvc;
using PhishingAnalyzer.Web.Models;
using PhishingAnalyzer.Web.Services;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace PhishingAnalyzer.Web.Controllers
{
    public class AnalyzerController : Controller
    {
        private readonly IAnalysisService _analysisService;
        private readonly ILogger<AnalyzerController> _logger;

        public AnalyzerController(IAnalysisService analysisService, ILogger<AnalyzerController> logger)
        {
            _analysisService = analysisService;
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

                return View("Result", result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing URL: {Url}", url);
                return View("Error", $"Analysis failed: {ex.Message}");
            }
        }
    }
} 