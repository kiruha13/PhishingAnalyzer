namespace PhishingAnalyzer.ML.Features
{
    public class UrlFeatures
    {
        public float Length { get; set; }
        public float SpecialChars { get; set; }
        public float Digits { get; set; }
        public float Uppercase { get; set; }
        public float SuspiciousWords { get; set; }
        public float HasValidProtocol { get; set; }
        public float HasValidDomain { get; set; }
        public float DomainLength { get; set; }
        public float PathLength { get; set; }
        public float QueryLength { get; set; }
        public float HasIPAddress { get; set; }
        public float HasSuspiciousTLD { get; set; }
        public float SuspiciousWordRatio { get; set; }
    }
} 