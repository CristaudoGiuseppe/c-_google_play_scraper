using HtmlAgilityPack;
using System.Net;
using NLog;


namespace MyAppFree
{
    class Program
    {
        // Declare variables
        private static Logger logger = LogManager.GetCurrentClassLogger(); // used to log main operations and errors
        private static string proxiesPath = Directory.GetParent(Environment.CurrentDirectory).Parent.FullName.Replace(@"bin\", "proxies.txt");
        private static string urlPlayStore = "https://play.google.com/store/apps"; 
        

        static async Task Main(string[] args)
        {

            var watch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                // Declare variables
                List<string> proxies = new List<string>();
                Random rand = new Random();
                string html;

                // Logger configuration
                configNlog();
                logger.Debug("DEBUG PARTITO");
                try
                {
                    proxies = ReadProxies(proxiesPath);
                }
                catch (Exception ex)
                {
                    logger.Error("Unable to read proxies: " + ex.Message);
                }
                if (proxies.Count == 0)
                {
                    Console.WriteLine("No proxies loaded..");
                    logger.Info("No proxies loaded..");
                    html = await GetHTMLContent(urlPlayStore, "");
                }
                else // make an HTTP request using proxies
                {
                    html = await GetHTMLContent(urlPlayStore, proxies[rand.Next(proxies.Count)]);
                }

                // Load the HTML into a HtmlDocument
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(html);

                // Use the SelectNodes method and an XPath expression to find all the "a" elements with a "href" attribute
                HtmlNodeCollection nodes = doc.DocumentNode.SelectNodes("//a[@href]");

                var hrefList = nodes.Where(node => node.GetAttributeValue("href", "").Contains("?id="))
                                   .Select(node => node.GetAttributeValue("href", "")).ToList();



                // Option 1: thread -> too complex
                // Option 2: Parallel.ForEach -> best in theory for sharding but does not work
                // Option 3: Task -> fast and easy to use
                // Option 4: TDL Block -> way too slow

                List<Task> tasks = new List<Task>();

                foreach (string appid in hrefList)
                {
                    // create a new task to process the element
                    Task task = Task.Run(async () =>
                    {
                        if (proxies.Count > 0)
                        {
                            ScrapeSingleApp ssa = new ScrapeSingleApp(appid, proxies[rand.Next(proxies.Count)]);
                            await ssa.Start();
                        }
                        else
                        {
                            ScrapeSingleApp ssa = new ScrapeSingleApp(appid);
                            await ssa.Start();
                        } 
                    });

                    tasks.Add(task);
                }

                Task.WaitAll(tasks.ToArray());
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
            }

            watch.Stop();
            Console.WriteLine($"Elapsed time: {watch.ElapsedMilliseconds} ms");
        }


        public static async Task<string> GetHTMLContent(string URL, string proxy)
        {  
            // Create a web client to download the HTML from the URL
            using (WebClient client = new())
            {
                if (proxy.Equals(""))
                {
                    return await client.DownloadStringTaskAsync(URL);
                }
                else
                {
                    client.Proxy = new WebProxy(proxy);
                    return await client.DownloadStringTaskAsync(URL);
                }
            }
        }


        static List<string> ReadProxies(string filePath)
        {
            List<string> validProxies = new List<string>();

            if (File.Exists(filePath))
            {
                string[] proxyList = File.ReadAllLines(filePath);

                if (proxyList.Length > 0)
                {
                    foreach (string proxy in proxyList)
                    {
                        if (IsValidProxyFormat(proxy))
                        {
                            validProxies.Add(proxy);
                        }
                    }
                }
            }

            return validProxies;
        }

        static bool IsValidProxyFormat(string proxy)
        {
            if (!proxy.StartsWith("http://") && !proxy.StartsWith("https://"))
            {
                return false;
            }

            if (!proxy.Contains(':') || !char.IsNumber(proxy, proxy.LastIndexOf(':') + 1))
            {
                return false;
            }

            return true;
        }

        static void configNlog()
        {
            string fileLog = Directory.GetParent(Environment.CurrentDirectory).Parent.FullName.Replace("bin", "log.txt");

            var config = new NLog.Config.LoggingConfiguration();
            var logfile = new NLog.Targets.FileTarget("logfile") { FileName = fileLog };
 
            config.AddRule(LogLevel.Info, LogLevel.Fatal, logfile);
            config.AddRule(LogLevel.Debug, LogLevel.Fatal, logfile);

            LogManager.Configuration = config;
        }

        static async Task TestFunc()
        {
            ScrapeSingleApp ssa1 = new("com.nianticlabs.pokemongo");
            ScrapeSingleApp ssa2 = new("com.king.candycrushsaga");
            await ssa1.Start();
            await ssa2.Start();
        }

    }
}