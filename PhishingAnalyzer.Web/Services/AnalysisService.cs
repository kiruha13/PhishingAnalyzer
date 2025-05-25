using System;
using System.Threading.Tasks;
using PhishingAnalyzer.Core.Services;
using PhishingAnalyzer.Web.Models;
using Microsoft.Extensions.Logging;
using Microsoft.ML;

namespace PhishingAnalyzer.Web.Services
{
    public class AnalysisService : IAnalysisService
    {
        private readonly WebsiteAnalyzer _analyzer;
        private readonly ILogger<AnalysisService> _logger;

        public AnalysisService(ILogger<AnalysisService> logger)
        {
            _logger = logger;
            try
            {
                var mlContext = new MLContext();
                // Look for model.zip in the root directory
                var modelPath = Path.Combine(Directory.GetCurrentDirectory(), "..","PhishingAnalyzer.ML", "phishing_model.zip");
                if (!File.Exists(modelPath))
                {
                    // Try alternative path
                    modelPath = Path.Combine(Directory.GetCurrentDirectory(),"phishing_model.zip");
                    if (!File.Exists(modelPath))
                    {
                        _logger.LogWarning("model.zip not found in any expected location, falling back to core analysis only");
                        _analyzer = new WebsiteAnalyzer();
                        return;
                    }
                }

                _analyzer = new WebsiteAnalyzer(modelPath);
                _logger.LogInformation("WebsiteAnalyzer initialized with model.zip from: {ModelPath}", modelPath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to initialize with ML model, falling back to core analysis only");
                _analyzer = new WebsiteAnalyzer();
            }
        }

        public async Task<AnalysisResult> AnalyzeUrlAsync(string url)
        {
            _logger.LogInformation("Starting analysis for URL: {Url}", url);
            
            try
            {
                var coreResult = await _analyzer.AnalyzeWebsiteAsync(url);
                _logger.LogInformation("Analysis completed for URL: {Url}, Risk Score: {RiskScore}, Risk Level: {RiskLevel}", 
                    url, coreResult.RiskScore, coreResult.RiskLevel);

                var result = new AnalysisResult
                {
                    Url = coreResult.Url,
                    RiskScore = (int)coreResult.RiskScore,
                    RiskLevel = coreResult.RiskLevel,
                    HasHttps = coreResult.IsSecure,
                    JavaScriptErrors = coreResult.JavaScriptErrors.Count,
                    Warnings = coreResult.Warnings.Count,
                    SuspiciousPatterns = coreResult.SuspiciousPatterns,
                    ScreenshotPath = coreResult.ScreenshotPath,
                    AnalysisDate = coreResult.AnalysisTime
                };

                // Add ML model results if available
                if (coreResult.AdditionalData.ContainsKey("MLPrediction"))
                {
                    var prediction = coreResult.AdditionalData["MLPrediction"];
                    result.MLPrediction = ((dynamic)prediction).Label;
                    result.MLProbability = ((dynamic)prediction).Probability;
                }

                // Add certificate information if available
                if (coreResult.AdditionalData.ContainsKey("CertificateInfo"))
                {
                    var certInfo = coreResult.AdditionalData["CertificateInfo"];
                    result.IsCertificateValid = ((dynamic)certInfo).IsValid;
                    result.CertificateSubject = ((dynamic)certInfo).Subject;
                    result.CertificateIssuer = ((dynamic)certInfo).Issuer;
                    result.CertificateValidFrom = ((dynamic)certInfo).ValidFrom;
                    result.CertificateValidTo = ((dynamic)certInfo).ValidTo;
                    result.CertificateThumbprint = ((dynamic)certInfo).Thumbprint;
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing URL: {Url}", url);
                throw;
            }
        }
    }
} 