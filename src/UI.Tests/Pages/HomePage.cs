using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;

public class HomePage
{
    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;
    private readonly string _baseUrl;
    private readonly TimeSpan _timeout = TimeSpan.FromSeconds(10);
    // Selectors centralized
    private const string HeaderSelector = "navbarNav";
    private const string BookingFormId = "booking-form";
    private const string FirstNameSelector = "input[name='firstname']";
    private const string LastNameSelector = "input[name='lastname']";
    private const string SubmitButtonSelector = "button[type='submit']";
    private const string BookNowButtonSelector = "a.btn.btn-primary.btn-lg[href=\"#booking\"]";

    public HomePage(IWebDriver driver, string baseUrl, TimeSpan? timeout = null)
    {
        _driver = driver ?? throw new ArgumentNullException(nameof(driver));
        _baseUrl = (baseUrl ?? throw new ArgumentNullException(nameof(baseUrl))).TrimEnd('/');
        _wait = new WebDriverWait(_driver, timeout ?? _timeout);
    }

    public void GoTo(string path = "")
    {
        var url = string.IsNullOrEmpty(path) ? _baseUrl : $"{_baseUrl}/{path.TrimStart('/')}";
        _driver.Navigate().GoToUrl(url);
    }

    public bool IsLoaded()
    {
        try
        {
            GoTo();
            var el = _wait.Until(d =>
            {
                try
                {
                    var e = d.FindElement(By.Id(HeaderSelector));
                    return (e.Displayed) ? e : null;
                }
                catch
                {
                    return null;
                }
            });
            return el != null && el.Displayed;
        }
        catch (Exception ex)
        {
            try
            {
                var ss = ((ITakesScreenshot)_driver).GetScreenshot();
                var file = $"homepage_failed{DateTime.UtcNow:yyyyMMdd_HHmmss}.png";
                System.IO.File.WriteAllBytes(file, ss.AsByteArray);
                Console.WriteLine($"IsLoaded failed: {ex.Message}. Screenshot saved to {file}. Current URL: {_driver.Url}");
            }
            catch (Exception ex2)
            {
                Console.WriteLine($"IsLoaded failed and screenshot capture also failed: {ex.Message}; screenshot error: {ex2.Message}");
            }

            return false;
        }
    }

    public void ClickBookNow(int timeoutSeconds = 10)
    {

    var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(timeoutSeconds));
        var el = wait.Until(d =>
        {
            try
            {
                var e = d.FindElement(By.CssSelector(BookNowButtonSelector));
                return (e.Displayed && e.Enabled) ? e : null;
            }
            catch
            {
                return null;
            }
        });
        el.Click();
    }


    // Wait and return header text (safe accessor)
    public string GetHeaderText()
    {
        var el = _wait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector(HeaderSelector)));
        return el.Text ?? string.Empty;
    }

    public bool IsBookingFormPresent()
    {
        try
        {
            _wait.Until(ExpectedConditions.ElementExists(By.Id(BookingFormId)));
            return true;
        }
        catch
        {
            return false;
        }
    }

    // Click submit and optionally wait for a post-submit condition (locator or timeout)
    public bool SubmitBooking(By? postSubmitCondition = null, int waitSeconds = 10)
    {
        try
        {
            var btn = _wait.Until(ExpectedConditions.ElementToBeClickable(By.CssSelector(SubmitButtonSelector)));
            btn.Click();

            if (postSubmitCondition is not null)
            {
                var postWait = new WebDriverWait(_driver, TimeSpan.FromSeconds(waitSeconds));
                postWait.Until(ExpectedConditions.ElementExists(postSubmitCondition));
            }

            return true;
        }
        catch
        {
            return false;
        }
    }
    public void ClickBookNowForRoom(string roomName, int timeoutSeconds = 10)
    {
        // XPath locates the card with the title text and its "Book now" link
        var xpath = $"//div[contains(@class,'card') and .//h5[contains(@class,'card-title') and normalize-space(.)='{roomName}']]//a[contains(@class,'btn') and normalize-space(.)='Book now']";
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(timeoutSeconds));
        try
        {
            var el = wait.Until(d =>
            {
                try
                {
                    var e = d.FindElement(By.XPath(xpath));
                    return (e.Displayed && e.Enabled) ? e : null;
                }
                catch
                {
                    return null;
                }
            });

            if (el == null)
                throw new InvalidOperationException($"Book now button for room '{roomName}' not found.");

            // Scroll element into center of viewport to avoid overlays
            ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].scrollIntoView({block: 'center'});", el);
            System.Threading.Thread.Sleep(150); // short pause to allow layout

            try
            {
                // normal click attempt
                el.Click();
                return;
            }
            catch (OpenQA.Selenium.ElementClickInterceptedException)
            {
                // fallback: action move + click
                try
                {
                    var actions = new OpenQA.Selenium.Interactions.Actions(_driver);
                    actions.MoveToElement(el).Click().Perform();
                    return;
                }
                catch
                {
                    // final fallback: JS click
                    ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", el);
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            // diagnostic: screenshot + rethrow to surface failure
            try
            {
                var ss = ((ITakesScreenshot)_driver).GetScreenshot();
                var file = $"click_booknow_failed_{DateTime.UtcNow:yyyyMMdd_HHmmss}.png";
                System.IO.File.WriteAllBytes(file, ss.AsByteArray);
                Console.WriteLine($"ClickBookNowForRoom failed: {ex.Message}. Screenshot saved to {file}. URL: {_driver.Url}");
            }
            catch { /* ignore screenshot errors */ }

            throw;
        }
    }

    public (string checkin, string checkout) SetBookingDates(DateTime? checkin = null, DateTime? checkout = null, int timeoutSeconds = 5)
    {
        // default: tomorrow -> +2 nights
        var ci = checkin ?? DateTime.UtcNow.Date.AddDays(1);
        var co = checkout ?? ci.AddDays(2);
        // format as displayed in the page (example: "16/02/2026")
        string fmt = "dd/MM/yyyy";
        var ciStr = ci.ToString(fmt);
        var coStr = co.ToString(fmt);

        // selectors for the inputs — adjust if your inputs have name/id attributes
        var checkinBy = By.CssSelector("input[name='checkin'], input.form-control[placeholder='Check In'], .react-datepicker__input-container input");
        var checkoutBy = By.CssSelector("input[name='checkout'], input.form-control[placeholder='Check Out'], .react-datepicker__input-container input");

        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(timeoutSeconds));

        try
        {
            // find inputs (choose first matching pair: checkin -> first, checkout -> second)
            var inputs = wait.Until(d =>
            {
                try
                {
                    var els = d.FindElements(By.CssSelector(".react-datepicker__input-container input, input.form-control"));
                    return (els != null && els.Count >= 2) ? els : null;
                }
                catch { return null; }
            });

            var checkinEl = inputs[0];
            var checkoutEl = inputs[1];

            // set value via JS and dispatch input/change events (works with custom datepickers)
            var js = (IJavaScriptExecutor)_driver;
            js.ExecuteScript("arguments[0].value = arguments[1]; arguments[0].dispatchEvent(new Event('input', { bubbles: true })); arguments[0].dispatchEvent(new Event('change', { bubbles: true }));", checkinEl, ciStr);
            js.ExecuteScript("arguments[0].value = arguments[1]; arguments[0].dispatchEvent(new Event('input', { bubbles: true })); arguments[0].dispatchEvent(new Event('change', { bubbles: true }));", checkoutEl, coStr);

            // small wait to let any UI update
            System.Threading.Thread.Sleep(200);

            // optionally verify values applied
            var appliedCi = checkinEl.GetAttribute("value") ?? checkinEl.GetAttribute("value");
            var appliedCo = checkoutEl.GetAttribute("value") ?? checkoutEl.GetAttribute("value");

            var appliedCiNonNull = appliedCi ?? ciStr;
            var appliedCoNonNull = appliedCo ?? coStr;
            return (appliedCiNonNull, appliedCoNonNull);
        }
        catch (Exception ex)
        {
            try
            {
                var ss = ((ITakesScreenshot)_driver).GetScreenshot();
                var file = $"set_dates_failed_{DateTime.UtcNow:yyyyMMdd_HHmmss}.png";
                System.IO.File.WriteAllBytes(file, ss.AsByteArray);
                Console.WriteLine($"SetBookingDates failed: {ex.Message}. Screenshot: {file}. URL: {_driver.Url}");
            }
            catch { /* ignore */ }
            throw;
        }
    }
    // Utility: take screenshot (caller responsible for saving path)
    public Screenshot TakeScreenshot() => ((ITakesScreenshot)_driver).GetScreenshot();
}