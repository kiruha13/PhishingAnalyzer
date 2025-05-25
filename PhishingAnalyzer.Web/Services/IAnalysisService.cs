using System.Threading.Tasks;
using PhishingAnalyzer.Web.Models;

namespace PhishingAnalyzer.Web.Services
{
    public interface IAnalysisService
    {
        Task<AnalysisResult> AnalyzeUrlAsync(string url);
    }
} 