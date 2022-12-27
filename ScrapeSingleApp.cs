using System.Net;
using HtmlAgilityPack;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using NLog.Fluent;

namespace MyAppFree
{
    // This class contains methods for scraping the HTML content of an app's page on the Google Play Store,
    // and saving the scraped data as a JSON string
    class ScrapeSingleApp
    {
        // Declare variables to store the app's file name, name, category, stars rating, version, URL, and proxy
        private string FileName;
        private string name;
        private string category;
        private string stars;
        private string VersioneCorrente;
        private string URL;
        private string proxy;

        public ScrapeSingleApp(string aFileName, string aProxy)
        {
            FileName = aFileName.Replace("/store/apps/details?id=", "");
            proxy= aProxy;
            URL = "https://play.google.com/store/apps/details?id=" + FileName;
            name = "";
            category = "";
            stars = "";
            VersioneCorrente = "";
    }

        public ScrapeSingleApp(string aFileName)
        {
            FileName = aFileName.Replace("/store/apps/details?id=", "");
            proxy = "";
            URL = "https://play.google.com/store/apps/details?id=" + FileName;
            name = "";
            category = "";
            stars = "";
            VersioneCorrente = "";
        }

        public async Task Start()
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            Console.WriteLine("Starting: " + FileName);
            string workingDirectory = Directory.GetParent(Environment.CurrentDirectory).Parent.FullName.Replace("bin", @"result");
            string path = Path.Combine(workingDirectory, FileName + ".json");
            if (File.Exists(path))
            {
                Console.WriteLine("File " + FileName + " already exists");
            }
            else
            {
                await ScrapeHtml();
                SaveJson();
            }
            watch.Stop();
            //Console.WriteLine(watch.ElapsedMilliseconds);
        }

        private async Task ScrapeHTMLParallel()
        {
            string html = await Program.GetHTMLContent(URL, proxy);

            // Load the HTML into a HtmlDocument
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Create a list of tasks to perform the operations in parallel
            List<Task> tasks = new List<Task>();

            // Get App Name using XPath
            Task task1 = Task.Run(() => {
                name = doc.DocumentNode.SelectSingleNode("//h1").InnerText;
            });
            tasks.Add(task1);

            // Get App Category using CPath
            Task task2 = Task.Run(() => {
                HtmlNodeCollection categoryNodes = doc.DocumentNode.SelectNodes("//span[@jsname='V67aGc' and @class='VfPpkd-vQzf8d']");
                HtmlNode mainCategoryNode = categoryNodes[3];
                category = mainCategoryNode.InnerText;
            });
            tasks.Add(task2);

            // Get App Version using REGEX
            Task task3 = Task.Run(() => {
                string versionRegex = "You can request that data be deleted"; // it seems the safest option
                MatchCollection versionMatches = Regex.Matches(html, versionRegex);
                Match mainVersionMatch = versionMatches[1];

                // Extract the phrase from the input string
                string versionSubstring = html.Substring(mainVersionMatch.Index, 100);
                VersioneCorrente = versionSubstring.Split("null,[[[")[1].Split("]]")[0].Replace("\"", "");
            });
            tasks.Add(task3);

            // Get App Stars using XPath
            Task task4 = Task.Run(() => {
                stars = doc.DocumentNode.SelectSingleNode("//div[@itemprop='starRating']").InnerText.Replace("star", "");
            });
            tasks.Add(task4);

            // Wait for all tasks to complete
            Task.WaitAll(tasks.ToArray());

        }

        private async Task ScrapeHtml()
        {
            try
            {
                string html = await Program.GetHTMLContent(URL, proxy);
                // Load the HTML into a HtmlDocument
                HtmlDocument doc = new();
              

                doc.LoadHtml(html);
                
                // Get App Name using XPath
                name = doc.DocumentNode.SelectSingleNode("//h1").InnerText;
                //Console.WriteLine(name);

                // Get App Category using CPath
                HtmlNodeCollection categoryNodes = doc.DocumentNode.SelectNodes("//span[@jsname='V67aGc' and @class='VfPpkd-vQzf8d']");
                HtmlNode mainCategoryNode = categoryNodes[3];
                category = mainCategoryNode.InnerText;
                //Console.WriteLine(category);

                // Get App Version using REGEX
                string versionRegex = "You can request that data be deleted"; // it seems the safest option
                MatchCollection versionMatches = Regex.Matches(html, versionRegex);
                Match mainVersionMatch = versionMatches[1];

                // Extract the phrase from the input string
                string versionSubstring = html.Substring(mainVersionMatch.Index, 100);
                VersioneCorrente = versionSubstring.Split("null,[[[")[1].Split("]]")[0].Replace("\"", "");
                //Console.WriteLine(VersioneCorrente);

                // Get App Starts using XPath
                stars = doc.DocumentNode.SelectSingleNode("//div[@itemprop='starRating']").InnerText.Replace("star", "");
                //Console.WriteLine(stars);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unable to get APP info");
                Program.logger.Info("Unable to get APP info: " + ex.Message);
            }
        }

        private void SaveJson()
        {
            // Create a dictionary and populate its keys and values
            Dictionary<string, string> data = new();
            data["Name"] = name;
            data["Category"] = category;
            data["Stars"] = stars;
            data["Versione corrente"] = VersioneCorrente;

            // Serialize the data to a JSON string
            string json = JsonConvert.SerializeObject(data, Formatting.Indented);

            Program.logger.Info(json);
            try
            {
                string workingDirectory = Directory.GetParent(Environment.CurrentDirectory).Parent.FullName.Replace("bin", @"result");
                string path = Path.Combine(workingDirectory, FileName + ".json");
                if (File.Exists(path))
                {
                    Console.WriteLine("File " + FileName + " already exists");
                }
                else
                {
                    File.WriteAllText(path, json);
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("Failed at saving " + FileName);
                Program.logger.Info("Failed at saving " + FileName + ": " + ex.Message);
            }

        }

        
    }
}
