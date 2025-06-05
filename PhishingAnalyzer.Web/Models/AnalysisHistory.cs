using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhishingAnalyzer.Web.Models;

public class AnalysisHistory
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Url { get; set; } = string.Empty;

    [Required]
    public string UserId { get; set; } = string.Empty;

    [ForeignKey("UserId")]
    public ApplicationUser? User { get; set; }

    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;

    public string? ScreenshotPath { get; set; }

    public string? AnalysisResult { get; set; }

    public double RiskScore { get; set; }

    public bool IsPhishing { get; set; }
} 