using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Remote;
using Docker.DotNet;
using Docker.DotNet.Models;
using PhishingAnalyzer.Core.Models;
using System.Net.Http;
using PhishingAnalyzer.ML.Services;
using OpenQA.Selenium.Support.UI;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Net;

namespace PhishingAnalyzer.Core.Services
{
    public class WebsiteAnalyzer
    {
        private readonly DockerClient _dockerClient;
        private readonly PhishingModelTrainer? _modelTrainer;
        private const string SeleniumImage = "seleniarm/standalone-chromium:latest";
        private const int SeleniumPort = 4444;
        private const int MaxRetries = 5;
        private const int RetryDelayMs = 2000;

        public WebsiteAnalyzer(string? modelPath = null)
        {
            try
            {
                _dockerClient = new DockerClientConfiguration().CreateClient();
                
                if (modelPath != null)
                {
                    _modelTrainer = new PhishingModelTrainer();
                    _modelTrainer.LoadModel(modelPath);
                }

                // Clean up any existing containers
                CleanupExistingContainers();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to initialize Docker client: {ex.Message}");
                throw;
            }
        }

        private async Task CleanupExistingContainers()
        {
            try
            {
                var containers = await _dockerClient.Containers.ListContainersAsync(
                    new ContainersListParameters { All = true });
                
                foreach (var container in containers)
                {
                    if (container.Image == SeleniumImage)
                    {
                        try
                        {
                            await _dockerClient.Containers.StopContainerAsync(
                                container.ID, 
                                new ContainerStopParameters());
                            await _dockerClient.Containers.RemoveContainerAsync(
                                container.ID, 
                                new ContainerRemoveParameters());
                            Console.WriteLine($"Cleaned up existing container: {container.ID}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error cleaning up container {container.ID}: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during container cleanup: {ex.Message}");
            }
        }

        private async Task WaitForSeleniumReady(string seleniumUrl)
        {
            using var client = new HttpClient();
            for (int i = 0; i < MaxRetries; i++)
            {
                try
                {
                    await Task.Delay(RetryDelayMs);
                    var response = await client.GetAsync($"{seleniumUrl}/status");
                    if (response.IsSuccessStatusCode)
                    {
                        return;
                    }
                }
                catch
                {
                    if (i == MaxRetries - 1)
                    {
                        throw new Exception("Selenium container is not ready after maximum retries");
                    }
                }
            }
        }

        private async Task<CertificateInfo> CheckCertificateAsync(string url)
        {
            var certificateInfo = new CertificateInfo();
            
            try
            {
                var uri = new Uri(url);
                using var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
                    {
                        if (cert != null)
                        {
                            certificateInfo.IsValid = sslPolicyErrors == SslPolicyErrors.None;
                            certificateInfo.Subject = cert.Subject;
                            certificateInfo.Issuer = cert.Issuer;
                            certificateInfo.ValidFrom = cert.NotBefore;
                            certificateInfo.ValidTo = cert.NotAfter;
                            certificateInfo.Thumbprint = cert.Thumbprint;
                        }
                        return true; // Return true to continue with the request
                    }
                };

                using var client = new HttpClient(handler);
                await client.GetAsync(url);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking certificate: {ex.Message}");
                certificateInfo.IsValid = false;
                certificateInfo.Error = ex.Message;
            }

            return certificateInfo;
        }

        public async Task<AnalysisResult> AnalyzeWebsiteAsync(string url)
        {
            var result = new AnalysisResult
            {
                Url = url,
                AnalysisTime = DateTime.UtcNow,
                RiskLevel = "Unknown" // Initial value, will be updated after analysis
            };

            try
            {
                // Get ML prediction if available
                if (_modelTrainer != null)
                {
                    var prediction = _modelTrainer.Predict(url);
                    result.AdditionalData["MLPrediction"] = prediction;
                }

                // Check for HTTPS and analyze certificate if present
                result.IsSecure = url.StartsWith("https://");
                if (result.IsSecure)
                {
                    var certificateInfo = await CheckCertificateAsync(url);
                    result.AdditionalData["CertificateInfo"] = certificateInfo;
                    
                    if (!certificateInfo.IsValid)
                    {
                        result.Warnings.Add($"Invalid SSL certificate: {certificateInfo.Error}");
                    }
                    else if (certificateInfo.ValidTo < DateTime.UtcNow)
                    {
                        result.Warnings.Add("SSL certificate has expired");
                    }
                    else if (certificateInfo.ValidTo < DateTime.UtcNow.AddDays(30))
                    {
                        result.Warnings.Add("SSL certificate will expire soon");
                    }
                }

                // Always perform deep analysis
                string? containerId = null;
                try
                {
                    containerId = await StartSeleniumContainerAsync();
                    Console.WriteLine($"Started Selenium container: {containerId}");

                    // Configure Chrome options
                    var options = new ChromeOptions();
                    options.AddArgument("--headless");
                    options.AddArgument("--no-sandbox");
                    options.AddArgument("--disable-dev-shm-usage");
                    options.AddArgument("--disable-gpu");
                    options.AddArgument("--window-size=1920,1080");

                    // Connect to Selenium
                    var seleniumUrl = $"http://localhost:{SeleniumPort}";
                    await WaitForSeleniumReady(seleniumUrl);
                    
                    using var driver = new RemoteWebDriver(new Uri($"{seleniumUrl}/wd/hub"), options);
                    Console.WriteLine("Connected to Selenium WebDriver");

                    // Navigate to URL
                    driver.Navigate().GoToUrl(url);
                    Console.WriteLine($"Navigated to URL: {url}");

                    // Wait for page to load
                    var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(30)); // Increased timeout
                    
                    // Wait for document ready state
                    wait.Until(d => ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState").Equals("complete"));
                    
                    // Wait for any AJAX requests to complete
                    wait.Until(d => ((IJavaScriptExecutor)d).ExecuteScript("return jQuery.active === 0").Equals(true));
                    
                    // Wait for any dynamic content to load
                    await Task.Delay(5000); // Increased delay for dynamic content
                    
                    // Additional wait for any lazy-loaded content
                    try
                    {
                        wait.Until(d => ((IJavaScriptExecutor)d).ExecuteScript(@"
                            return new Promise((resolve) => {
                                const images = Array.from(document.getElementsByTagName('img'));
                                const scripts = Array.from(document.getElementsByTagName('script'));
                                const iframes = Array.from(document.getElementsByTagName('iframe'));
                                
                                const allLoaded = images.every(img => img.complete) &&
                                                scripts.every(script => script.readyState === 'complete' || script.readyState === 'loaded') &&
                                                iframes.every(iframe => iframe.contentWindow.document.readyState === 'complete');
                                
                                resolve(allLoaded);
                            });
                        ").Equals(true));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Warning: Some content might not have loaded completely: {ex.Message}");
                    }

                    // Collect JavaScript errors
                    var logs = driver.Manage().Logs.GetLog(LogType.Browser);
                    foreach (var log in logs)
                    {
                        if (log.Level == LogLevel.Severe)
                        {
                            result.JavaScriptErrors.Add(log.Message);
                        }
                        else if (log.Level == LogLevel.Warning)
                        {
                            result.Warnings.Add(log.Message);
                        }
                    }

                    // Take screenshot
                    try
                    {
                        var screenshot = ((ITakesScreenshot)driver).GetScreenshot();
                        var screenshotFileName = $"{Guid.NewGuid()}.png";
                        var screenshotPath = Path.Combine("wwwroot", "screenshots", screenshotFileName);
                        screenshot.SaveAsFile(screenshotPath);
                        result.ScreenshotPath = $"/screenshots/{screenshotFileName}";
                        Console.WriteLine($"Saved screenshot to: {screenshotPath}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to take screenshot: {ex.Message}");
                        result.Warnings.Add($"Failed to take screenshot: {ex.Message}");
                    }

                    // Analyze page content
                    AnalyzePageContent(driver, result);

                    // Calculate risk score
                    CalculateRiskScore(result);

                    // Log comprehensive analysis results
                    Console.WriteLine("\n=== Comprehensive Analysis Results ===");
                    Console.WriteLine($"URL: {result.Url}");
                    Console.WriteLine($"Analysis Time: {result.AnalysisTime}");
                    Console.WriteLine($"Risk Score: {result.RiskScore}");
                    Console.WriteLine($"Risk Level: {result.RiskLevel}");
                    Console.WriteLine($"Is Secure (HTTPS): {result.IsSecure}");
                    
                    Console.WriteLine("\nJavaScript Errors:");
                    if (result.JavaScriptErrors.Count > 0)
                    {
                        foreach (var error in result.JavaScriptErrors)
                        {
                            Console.WriteLine($"- {error}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("No JavaScript errors detected");
                    }

                    Console.WriteLine("\nWarnings:");
                    if (result.Warnings.Count > 0)
                    {
                        foreach (var warning in result.Warnings)
                        {
                            Console.WriteLine($"- {warning}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("No warnings detected");
                    }

                    Console.WriteLine("\nSuspicious Patterns:");
                    if (result.SuspiciousPatterns.Count > 0)
                    {
                        foreach (var pattern in result.SuspiciousPatterns)
                        {
                            Console.WriteLine($"- {pattern}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("No suspicious patterns detected");
                    }

                    if (result.AdditionalData.ContainsKey("MLPrediction"))
                    {
                        var prediction = result.AdditionalData["MLPrediction"];
                        Console.WriteLine("\nML Model Prediction:");
                        Console.WriteLine($"- Label: {((dynamic)prediction).Label}");
                        Console.WriteLine($"- Probability: {((dynamic)prediction).Probability:P2}");
                    }

                    if (result.AdditionalData.ContainsKey("CertificateInfo"))
                    {
                        var certInfo = result.AdditionalData["CertificateInfo"];
                        Console.WriteLine("\nSSL Certificate Information:");
                        Console.WriteLine($"- Valid: {((dynamic)certInfo).IsValid}");
                        Console.WriteLine($"- Subject: {((dynamic)certInfo).Subject}");
                        Console.WriteLine($"- Issuer: {((dynamic)certInfo).Issuer}");
                        Console.WriteLine($"- Valid From: {((dynamic)certInfo).ValidFrom}");
                        Console.WriteLine($"- Valid To: {((dynamic)certInfo).ValidTo}");
                        Console.WriteLine($"- Thumbprint: {((dynamic)certInfo).Thumbprint}");
                    }

                    if (!string.IsNullOrEmpty(result.ScreenshotPath))
                    {
                        Console.WriteLine($"\nScreenshot saved to: {result.ScreenshotPath}");
                    }

                    Console.WriteLine("\n=== End of Analysis Results ===\n");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error during website analysis: {ex.Message}");
                    result.Warnings.Add($"Analysis failed: {ex.Message}");
                }
                finally
                {
                    if (containerId != null)
                    {
                        try
                        {
                            await StopContainerAsync(containerId);
                            Console.WriteLine($"Stopped Selenium container: {containerId}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error stopping container: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fatal error during analysis: {ex.Message}");
                result.Warnings.Add($"Analysis failed: {ex.Message}");
            }

            return result;
        }

        private async Task<string> StartSeleniumContainerAsync()
        {
            try
            {
                var createParams = new CreateContainerParameters
                {
                    Image = SeleniumImage,
                    ExposedPorts = new Dictionary<string, EmptyStruct>
                    {
                        { $"{SeleniumPort}/tcp", new EmptyStruct() }
                    },
                    HostConfig = new HostConfig
                    {
                        PortBindings = new Dictionary<string, IList<PortBinding>>
                        {
                            {
                                $"{SeleniumPort}/tcp",
                                new List<PortBinding>
                                {
                                    new PortBinding
                                    {
                                        HostPort = SeleniumPort.ToString(),
                                        HostIP = "0.0.0.0"
                                    }
                                }
                            }
                        }
                    }
                };

                var response = await _dockerClient.Containers.CreateContainerAsync(createParams);
                await _dockerClient.Containers.StartContainerAsync(response.ID, new ContainerStartParameters());
                return response.ID;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to start Selenium container: {ex.Message}");
                throw;
            }
        }

        private async Task StopContainerAsync(string containerId)
        {
            try
            {
                await _dockerClient.Containers.StopContainerAsync(containerId, new ContainerStopParameters());
                await _dockerClient.Containers.RemoveContainerAsync(containerId, new ContainerRemoveParameters());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to stop container: {ex.Message}");
                throw;
            }
        }

        private void AnalyzePageContent(IWebDriver driver, AnalysisResult result)
        {
            try
            {
                // Check for login forms
                var loginForms = driver.FindElements(By.CssSelector("form[action*='login'], form[action*='signin']"));
                if (loginForms.Count > 0)
                {
                    result.SuspiciousPatterns.Add("Login form detected");
                }

                // Check for redirects
                var currentUrl = driver.Url;
                if (currentUrl != result.Url)
                {
                    result.SuspiciousPatterns.Add($"Page redirected from {result.Url} to {currentUrl}");
                }

                // Check for suspicious scripts
                var scripts = driver.FindElements(By.TagName("script"));
                foreach (var script in scripts)
                {
                    var src = script.GetAttribute("src");
                    var type = script.GetAttribute("type");
                    var content = script.GetAttribute("innerHTML");

                    if (!string.IsNullOrEmpty(src))
                    {
                        if (!src.StartsWith("http"))
                        {
                            result.SuspiciousPatterns.Add($"Suspicious external script source: {src}");
                        }
                        else if (src.Contains("javascript:") || src.Contains("data:"))
                        {
                            result.SuspiciousPatterns.Add($"Potentially dangerous script source: {src}");
                        }
                    }
                    else if (!string.IsNullOrEmpty(content))
                    {
                        // Check for suspicious patterns in inline scripts
                        if (content.Contains("eval(") || content.Contains("document.write("))
                        {
                            result.SuspiciousPatterns.Add("Suspicious inline script detected with eval or document.write");
                        }
                        else if (content.Contains("window.location") || content.Contains("document.location"))
                        {
                            result.SuspiciousPatterns.Add("Suspicious inline script detected with location manipulation");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error analyzing page content: {ex.Message}");
                result.Warnings.Add($"Page content analysis failed: {ex.Message}");
            }
        }

        private void CalculateRiskScore(AnalysisResult result)
        {
            try
            {
                double score = 0;

                // Base score for non-HTTPS
                if (!result.IsSecure)
                {
                    score += 30;
                }

                // Add points for JavaScript errors
                score += result.JavaScriptErrors.Count * 5;

                // Add points for warnings
                score += result.Warnings.Count * 2;

                // Add points for suspicious patterns
                score += result.SuspiciousPatterns.Count * 10;

                // Calculate risk level
                result.RiskScore = Math.Min(100, score);
                result.RiskLevel = result.RiskScore switch
                {
                    < 30 => "Low",
                    < 60 => "Medium",
                    < 80 => "High",
                    _ => "Critical"
                };

                Console.WriteLine($"Calculated risk score: {result.RiskScore}, level: {result.RiskLevel}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calculating risk score: {ex.Message}");
                result.RiskScore = 0;
                result.RiskLevel = "Unknown";
            }
        }
    }

    public class CertificateInfo
    {
        public bool IsValid { get; set; }
        public string? Subject { get; set; }
        public string? Issuer { get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime ValidTo { get; set; }
        public string? Thumbprint { get; set; }
        public string? Error { get; set; }
    }
} 