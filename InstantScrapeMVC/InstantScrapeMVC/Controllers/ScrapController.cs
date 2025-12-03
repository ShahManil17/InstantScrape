using InstantScrapeMVC.Models;
using Microsoft.AspNetCore.Mvc;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium;
using System.Text.RegularExpressions;
using OpenQA.Selenium.Support.UI;
using InstantScrapeMVC.Helpers;

namespace InstantScrapeMVC.Controllers
{
    public class ScrapController : Controller
    {
        [HttpGet]
        public async Task<JsonResult> GetAllResult([FromQuery] ScrapInputModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Category) || string.IsNullOrWhiteSpace(model.Place))
                return new JsonResult("Category and Place must not be null!");

            var chromeOptions = new ChromeOptions();
            chromeOptions.AddArgument("headless");

            using var driver = new ChromeDriver(chromeOptions);

            string searchUrl =
                $"https://www.google.com/search?q=best+{model.Category}+in+{model.Place}&tbm=lcl";

            driver.Navigate().GoToUrl(searchUrl);

            // Use explicit Selenium wait instead of fixed delay
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(5));
            wait.Until(d => d.FindElements(By.Id("search")).Any());

            try
            {
                var searchContainer = driver.FindElements(By.Id("search")).FirstOrDefault();
                if (searchContainer == null)
                    return new JsonResult("No Direct Result Can Be Found!");

                var items = searchContainer.FindElements(By.CssSelector(".w7Dbne"));
                if (!items.Any())
                    return new JsonResult("No Direct Result Can Be Found!");

                List<ScrapResponseModel> responseList = new();

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
                    res.Address = detailChildren?.ElementAtOrDefault(addressIndex)?.Text;

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
                return new JsonResult($"Unexpected Exception: {ex.Message}");
            }
        }
    }
}
