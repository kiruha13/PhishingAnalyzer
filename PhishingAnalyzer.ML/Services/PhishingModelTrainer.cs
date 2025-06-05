using System;
using System.IO;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms;
using PhishingAnalyzer.ML.Models;
using PhishingAnalyzer.ML.Features;

namespace PhishingAnalyzer.ML.Services
{
    public class PhishingModelTrainer
    {
        private readonly MLContext _mlContext;
        private ITransformer? _model;
        private PredictionEngine<UrlData, UrlPrediction>? _predictionEngine;

        public PhishingModelTrainer()
        {
            _mlContext = new MLContext(seed: 1);
            // Register the assembly containing the custom mapping
            _mlContext.ComponentCatalog.RegisterAssembly(typeof(UrlFeatureExtractionFactory).Assembly);
        }

        public (ITransformer Model, IDataView TestData) Train(string dataPath)
        {
            // 1. Load raw data
            var data = _mlContext.Data.LoadFromTextFile<UrlData>(
                path: dataPath,
                hasHeader: true,
                separatorChar: ',');

            // 2. Split into train/test
            var split = _mlContext.Data.TrainTestSplit(data, testFraction: 0.2);

            // 3. Define label mapping ("good" -> false, "bad" -> true)
            var labelMapping = new[]
            {
                new KeyValuePair<string, bool>("good", false),
                new KeyValuePair<string, bool>("bad", true)
            };

            // 4. Build pipeline
            var pipeline = _mlContext.Transforms.Conversion.MapValue(
                    outputColumnName: "IsPhishing",
                    inputColumnName: "Label",
                    keyValuePairs: labelMapping)
                .Append(_mlContext.Transforms.Text.FeaturizeText("UrlTextFeatures", "Url"))
                .Append(_mlContext.Transforms.CustomMapping<UrlData, UrlFeatures>(
                    (input, output) =>
                    {
                        var features = UrlFeatureExtractor.ExtractFeatures(input.Url);
                        output.Length = features[0];
                        output.SpecialChars = features[1];
                        output.Digits = features[2];
                        output.Uppercase = features[3];
                        output.SuspiciousWords = features[4];
                        output.HasValidProtocol = features[5];
                        output.HasValidDomain = features[6];
                        output.DomainLength = features[7];
                        output.PathLength = features[8];
                        output.QueryLength = features[9];
                        output.HasIPAddress = features[10];
                        output.HasSuspiciousTLD = features[11];
                        output.SuspiciousWordRatio = features[12];
                    }, "UrlFeatureExtraction"))
                .Append(_mlContext.Transforms.Concatenate("Features",
                    "UrlTextFeatures",
                    "Length",
                    "SpecialChars",
                    "Digits",
                    "Uppercase",
                    "SuspiciousWords",
                    "HasValidProtocol",
                    "HasValidDomain",
                    "DomainLength",
                    "PathLength",
                    "QueryLength",
                    "HasIPAddress",
                    "HasSuspiciousTLD",
                    "SuspiciousWordRatio"))
                .Append(_mlContext.Transforms.NormalizeMinMax("Features"))
                .Append(_mlContext.BinaryClassification.Trainers.LbfgsLogisticRegression(
                    labelColumnName: "IsPhishing"));

            // 5. Train model
            Console.WriteLine("Training the model...");
            _model = pipeline.Fit(split.TrainSet);

            // 6. Create prediction engine
            _predictionEngine = _mlContext.Model.CreatePredictionEngine<UrlData, UrlPrediction>(_model);

            return (_model, split.TestSet);
        }

        public UrlPrediction Predict(string url)
        {
            if (_predictionEngine == null)
                throw new InvalidOperationException("Model not trained yet.");

            var urlData = new UrlData
            {
                Url = url,
                Label = "good" // dummy value, not used
            };

            return _predictionEngine.Predict(urlData);
        }

        public void SaveModel(string modelPath)
        {
            if (_model == null)
                throw new InvalidOperationException("Model not trained yet.");

            var emptyData = _mlContext.Data.LoadFromEnumerable<UrlData>(Array.Empty<UrlData>());

            using var stream = new FileStream(modelPath, FileMode.Create, FileAccess.Write, FileShare.Write);
            _mlContext.Model.Save(_model, emptyData.Schema, stream);
        }

        public void LoadModel(string modelPath)
        {
            using var stream = new FileStream(modelPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            _model = _mlContext.Model.Load(stream, out var _);
            _predictionEngine = _mlContext.Model.CreatePredictionEngine<UrlData, UrlPrediction>(_model);
        }
    }
}