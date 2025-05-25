using System;
using System.Collections.Generic;

namespace PhishingAnalyzer.Core.Models
{
    public class AnalysisResult
    {
        public required string Url { get; set; }
        public DateTime AnalysisTime { get; set; }
        public bool IsSecure { get; set; }
        public List<string> JavaScriptErrors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
        public List<string> SuspiciousPatterns { get; set; } = new List<string>();
        public string? ScreenshotPath { get; set; }
        public double RiskScore { get; set; }
        public required string RiskLevel { get; set; }
        public Dictionary<string, object> AdditionalData { get; set; } = new Dictionary<string, object>();
    }
} 