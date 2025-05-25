using System;
using System.Threading.Tasks;
using PhishingAnalyzer.Core.Services;
using PhishingAnalyzer.ML.Services;
using Newtonsoft.Json;

namespace PhishingAnalyzer.Core
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Phishing Website Analyzer");
            Console.WriteLine("------------------------");

            if (args.Length == 0)
            {
                Console.WriteLine("Usage:");
                Console.WriteLine("  Train model: dotnet run -- train <dataset_path> <model_save_path>");
                Console.WriteLine("  Analyze URL: dotnet run -- analyze <url> [model_path]");
                return;
            }

            var command = args[0].ToLower();

            switch (command)
            {
                case "train":
                    if (args.Length < 3)
                    {
                        Console.WriteLine("Please provide dataset path and model save path.");
                        return;
                    }
                    TrainModel(args[1], args[2]);
                    break;

                case "analyze":
                    if (args.Length < 2)
                    {
                        Console.WriteLine("Please provide a URL to analyze.");
                        return;
                    }
                    var url = args[1];
                    var modelPath = args.Length > 2 ? args[2] : null;
                    await AnalyzeUrl(url, modelPath);
                    break;

                default:
                    Console.WriteLine("Unknown command. Use 'train' or 'analyze'.");
                    break;
            }
        }

        private static void TrainModel(string datasetPath, string modelSavePath)
        {
            Console.WriteLine($"Training model using dataset: {datasetPath}");
            var trainer = new PhishingModelTrainer();
            trainer.Train(datasetPath);
            trainer.SaveModel(modelSavePath);
            Console.WriteLine($"Model saved to: {modelSavePath}");
        }

        private static async Task AnalyzeUrl(string url, string? modelPath)
        {
            Console.WriteLine($"Analyzing URL: {url}");
            if (modelPath != null)
            {
                Console.WriteLine($"Using ML model: {modelPath}");
            }

            var analyzer = new WebsiteAnalyzer(modelPath);
            var result = await analyzer.AnalyzeWebsiteAsync(url);

            Console.WriteLine("\nAnalysis Results:");
            Console.WriteLine($"URL: {result.Url}");
            Console.WriteLine($"Analysis Time: {result.AnalysisTime}");
            Console.WriteLine($"Is Secure: {result.IsSecure}");
            Console.WriteLine($"Risk Score: {result.RiskScore}");
            Console.WriteLine($"Risk Level: {result.RiskLevel}");

            if (result.AdditionalData.ContainsKey("MLPrediction"))
            {
                var prediction = result.AdditionalData["MLPrediction"];
                Console.WriteLine("\nML Model Prediction:");
                Console.WriteLine($"Predicted Label: {((dynamic)prediction).Label}");
                Console.WriteLine($"Probability: {((dynamic)prediction).Probability:P2}");
            }

            if (result.JavaScriptErrors.Count > 0)
            {
                Console.WriteLine("\nJavaScript Errors:");
                foreach (var error in result.JavaScriptErrors)
                {
                    Console.WriteLine($"- {error}");
                }
            }

            if (result.Warnings.Count > 0)
            {
                Console.WriteLine("\nWarnings:");
                foreach (var warning in result.Warnings)
                {
                    Console.WriteLine($"- {warning}");
                }
            }

            if (result.SuspiciousPatterns.Count > 0)
            {
                Console.WriteLine("\nSuspicious Patterns:");
                foreach (var pattern in result.SuspiciousPatterns)
                {
                    Console.WriteLine($"- {pattern}");
                }
            }

            if (!string.IsNullOrEmpty(result.ScreenshotPath))
            {
                Console.WriteLine($"\nScreenshot saved to: {result.ScreenshotPath}");
            }
        }
    }
}
