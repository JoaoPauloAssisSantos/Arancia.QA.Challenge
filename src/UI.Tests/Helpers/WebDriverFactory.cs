using System;
using System.Drawing;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;

namespace UI.Tests.Helpers
{
    public enum BrowserType
    {
        Chrome,
        Firefox
    }
    public class WebDriverOptions
    {
        public BrowserType Browser { get; set; } = BrowserType.Chrome;
        public bool Headless { get; set; } = false;
        public int ImplicitWaitSeconds { get; set; } = 5;
    }

    public static class WebDriverFactory
    {
        public static IWebDriver Create(WebDriverOptions options)
        {
            if (options == null) options = new WebDriverOptions();

            IWebDriver driver = options.Browser switch
            {
                BrowserType.Firefox => CreateFirefox(options.Headless),
                _ => CreateChrome(options.Headless)
            };

            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(options.ImplicitWaitSeconds);

            // Set a consistent viewport when running headless; otherwise maximize.
            try
            {
                if (options.Headless)
                {
                    try
                    {
                        // Preferred: use Selenium Size (requires System.Drawing)
                        driver.Manage().Window.Size = new Size(1920, 1080);
                    }
                    catch (Exception)
                    {
                        // Fallback: resize via JS if platform or System.Drawing causes issues
                        try
                        {
                            ((IJavaScriptExecutor)driver).ExecuteScript("window.resizeTo(1920,1080);");
                        }
                        catch
                        {
                            // ignore fallback failures
                        }
                    }
                }
                else
                {
                    driver.Manage().Window.Maximize();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WebDriverFactory] Warning setting window size/maximize: {ex.Message}");
            }

            return driver;
        }

        private static IWebDriver CreateChrome(bool headless)
        {
            var chromeOptions = new ChromeOptions();
            if (headless)
            {
                chromeOptions.AddArgument("--headless=new"); // or "--headless" for older versions
                chromeOptions.AddArgument("--disable-gpu");
            }
            chromeOptions.AddArgument("--no-sandbox");
            chromeOptions.AddArgument("--disable-dev-shm-usage");
            return new ChromeDriver(chromeOptions);
        }

        private static IWebDriver CreateFirefox(bool headless)
        {
            var firefoxOptions = new FirefoxOptions();
            if (headless)
            {
                firefoxOptions.AddArgument("--headless");
            }
            firefoxOptions.SetPreference("dom.webnotifications.enabled", false);
            return new FirefoxDriver(firefoxOptions);
        }
    }
}