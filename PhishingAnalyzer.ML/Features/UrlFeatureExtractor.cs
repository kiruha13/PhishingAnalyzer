using System;
using System.Text.RegularExpressions;
using System.Linq;

namespace PhishingAnalyzer.ML.Features
{
    public class UrlFeatureExtractor
    {
        private static readonly string[] SuspiciousWords = new[]
        {
            "login", "signin", "account", "secure", "banking", "verify",
            "confirm", "update", "password", "security", "alert", "warning"
        };

        public static float[] ExtractFeatures(string url)
        {
            var features = new List<float>();

            // Basic URL features
            features.Add(url.Length);
            features.Add(CountSpecialCharacters(url));
            features.Add(CountDigits(url));
            features.Add(CountUppercaseLetters(url));
            features.Add(CountSuspiciousWords(url));
            
            // URL structure features
            features.Add(HasValidProtocol(url) ? 1 : 0);
            features.Add(HasValidDomain(url) ? 1 : 0);
            features.Add(GetDomainLength(url));
            features.Add(GetPathLength(url));
            features.Add(GetQueryLength(url));
            
            // Additional features
            features.Add(HasIPAddress(url) ? 1 : 0);
            features.Add(HasSuspiciousTLD(url) ? 1 : 0);
            features.Add(GetSuspiciousWordRatio(url));

            return features.ToArray();
        }

        private static int CountSpecialCharacters(string url)
        {
            return url.Count(c => !char.IsLetterOrDigit(c) && c != '.' && c != '/' && c != ':' && c != '-');
        }

        private static int CountDigits(string url)
        {
            return url.Count(char.IsDigit);
        }

        private static int CountUppercaseLetters(string url)
        {
            return url.Count(char.IsUpper);
        }

        private static int CountSuspiciousWords(string url)
        {
            return SuspiciousWords.Count(word => url.ToLower().Contains(word));
        }

        private static bool HasValidProtocol(string url)
        {
            return url.StartsWith("http://") || url.StartsWith("https://");
        }

        private static bool HasValidDomain(string url)
        {
            try
            {
                var uri = new Uri(url);
                return !string.IsNullOrEmpty(uri.Host);
            }
            catch
            {
                return false;
            }
        }

        private static float GetDomainLength(string url)
        {
            try
            {
                var uri = new Uri(url);
                return uri.Host.Length;
            }
            catch
            {
                return 0;
            }
        }

        private static float GetPathLength(string url)
        {
            try
            {
                var uri = new Uri(url);
                return uri.AbsolutePath.Length;
            }
            catch
            {
                return 0;
            }
        }

        private static float GetQueryLength(string url)
        {
            try
            {
                var uri = new Uri(url);
                return uri.Query.Length;
            }
            catch
            {
                return 0;
            }
        }

        private static bool HasIPAddress(string url)
        {
            var ipPattern = @"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b";
            return Regex.IsMatch(url, ipPattern);
        }

        private static bool HasSuspiciousTLD(string url)
        {
            var suspiciousTLDs = new[] { ".xyz", ".tk", ".pw", ".info", ".biz" };
            return suspiciousTLDs.Any(tld => url.EndsWith(tld, StringComparison.OrdinalIgnoreCase));
        }

        private static float GetSuspiciousWordRatio(string url)
        {
            var words = url.Split(new[] { '/', '.', '-', '_', '?' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0) return 0;
            
            var suspiciousCount = words.Count(word => 
                SuspiciousWords.Any(suspicious => 
                    word.Contains(suspicious, StringComparison.OrdinalIgnoreCase)));
            
            return (float)suspiciousCount / words.Length;
        }
    }
} 