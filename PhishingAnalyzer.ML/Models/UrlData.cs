using Microsoft.ML.Data;

namespace PhishingAnalyzer.ML.Models
{
    public class UrlData
    {
        [LoadColumn(0)]
        public string Url { get; set; } = string.Empty;

        [LoadColumn(1)]
        public string Label { get; set; } = string.Empty;
    }

    public class UrlPrediction
    {
        [ColumnName("PredictedLabel")]
        public bool PredictedLabel { get; set; }

        [ColumnName("Score")]
        public float Score { get; set; }

        public string Label => PredictedLabel ? "Bad" : "Good";

        public float Probability => 1 / (1 + (float)Math.Exp(-Score));
    }
} 