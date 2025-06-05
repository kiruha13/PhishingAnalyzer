using Microsoft.ML;
using Microsoft.ML.Data;
using PhishingAnalyzer.ML.Features;

namespace PhishingAnalyzer.ML.Services;

public class ModelEvaluator
{
    private readonly MLContext _mlContext;
    private readonly ITransformer _model;

    public ModelEvaluator(MLContext mlContext, ITransformer model)
    {
        _mlContext = mlContext;
        _model = model;
    }

    public void EvaluateModel(IDataView testData)
    {
        var predictions = _model.Transform(testData);
        var metrics = _mlContext.BinaryClassification.Evaluate(predictions, labelColumnName: "IsPhishing");

        Console.WriteLine("\nModel Evaluation Metrics:");
        Console.WriteLine("------------------------");
        Console.WriteLine($"Accuracy: {metrics.Accuracy:P2}");
        Console.WriteLine($"Precision: {metrics.PositivePrecision:P2}");
        Console.WriteLine($"Recall: {metrics.PositiveRecall:P2}");
        Console.WriteLine($"F1Score: {metrics.F1Score:P2}");
        Console.WriteLine($"AUC: {metrics.AreaUnderRocCurve:P2}");
        Console.WriteLine($"AUCPR: {metrics.AreaUnderPrecisionRecallCurve:P2}");
        Console.WriteLine("\nConfusion Matrix:");
        Console.WriteLine($"True Positives: {metrics.ConfusionMatrix.GetCountForClassPair(1, 1)}");
        Console.WriteLine($"False Positives: {metrics.ConfusionMatrix.GetCountForClassPair(0, 1)}");
        Console.WriteLine($"True Negatives: {metrics.ConfusionMatrix.GetCountForClassPair(0, 0)}");
        Console.WriteLine($"False Negatives: {metrics.ConfusionMatrix.GetCountForClassPair(1, 0)}");

        // Calculate additional metrics
        var total = metrics.ConfusionMatrix.GetCountForClassPair(1, 1) +
                   metrics.ConfusionMatrix.GetCountForClassPair(0, 1) +
                   metrics.ConfusionMatrix.GetCountForClassPair(0, 0) +
                   metrics.ConfusionMatrix.GetCountForClassPair(1, 0);

        var truePositives = metrics.ConfusionMatrix.GetCountForClassPair(1, 1);
        var falsePositives = metrics.ConfusionMatrix.GetCountForClassPair(0, 1);
        var trueNegatives = metrics.ConfusionMatrix.GetCountForClassPair(0, 0);
        var falseNegatives = metrics.ConfusionMatrix.GetCountForClassPair(1, 0);

        var specificity = (double)trueNegatives / (trueNegatives + falsePositives);
        var negativePredictiveValue = (double)trueNegatives / (trueNegatives + falseNegatives);
        var falsePositiveRate = (double)falsePositives / (falsePositives + trueNegatives);
        var falseNegativeRate = (double)falseNegatives / (falseNegatives + truePositives);

        Console.WriteLine("\nAdditional Metrics:");
        Console.WriteLine($"Specificity: {specificity:P2}");
        Console.WriteLine($"Negative Predictive Value: {negativePredictiveValue:P2}");
        Console.WriteLine($"False Positive Rate: {falsePositiveRate:P2}");
        Console.WriteLine($"False Negative Rate: {falseNegativeRate:P2}");
        Console.WriteLine($"Total Samples: {total}");
    }
} 