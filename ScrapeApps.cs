using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MyAppFree
{
    internal class ScrapeApps
    {
        private List<string> _apps;
        private string _URL;
        private string _proxy;

        public ScrapeApps(string URL)
        {
            _apps = new List<string>();
            _URL = URL;
            _proxy = "";
        }

        public ScrapeApps(string URL, string proxy)
        {
            _apps = new List<string>();
            _URL = URL;
            _proxy = proxy;
        }

        public ScrapeApps(List<string> apps, string URL, string proxy)
        {
            _apps = apps;
            _URL = URL;
            _proxy = proxy;
        }

        public List<string> GetApps()
        {
            return _apps;
        } 

        public async Task ScrapeAppsFromURL()
        {
            try
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();
                string html = await Program.GetHTMLContent(_URL, _proxy);
                // Load the HTML into a HtmlDocument
                HtmlDocument doc = new();
                doc.LoadHtml(html);

                Regex regex = new Regex("details\\?id=");
                MatchCollection matches = regex.Matches(html);
                foreach (Match match in matches)
                {
                    _apps.Add(html.Substring(match.Index, 100).Split("=")[1].Split("\"")[0]);
                }
                watch.Stop();
                //Console.WriteLine(watch1.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unable to get APPs ");
                Program.logger.Info("Unable to get APPs: " + ex.Message);
            }
        }

        // SLOWER BUT SAFER OPTION
        public async Task ScrapeAppsFromURLXPath()
        {
            try
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();
                string html = await Program.GetHTMLContent(_URL, _proxy);
                // Load the HTML into a HtmlDocument
                HtmlDocument doc = new();
                doc.LoadHtml(html);

                // Use the SelectNodes method and an XPath expression to find all the "a" elements with a "href" attribute
                HtmlNodeCollection nodes = doc.DocumentNode.SelectNodes("//a[@href]");
                var hrefList = nodes.Where(node => node.GetAttributeValue("href", "").Contains("?id="))
                                  .Select(node => node.GetAttributeValue("href", "")).ToList();
                foreach (var href in hrefList)
                {
                    _apps.Add(href);
                }
                watch.Stop();
                //Console.WriteLine(watch1.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unable to get APPs ");
                Program.logger.Info("Unable to get APPs: " + ex.Message);
            }
        }

    }
}
