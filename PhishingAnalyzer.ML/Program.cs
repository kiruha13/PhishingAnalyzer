using Microsoft.ML;
using PhishingAnalyzer.ML.Services;
using System;

namespace PhishingAnalyzer.ML
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                // Create MLContext
                var mlContext = new MLContext(seed: 1);

                // Create trainer instance
                var trainer = new PhishingModelTrainer();

                // Path to your existing CSV file
                string trainingDataPath = "phishing_site_urls.csv";
                
                // Train the model
                Console.WriteLine("Starting model training...");
                var (model, testData) = trainer.Train(trainingDataPath);

                // Save the trained model
                string modelPath = "phishing_model.zip";
                trainer.SaveModel(modelPath);
                Console.WriteLine($"Model saved to: {modelPath}");

                // Evaluate the model
                Console.WriteLine("\nEvaluating model performance...");
                var evaluator = new ModelEvaluator(mlContext, model);
                evaluator.EvaluateModel(testData);

                // Test the model with some URLs
                TestModel(trainer);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        static void TestModel(PhishingModelTrainer trainer)
        {
            // Test URLs
            string[] testUrls = new[]
            {
                "https://www.google.com",
                "https://www.microsoft.com",
                "autonomybibliography.info/",
                "awb.com/"
            };

            Console.WriteLine("\nTesting model with sample URLs:");
            foreach (var url in testUrls)
            {
                var prediction = trainer.Predict(url);
                Console.WriteLine($"URL: {url}");
                Console.WriteLine($"Prediction: {prediction.Label}");
                Console.WriteLine($"Probability: {prediction.Probability:P2}");
                Console.WriteLine();
            }
        }
    }
} 