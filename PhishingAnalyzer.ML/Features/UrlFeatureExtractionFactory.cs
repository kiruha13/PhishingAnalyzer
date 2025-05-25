using Microsoft.ML.Transforms;
using PhishingAnalyzer.ML.Models;

namespace PhishingAnalyzer.ML.Features
{
    [CustomMappingFactoryAttribute("UrlFeatureExtraction")]
    public class UrlFeatureExtractionFactory : CustomMappingFactory<UrlData, UrlFeatures>
    {
        public override Action<UrlData, UrlFeatures> GetMapping()
        {
            return (input, output) =>
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
            };
        }
    }
} 