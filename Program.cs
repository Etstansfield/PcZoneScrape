using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System.Linq;
using System.Threading;
using System.Net;

namespace PcZoneScrape
{
    class Program
    {

        private static string _url = "https://www.pixsoriginadventures.co.uk/PCZone/";

        static void Main(string[] args)
        {

            Directory.CreateDirectory("PcZone");

            ChromeOptions options = new ChromeOptions();
            ChromeDriver driver = new ChromeDriver(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), options);
            WebClient myWebClient = new WebClient();
            
            try
            {
                driver.Navigate().GoToUrl(_url);
                List<string> mainLinks = GetAnchorLinks(driver);
                mainLinks.RemoveRange(0, 152);
                foreach (string item in mainLinks)
                {
                    IWebElement element = driver.FindElement(By.LinkText(item));

                    if (element == null) {
                        return;
                    }

                    CheckSubFolder(element, driver, "PcZone", myWebClient);
                    Thread.Sleep(5000);
                }

                // now we have our list of links - go over these links - go to the page - go over the links on the page - download the contents

            } catch (OpenQA.Selenium.WebDriverException err)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"+++ Error Accessing site: {err} - quiting +++");
                Console.ForegroundColor = ConsoleColor.White;
                return;
            }
            driver.Quit();
        }

        static void CheckSubFolder(IWebElement link, ChromeDriver driver, string folderPreName, WebClient myWebClient) {
            Console.WriteLine($"+++ Checking SubFolder: {link.Text} +++");

            string folderName = folderPreName + "/" + NormaliseFolderName(link.Text).Trim();

            Directory.CreateDirectory(folderName);
            link.Click();
            // directory created - now go to the page
            List<string> innerLinks = GetAnchorLinks(driver);

            // need to determine which of these are links and which are download links
            // links end in / in the href - otherwise they are download links

            foreach (string item in innerLinks)
            {
                IWebElement element = driver.FindElement(By.LinkText(item));
                if (item.EndsWith("/")) {
                    // we have a link - get the element and recursively call this function
                    if (element == null) {
                        return;
                    }

                    CheckSubFolder(element, driver, folderName, myWebClient);
                } else {
                    // construct the full uri of the resource
                    string uri = element.GetAttribute("href");
                    string fileName = uri.Split("/").Last().Replace("%20", " ");
                    Console.WriteLine($"+++ Attempting to download resource {fileName} to {folderName} +++");
                    myWebClient.DownloadFile(uri, folderName + "/" + fileName);
                }
                Thread.Sleep(2000);
            }

            // grab all of the links on the page
            

            driver.Navigate().Back();
        }

        static List<string> GetAnchorLinks(ChromeDriver driver) {
            var links = driver.FindElements(By.TagName("a"));
            List<string> filteredLinks = new List<string>();
            List<String> filterWords = new List<String>();
            filterWords.Add("Name");
            filterWords.Add("Last modified");
            filterWords.Add("Size");
            filterWords.Add("Description");
            filterWords.Add("Parent Directory");
            filterWords.Add("DVDZone");
            foreach (var item in links)
            {
                
                bool containsAnySearchString = filterWords.Any(word => item.Text.Contains(word));

                if (!containsAnySearchString) {
                    filteredLinks.Add(item.Text);
                    // System.Console.WriteLine(item.Text);
                }
            }         

            return filteredLinks;
        }

        static string NormaliseFolderName(string name) {
            string[] words = name.Split("(");

            return words[0];
        }
    }
}
