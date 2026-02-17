using OpenQA.Selenium;
using UI.Tests.Helpers;
namespace UI.Tests.Fixture;

public class WebDriverFixture : IDisposable
{
    public IWebDriver Driver { get; }
    public WebDriverFixture()
    {
        // Read env vars (optional) to choose browser/headless in CI
        var browser = Environment.GetEnvironmentVariable("UI_BROWSER")?.ToLowerInvariant() == "firefox"
            ? BrowserType.Firefox
            : BrowserType.Chrome;
        var headless = (Environment.GetEnvironmentVariable("UI_HEADLESS") ?? "true").ToLowerInvariant() == "true";
        var wait = int.TryParse(Environment.GetEnvironmentVariable("UI_IMPLICIT_WAIT"), out var w) ? w : 5;

        var options = new WebDriverOptions
        {
            Browser = browser,
            Headless = headless,
            ImplicitWaitSeconds = wait
        };

        Driver = WebDriverFactory.Create(options);
    }

    public void Dispose()
    {
        try { Driver.Quit(); } catch { /* ignore */ }
        try { Driver.Dispose(); } catch { /* ignore */ }
    }
}