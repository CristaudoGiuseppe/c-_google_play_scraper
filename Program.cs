using HtmlAgilityPack;
using System.Net;
using NLog;
using System.Text.RegularExpressions;
using System.Threading.Tasks;


namespace MyAppFree
{
    class Program
    {
        // Declare variables
        public static Logger logger = LogManager.GetCurrentClassLogger(); // used to log main operations and errors
        private static string proxiesPath = Directory.GetParent(Environment.CurrentDirectory).Parent.FullName.Replace(@"bin\", "proxies.txt");
        private static string urlPlayStore = "https://play.google.com/store/apps?hl=it&gl=US";
        private static List<string> categories = new List<string>() { "GAME_CASUAL" , "GAME_ACTION", "GAME_ARCADE", "GAME_CARD", "PRODUCTIVITY", "HEALTH_AND_FITNESS", "GAME_CASINO", "MUSIC_AND_AUDIO", "EDUCATION" };
        private static List<string> proxies = new List<string>();


        // The Main method serves as the entry point for the application. It starts by setting up the configuration for a Logger object
        // logger which is used to log main operations and errors. It then reads in a list of proxies from a text file and uses one of
        // the proxies (or none if the list is empty) to make an HTTP request to a specified URL.
        // The HTML content obtained from the HTTP request is then processed to extract a list of "href" attributes from certain "a" elements
        // in the HTML. The method then creates a list of Task objects which asynchronously scrape each app in the list using the
        // ScrapeSingleApp class.
        // Finally, it waits for all the tasks to complete and prints the elapsed time of the method to the console.
        static async Task Main(string[] args)
        {

            var watch = System.Diagnostics.Stopwatch.StartNew();
            
            // Declare variables
            Random rand = new Random();

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
            }

            await ScrapeAppsFromCategories(rand);

            watch.Stop();
            Console.WriteLine($"Elapsed time: {watch.ElapsedMilliseconds} ms");
        }

        private static async Task ScrapeAppsFromCategories(Random _rand)
        {
            try
            {
                List<Task> tasks_main = new List<Task>();

                foreach (string category in categories)
                {
                    Task task = Task.Run(async () =>
                    {
                        string categoryURL = "https://play.google.com/store/apps/category/" + category + "?hl=it&gl=US";
                        if (proxies.Count > 0)
                        {
                            ScrapeApps sa = new ScrapeApps(categoryURL, proxies[_rand.Next(proxies.Count)]);
                            await sa.ScrapeAppsFromURL();
                            List<string> result = sa.GetApps();
                            await GetAppsInfoParallel(result, _rand);
                        }
                        else
                        {
                            ScrapeApps sa = new ScrapeApps(categoryURL);
                            await sa.ScrapeAppsFromURL();
                            List<string> result = sa.GetApps();
                            await GetAppsInfoParallel(result, _rand);
                        }
                    });

                    tasks_main.Add(task);
                }

                Task.WaitAll(tasks_main.ToArray());
            } catch (Exception ex)
            {
                Console.WriteLine("Error scraping APP IDs from categories");
                logger.Error("Error scraping APP IDs from categories: " + ex.Message);
            }

        }

        public static async Task GetAppsInfoParallel(List<string> apps, Random _rand)
        {
            List<Task> tasks = new List<Task>();

            // Split the list of apps into chunks for parallel processing
            int chunkSize = (int)Math.Ceiling((double)apps.Count / Environment.ProcessorCount);
            List<List<string>> appChunks = Enumerable.Range(0, apps.Count)
                .GroupBy(i => i / chunkSize)
                .Select(g => g.Select(i => apps[i]).ToList())
                .ToList();

            Parallel.ForEach(appChunks, appChunk =>
            {
                foreach (string app in appChunk)
                {
                    // create a new task to process the element
                    Task task = Task.Run(async () =>
                    {
                        if (proxies.Count > 0)
                        {
                            ScrapeSingleApp ssa = new ScrapeSingleApp(app, proxies[_rand.Next(proxies.Count)]);
                            await ssa.Start();
                        }
                        else
                        {
                            ScrapeSingleApp ssa = new ScrapeSingleApp(app);
                            await ssa.Start();
                        }
                    });

                    tasks.Add(task);
                }
            });

            Task.WaitAll(tasks.ToArray());
        }

        public static async Task GetAppsInfo(List<string> apps, Random _rand)
        {
            //// Option 1: thread -> too complex
            //// Option 2: Parallel.ForEach -> best in theory for sharding but does not work
            //// Option 3: Task -> fast and easy to use
            //// Option 4: TDL Block -> way too slow

            List<Task> tasks = new List<Task>();

            foreach (string app in apps)
            {
                // create a new task to process the element
                Task task = Task.Run(async () =>
                {
                    if (proxies.Count > 0)
                    {
                        ScrapeSingleApp ssa = new ScrapeSingleApp(app, proxies[_rand.Next(proxies.Count)]);
                        await ssa.Start();
                    }
                    else
                    {
                        ScrapeSingleApp ssa = new ScrapeSingleApp(app);
                        await ssa.Start();
                    }
                });

                tasks.Add(task);
            }

            Task.WaitAll(tasks.ToArray());
        }

        // The GetHTMLContent method is an asynchronous method which returns the HTML content of a given URL as the result of a Task<string>.
        // It takes in two parameters: a string called URL which is the URL to retrieve the HTML content from, and a string called proxy which
        // is the address of a proxy to use for the HTTP request. If the proxy parameter is an empty string, the method makes an HTTP request to the
        // URL without using a proxy. Otherwise, it sets the proxy for a WebClient object and makes an HTTP request to the URL using the specified proxy.
        // The HTML content obtained from the HTTP request is then returned as the result of the Task.
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

        // The ReadProxies method takes in a file path as a string parameter and returns a List<string> containing 
        // the lines of the text file.If an error occurs while reading the file, it throws an exception.
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


        // The configNlog method sets up the configuration for the logger object. It is not clear from the code what specific configurations are being set up.
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