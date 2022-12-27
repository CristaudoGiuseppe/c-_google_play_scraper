# Google Play Scraper
Google Play Scraper is a web scraper that extracts information about apps from the Google Play Store and saves the information as JSON files.

## Requirements
* .NET Core 3.1
*  NLog 4.7.1
* HtmlAgilityPack 1.11.30
* Newtonsoft.Json 12.0.3

## Usage
To use the web scraper, download or clone the repository and navigate to the root directory of the project. Then, run the following command in the terminal:

```console
	dotnet run
```
By default, the web scraper will scrape information about all the apps on the first page of the Google Play Store's app listings. The scraped data will be saved as JSON files in a directory called output within the root directory of the project.

## Configuration
You can configure the web scraper by modifying the following variables in the Program class:

**proxiesPath**: The path to a text file containing a list of proxy servers to use when making HTTP requests. If this variable is left blank or the text file does not exist, the web scraper will not use a proxy server.
**urlPlayStore**: The URL of the page on the Google Play Store that you want to scrape. By default, this is set to the URL of the first page of the app listings.


##Dependencies
Google Play Scraper relies on the following libraries:

* .NET Core: A free, open-source, cross-platform framework for building applications.
* NLog: A logging library for .NET that allows you to write log messages to various outputs (e.g., console, file, database).
* HtmlAgilityPack: A library that provides an HTML document object model (DOM) and allows you to manipulate and traverse HTML documents.
* Newtonsoft.Json: A library that allows you to serialize and deserialize JSON objects.

Here is a list of the methods in the **Program** class:

* **Main**: The entry point for the application. It sets up a logger, reads in a list of proxies from a text file, makes an HTTP request to a specified URL using a random proxy from the list (or no proxy if the list is empty), processes the HTML content of the response to extract a list of app IDs, and creates a list of tasks to asynchronously scrape each app using the ScrapeSingleApp class. It then waits for all the tasks to complete and prints the elapsed time of the method to the console.
* **configNlog**: A helper method that configures the logger object by specifying the output targets and formatting for log messages.
* **ReadProxies**: A method that reads a list of proxies from a text file and returns the list as a List<string>.
* **GetHTMLContent**: An async method that makes an HTTP request to a specified URL using a specified proxy (or no proxy if the proxy is an empty string) and returns the HTML content of the response as a string.

Here is a list of the methods in the **ScrapeSingleApp** class:

* **ScrapeSingleApp**: A constructor that initializes the FileName, URL, proxy, name, category, stars, and VersioneCorrente variables with the provided arguments (or default values if they are not provided).
* **Start**: An async method that starts the web scraping process by calling the ScrapeHtml method and then saves the scraped data as a JSON file by calling the SaveJson method.
* **ScrapeHtml**: An async method that retrieves the HTML content of an app's page on the Google Play Store using the GetHTMLContent method and then uses various techniques to extract specific information from the HTML content, including using XPath queries, regular expressions, and DOM traversal.
* **SaveJson**: A method that creates a dictionary with keys and values that correspond to the app's name


##Credits
Google Play Scraper was created by Criss.

## License
Google Play Scraper is licensed under the MIT License. See the LICENSE file for details.