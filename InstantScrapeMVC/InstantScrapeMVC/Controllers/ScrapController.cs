using InstantScrapeMVC.Helpers;
using InstantScrapeMVC.Models;
using Microsoft.AspNetCore.Mvc;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System.Text.RegularExpressions;

namespace InstantScrapeMVC.Controllers
{
    public class ScrapController : Controller
    {
        /// <summarry>
        /// Fetches a single page of Google local search results for a category and place.
        /// </summarry>
        /// <param name="model">Input query model containing category, place, and start offset.</param>
        /// <returns>A JSON payload containing scraped business records or validation/error details.</returns>
        [HttpGet]
        public async Task<JsonResult> GetAllResult([FromQuery] ScrapInputModel model)
        {
            // Validate required query parameters before opening the browser.
            if (string.IsNullOrWhiteSpace(model.Category) || string.IsNullOrWhiteSpace(model.Place))
                return new JsonResult("Category and Place must not be null!");

            var chromeOptions = new ChromeOptions();
            chromeOptions.AddArgument("--headless=new");
            //Enable while doing development to prevent CAPTCHA issue
            //chromeOptions.AddArgument("--start-maximized");
            chromeOptions.AddArgument("--disable-gpu");
            chromeOptions.AddArgument("--no-sandbox");
            chromeOptions.AddArgument("--disable-dev-shm-usage");
            chromeOptions.AddArgument("--blink-settings=imagesEnabled=false");
            // Anti-detection arguments
            chromeOptions.AddArgument("user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            chromeOptions.AddArgument("--disable-blink-features=AutomationControlled");
            chromeOptions.AddExcludedArgument("enable-automation");
            chromeOptions.AddUserProfilePreference("useAutomationExtension", false);

            using var driver = new ChromeDriver(chromeOptions);
            
            // Build Google local results search URL using requested paging offset.
            string searchUrl =
                $"https://www.google.com/search?q=best+{model.Category}+in+{model.Place}&start={model.Start}&udm=1&hl=en";

            driver.Navigate().GoToUrl(searchUrl);

            // Use explicit Selenium wait instead of fixed delay 
                // Increased to 120s to allow MANUAL CAPTCHA SOLVING by the user if needed.
                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(200));

            try
            {
                wait.Until(d => d.FindElements(By.Id("search")).Any());

                var searchContainer = driver.FindElements(By.Id("search")).FirstOrDefault();
                if (searchContainer == null)
                    return new JsonResult("No Direct Result Can Be Found!");

                var items = searchContainer.FindElements(By.CssSelector(".w7Dbne"));
                if (!items.Any())
                    return new JsonResult("No Direct Result Can Be Found!");

                List<ScrapResponseModel> responseList = new();

                // Iterate each result card and map extracted fields into response models.
                foreach (var item in items)
                {
                    string id = item.GetAttribute("id");
                    if (string.IsNullOrEmpty(id) || !id.Contains("tsuid_"))
                        continue;

                    var record = item.FindElementSafe(".VkpGBb");
                    if (record == null)
                        continue;

                    var detailContainer = record.FindElementSafe(".rllt__details");
                    var detailChildren = detailContainer?.FindElements(By.XPath("./*"));

                    //var nameElement = record.FindElementSafe(".OSrXXb");

                    //if (nameElement != null && nameElement.Displayed && nameElement.Enabled)
                    //{
                    //    try
                    //    {
                    //        ((IJavaScriptExecutor)driver)
                    //            .ExecuteScript("arguments[0].scrollIntoView({block:'center'});", nameElement);

                    //        nameElement.Click();
                    //    }
                    //    catch
                    //    {
                    //        ((IJavaScriptExecutor)driver)
                    //            .ExecuteScript("arguments[0].click();", nameElement);
                    //    }
                    //}

                    //fetch phone number after click
                    //string? phoneNumber = null;
                    //try
                    //{
                    //    var callBtn = wait.Until(d =>
                    //    {
                    //        var el = d.FindElements(By.CssSelector("a.Od1FEc.n1obkb"))
                    //                  .FirstOrDefault();
                    //        return el?.Displayed == true ? el : null;
                    //    });

                    //    phoneNumber = callBtn?.GetAttribute("data-phone-number");
                    //    if (!string.IsNullOrEmpty(phoneNumber) && phoneNumber.StartsWith("0"))
                    //    {
                    //        phoneNumber = phoneNumber.Substring(1);
                    //    }
                    //}
                    //catch { }

                    var url = record.FindElementSafe(".yYlJEf.Q7PwXb.L48Cpd.brKmxb")?.GetAttribute("href");
                    var name = record.FindElementSafe(".OSrXXb")?.Text;
                    var rating = record.FindElementSafe(".yi40Hd.YrbPuc")?.Text;
                    var reviewCount = record.FindElementSafe(".RDApEe.YrbPuc")?.Text?
                                        .TrimStart('(').TrimEnd(')');

                    var res = new ScrapResponseModel
                    {
                        Name = name,
                        Ratings = rating,
                        NumberOfReviews = reviewCount,
                        Url = url
                    };

                    // Address logic
                    int addressIndex = string.IsNullOrEmpty(rating) ? 1 : 2;
                    var rawAddress = detailChildren?.ElementAtOrDefault(addressIndex)?.Text;

                    if (!string.IsNullOrWhiteSpace(rawAddress))
                    {
                        int dotCount = rawAddress.Count(c => c == '·');
                        var parts = rawAddress
                            .Split('·', StringSplitOptions.RemoveEmptyEntries)
                            .Select(p => p.Trim())
                            .ToArray();
                        res.Address = dotCount == 2 ? res.Address = parts.Length > 1 ? parts[1] : null : res.Address = parts[0];

                        var extractedPhone = parts.LastOrDefault();
                        if (!string.IsNullOrEmpty(extractedPhone))
                        {   
                            // remove leading 0 ONLY
                            extractedPhone = extractedPhone.StartsWith("0")
                                ? extractedPhone.Substring(1)
                                : extractedPhone;

                            // remove all spaces
                            extractedPhone = extractedPhone.Replace(" ", "");
                            extractedPhone = extractedPhone.Length > 10 ? null : extractedPhone;
                            res.PhoneNumber = extractedPhone;
                        }
                    }

                    // Description extraction
                    var descNode = detailChildren?.LastOrDefault()?
                                    .FindElementSafe(".uDyWh.OSrXXb.btbrud");

                    if (descNode != null)
                    {
                        string cleaned = Regex.Replace(descNode.Text, @"[\\\""]+", "");
                        res.Description = cleaned;
                    }

                    responseList.Add(res);
                }

                return new JsonResult(responseList);
            }
            catch (Exception ex)
            {
                // Save page source for debugging
                try 
                {
                    var debugPath = Path.Combine(Directory.GetCurrentDirectory(), "error_source_getall.html");
                    System.IO.File.WriteAllText(debugPath, driver.PageSource);
                } catch { /* ignore file write errors */ }
                
                return new JsonResult($"Unexpected Exception: {ex.Message}");
            }
        }

        /// <summarry>
        /// Fetches multiple pages of Google local search results until the requested size is reached.
        /// </summarry>
        /// <param name="model">Input query model containing category, place, and total number of records to collect.</param>
        /// <returns>A JSON payload containing bulk scraped business records or validation/error details.</returns>
        [HttpGet]
        public async Task<JsonResult> GetBulkResult([FromQuery] BulkScrapInputModel model)
        {
            // Validate required query parameters before opening the browser.
            if (string.IsNullOrWhiteSpace(model.Category) || string.IsNullOrWhiteSpace(model.Place))
                return new JsonResult("Category and Place must not be null!");

            // Enforce minimum request size expected by this endpoint implementation.
            if(model.Size < 20)
                return new JsonResult("Size must be less than or equal to 20");

            var chromeOptions = new ChromeOptions();
            chromeOptions.AddArgument("--headless=new");
            //Enable while doing development to prevent CAPTCHA issue
            //chromeOptions.AddArgument("--start-maximized");
            chromeOptions.AddArgument("--disable-gpu");
            chromeOptions.AddArgument("--no-sandbox");
            chromeOptions.AddArgument("--disable-dev-shm-usage");
            chromeOptions.AddArgument("--blink-settings=imagesEnabled=false");
            // Anti-detection arguments
            chromeOptions.AddArgument("user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            chromeOptions.AddArgument("--disable-blink-features=AutomationControlled");
            chromeOptions.AddExcludedArgument("enable-automation");
            chromeOptions.AddUserProfilePreference("useAutomationExtension", false);

            using var driver = new ChromeDriver(chromeOptions);
            List<ScrapResponseModel> responseList = new();

            // Paginate through Google results in chunks of 20 until requested size is collected.
            for (int i = 0; i < model.Size; i+=20 ) //45
            {
                string searchUrl =
                $"https://www.google.com/search?q=best+{model.Category}+in+{model.Place}&start={i}&udm=1&hl=en";
                driver.Navigate().GoToUrl(searchUrl);

                // Use explicit Selenium wait instead of fixed delay
                // Increased to 120s to allow MANUAL CAPTCHA SOLVING by the user if needed.
                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(200));

                try
                {
                    wait.Until(d => d.FindElements(By.Id("search")).Any());

                    var searchContainer = driver.FindElements(By.Id("search")).FirstOrDefault();
                    if (searchContainer == null)
                        return new JsonResult("No Direct Result Can Be Found!");

                    var items = searchContainer.FindElements(By.CssSelector(".w7Dbne"));
                    if (!items.Any())
                        return new JsonResult("No Direct Result Can Be Found!");

                    // Parse each result entry on the current page.
                    foreach (var item in items)
                    {
                        if (responseList.Count >= model.Size) break;
                        string id = item.GetAttribute("id");
                        if (string.IsNullOrEmpty(id) || !id.Contains("tsuid_"))
                            continue;

                        var record = item.FindElementSafe(".VkpGBb");
                        if (record == null)
                            continue;

                        var detailContainer = record.FindElementSafe(".rllt__details");
                        var detailChildren = detailContainer?.FindElements(By.XPath("./*"));

                        var url = record.FindElementSafe(".yYlJEf.Q7PwXb.L48Cpd.brKmxb")?.GetAttribute("href");
                        var name = record.FindElementSafe(".OSrXXb")?.Text;
                        var rating = record.FindElementSafe(".yi40Hd.YrbPuc")?.Text;
                        var reviewCount = record.FindElementSafe(".RDApEe.YrbPuc")?.Text?
                                            .TrimStart('(').TrimEnd(')');

                        var res = new ScrapResponseModel
                        {
                            Name = name,
                            Ratings = rating,
                            NumberOfReviews = reviewCount,
                            Url = url
                        };

                        // Address logic
                        int addressIndex = string.IsNullOrEmpty(rating) ? 1 : 2;
                        var rawAddress = detailChildren?.ElementAtOrDefault(addressIndex)?.Text;

                        if (!string.IsNullOrWhiteSpace(rawAddress))
                        {
                            int dotCount = rawAddress.Count(c => c == '·');
                            var parts = rawAddress
                                .Split('·', StringSplitOptions.RemoveEmptyEntries)
                                .Select(p => p.Trim())
                                .ToArray();
                            res.Address = dotCount == 2 ? res.Address = parts.Length > 1 ? parts[1] : null : res.Address = parts[0];

                            var extractedPhone = parts.LastOrDefault();
                            if (!string.IsNullOrEmpty(extractedPhone))
                            {
                                // remove leading 0 ONLY
                                extractedPhone = extractedPhone.StartsWith("0")
                                    ? extractedPhone.Substring(1)
                                    : extractedPhone;

                                // remove all spaces
                                extractedPhone = extractedPhone.Replace(" ", "");
                                extractedPhone = extractedPhone.Length > 10 ? null : extractedPhone;
                                res.PhoneNumber = extractedPhone;
                            }
                        }

                        // Description extraction
                        var descNode = detailChildren?.LastOrDefault()?
                                        .FindElementSafe(".uDyWh.OSrXXb.btbrud");

                        if (descNode != null)
                        {
                            string cleaned = Regex.Replace(descNode.Text, @"[\\\""]+", "");
                            res.Description = cleaned;
                        }

                        responseList.Add(res);
                    }
                }
                catch (Exception ex)
                {
                    // Save page source for debugging
                    try 
                    {
                        var debugPath = Path.Combine(Directory.GetCurrentDirectory(), $"error_source_bulk_{i}.html");
                        System.IO.File.WriteAllText(debugPath, driver.PageSource);
                    } catch { /* ignore file write errors */ }

                    return new JsonResult($"Unexpected Exception: {ex.Message}");
                }
            }

            return new JsonResult(responseList);
        }
    }
}
