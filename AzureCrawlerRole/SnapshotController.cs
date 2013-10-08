using AzureCrawlerRole.Helpers;
using AzureCrawlerRole.Model;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace AzureCrawlerRole
{    
    /// <summary>
    /// Crawl urls using phantomjs
    /// </summary>
    public class SnapshotController : ApiController
    {
        /// <summary>
        /// Receive post request and crawl the target url
        /// /api/snapshot
        /// </summary>
        /// <param name="config">The config object</param>
        /// <returns>HttpResponseMessage</returns>
        [HttpPost]
        public HttpResponseMessage Post(SnapshotConfig config)
        {
            // Validate credentials
            if (!ValidateCredentials(config.ApiId, config.Application))
            {
                return new HttpResponseMessage(HttpStatusCode.Forbidden);
            }

            // If Url is null the can´t crawl
            if (config.Url == null)
            {
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Content = new StringContent("Url is required for crawl")
                };
            }

            // If storing snapshot then a expiration date is required
            if (config.Store && config.ExpirationDate == null)
            {
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Content = new StringContent("Can´t store the crawl result without an expiration date")
                };
            }

            //Create container for the app
            AzureHelper.CreateAppContainerIfNotExists(config.Application);

            if (AzureHelper.SnapshotExist(config.Application, config.Url))
            {
                // If the snapshot is expired then crawl again
                if (AzureHelper.SnapshotExpired(config.Application, config.Url))
                {
                    var result = Crawl(config.Url);

                    if (!result.Contains("Error : Unable to load url") && config.Store)
                    {                        
                        AzureHelper.SaveSnapshot(config.Application, config.Url, SkipMetaFragment(result));
                        AzureHelper.SetBlobAttributes(config.Application, config.Url, config.UserAgent, config.ExpirationDate);
                    }

                    return new HttpResponseMessage()
                    {
                        Content = new StringContent(result)
                    };
                }

                // If not expired then read the stored snapshot
                return new HttpResponseMessage()
                {
                    Content = new StringContent(AzureHelper.ReadSnapshot(config.Application, config.Url))
                };
            }
            else
            {
                // Crawl and store the snapshot
                var result = Crawl(config.Url);

                if (!result.Contains("Error : Unable to load url") && config.Store)
                {
                    AzureHelper.SaveSnapshot(config.Application, config.Url, SkipMetaFragment(result));
                    AzureHelper.SetBlobAttributes(config.Application, config.Url, config.UserAgent, config.ExpirationDate);
                }

                return new HttpResponseMessage()
                {
                    Content = new StringContent(result)
                };
            }      
        }

        /// <summary>
        /// Start a new phantomjs process for crawling
        /// </summary>
        /// <param name="url">The target url</param>
        /// <returns>Html string</returns>
        private string Crawl(string url)
        {
            string appRoot = Path.Combine(Environment.GetEnvironmentVariable("RoleRoot") + @"\", @"approot");

            var startInfo = new ProcessStartInfo
            {
                Arguments = String.Format("{0} {1}", Path.Combine(appRoot, "Scripts\\createSnapshot.js"), url),
                FileName = Path.Combine(appRoot, "phantomjs.exe"),
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8
            };

            var p = new Process();
            p.StartInfo = startInfo;
            p.Start();
            string output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();

            return output;
        }

        /// <summary>
        /// Validate ApiKey. In the real world you should this against a custom store
        /// </summary>
        /// <param name="apiKey">The api key</param>
        /// <param name="apiKey">The application</param>
        /// <returns>bool</returns>
        private bool ValidateCredentials(string apiKey, string application)
        {
            if (apiKey == "23aba36c-731f-4279-8114-4c761e25dbbb" && application == "durandalauth")
            {
                return true;
            }

            return false;
        }

        private string SkipMetaFragment(string html)
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);
            HtmlNode node = doc.DocumentNode.SelectSingleNode("//meta[@name='fragment']");
            if (node != null)
            {
                node.Remove();
            }            
            return doc.DocumentNode.InnerHtml;
        }
    }
}