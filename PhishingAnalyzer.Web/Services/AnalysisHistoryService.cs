using Microsoft.EntityFrameworkCore;
using PhishingAnalyzer.Web.Data;
using PhishingAnalyzer.Web.Models;

namespace PhishingAnalyzer.Web.Services;

public class AnalysisHistoryService : IAnalysisHistoryService
{
    private readonly ApplicationDbContext _context;

    public AnalysisHistoryService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task SaveAnalysisAsync(string url, string userId, string? screenshotPath, string? analysisResult, double riskScore, bool isPhishing)
    {
        var history = new AnalysisHistory
        {
            Url = url,
            UserId = userId,
            ScreenshotPath = screenshotPath,
            AnalysisResult = analysisResult,
            RiskScore = riskScore,
            IsPhishing = isPhishing,
            AnalyzedAt = DateTime.UtcNow
        };

        _context.AnalysisHistory.Add(history);
        await _context.SaveChangesAsync();
    }

    public async Task<List<AnalysisHistory>> GetUserHistoryAsync(string userId)
    {
        return await _context.AnalysisHistory
            .Where(h => h.UserId == userId)
            .OrderByDescending(h => h.AnalyzedAt)
            .ToListAsync();
    }

    public async Task<AnalysisHistory?> GetAnalysisByIdAsync(int id)
    {
        return await _context.AnalysisHistory.FindAsync(id);
    }

    public async Task DeleteAnalysisAsync(int id)
    {
        var analysis = await _context.AnalysisHistory.FindAsync(id);
        if (analysis != null)
        {
            _context.AnalysisHistory.Remove(analysis);
            await _context.SaveChangesAsync();
        }
    }
} 