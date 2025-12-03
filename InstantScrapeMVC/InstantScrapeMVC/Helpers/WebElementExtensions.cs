using OpenQA.Selenium;

namespace InstantScrapeMVC.Helpers
{
    public static class WebElementExtensions
    {
        public static IWebElement FindElementSafe(this IWebElement element, string cssSelector)
        {
            try { return element.FindElement(By.CssSelector(cssSelector)); }
            catch { return null; }
        }

        public static IWebElement FindElementSafe(this IWebDriver driver, string cssSelector)
        {
            try { return driver.FindElement(By.CssSelector(cssSelector)); }
            catch { return null; }
        }
    }
}