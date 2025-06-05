using PhishingAnalyzer.Web.Models;

namespace PhishingAnalyzer.Web.Services;

public interface IAnalysisHistoryService
{
    Task SaveAnalysisAsync(string url, string userId, string? screenshotPath, string? analysisResult, double riskScore, bool isPhishing);
    Task<List<AnalysisHistory>> GetUserHistoryAsync(string userId);
    Task<AnalysisHistory?> GetAnalysisByIdAsync(int id);
    Task DeleteAnalysisAsync(int id);
} 