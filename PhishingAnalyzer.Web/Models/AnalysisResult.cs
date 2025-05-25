using System;
using System.Collections.Generic;

namespace PhishingAnalyzer.Web.Models
{
    public class AnalysisResult
    {
        public required string Url { get; set; }
        public int RiskScore { get; set; }
        public required string RiskLevel { get; set; }
        public bool HasHttps { get; set; }
        public int JavaScriptErrors { get; set; }
        public int Warnings { get; set; }
        public List<string> SuspiciousPatterns { get; set; } = new List<string>();
        public string? ScreenshotPath { get; set; }
        public DateTime AnalysisDate { get; set; }
        
        // ML Model Results
        public string? MLPrediction { get; set; }
        public float MLProbability { get; set; }

        // Certificate Information
        public bool IsCertificateValid { get; set; }
        public string? CertificateSubject { get; set; }
        public string? CertificateIssuer { get; set; }
        public DateTime? CertificateValidFrom { get; set; }
        public DateTime? CertificateValidTo { get; set; }
        public string? CertificateThumbprint { get; set; }
    }
} 