using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;

public class RoomPage
{
    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;
    private readonly string _baseUrl;
    private readonly TimeSpan _timeout = TimeSpan.FromSeconds(10);

    public RoomPage(IWebDriver driver, string baseUrl, TimeSpan? timeout = null)
    {
        _driver = driver ?? throw new ArgumentNullException(nameof(driver));
        _baseUrl = (baseUrl ?? throw new ArgumentNullException(nameof(baseUrl))).TrimEnd('/');
        _wait = new WebDriverWait(_driver, timeout ?? _timeout);
    }
    public void ClickReserveNow(int timeoutSeconds = 10)
    {
        var by = By.CssSelector("button#doReservation");
        var wait = new OpenQA.Selenium.Support.UI.WebDriverWait(_driver, TimeSpan.FromSeconds(timeoutSeconds));

        try
        {
            // wait element exists + visible
            var el = wait.Until(d =>
            {
                try
                {
                    var e = d.FindElement(by);
                    return (e.Displayed && e.Enabled) ? e : null;
                }
                catch { return null; }
            });

            if (el == null) throw new InvalidOperationException("Reserve Now button not found or not visible.");

            // scroll element into center of viewport to avoid sticky overlays
            ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].scrollIntoView({block: 'center'});", el);
            System.Threading.Thread.Sleep(200); // short pause for layout

            try
            {
                // try normal click
                el.Click();
                return;
            }
            catch (OpenQA.Selenium.ElementClickInterceptedException)
            {
                // fallback: try Actions move+click
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
            // diagnostic: screenshot + rethrow
            try
            {
                var ss = ((ITakesScreenshot)_driver).GetScreenshot();
                var file = $"click_reserve_failed_{DateTime.UtcNow:yyyyMMdd_HHmmss}.png";
                System.IO.File.WriteAllBytes(file, ss.AsByteArray);
                Console.WriteLine($"ClickReserveNow failed: {ex.Message}. Screenshot: {file}. URL: {_driver.Url}");
            }
            catch { /* ignore */ }

            throw;
        }
    }


    // inside HomePage class (uses _wait and _driver fields)
    public Contact FillContactFormAndReturnData(int timeoutSeconds = 10)
    {
        var contact = ContactFactory.CreateRandomContact();

        var firstSel = By.CssSelector("input.room-firstname[name='firstname'], input.form-control.room-firstname[name='firstname']");
        var lastSel = By.CssSelector("input.room-lastname[name='lastname'], input.form-control.room-lastname[name='lastname']");
        var emailSel = By.CssSelector("input.room-email[name='email'], input.form-control.room-email[name='email']");
        var phoneSel = By.CssSelector("input.room-phone[name='phone'], input.form-control.room-phone[name='phone']");

        var wait = new OpenQA.Selenium.Support.UI.WebDriverWait(_driver, TimeSpan.FromSeconds(timeoutSeconds));

        // helper to find and fill
        IWebElement FindAndClearSend(By by, string value)
        {
            var el = wait.Until(d =>
            {
                try
                {
                    var e = d.FindElement(by);
                    return (e.Displayed && e.Enabled) ? e : null;
                }
                catch { return null; }
            });
            el.Clear();
            el.SendKeys(value);
            return el;
        }

        FindAndClearSend(firstSel, contact.FirstName);
        FindAndClearSend(lastSel, contact.LastName);
        FindAndClearSend(emailSel, contact.Email);
        FindAndClearSend(phoneSel, contact.Phone);

        return contact;
    }
    // Fill the contact form with the provided Contact object.
    // Uses explicit waits and clears each field before typing.
    public void FillContactForm(Contact contact, int timeoutSeconds = 10)
    {
        if (contact is null) throw new ArgumentNullException(nameof(contact));

        // CSS selectors for the inputs (adjust if your DOM differs)
        var firstSel = By.CssSelector("input.room-firstname[name='firstname'], input.form-control.room-firstname[name='firstname']");
        var lastSel = By.CssSelector("input.room-lastname[name='lastname'], input.form-control.room-lastname[name='lastname']");
        var emailSel = By.CssSelector("input.room-email[name='email'], input.form-control.room-email[name='email']");
        var phoneSel = By.CssSelector("input.room-phone[name='phone'], input.form-control.room-phone[name='phone']");

        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(timeoutSeconds));

        // Helper: wait for element, clear it, and send value
        IWebElement FindAndClearSend(By by, string value)
        {
            var el = wait.Until(d =>
            {
                try
                {
                    var e = d.FindElement(by);
                    return (e.Displayed && e.Enabled) ? e : null;
                }
                catch
                {
                    return null;
                }
            });
            el.Clear();
            el.SendKeys(value ?? string.Empty);
            return el;
        }

        // Fill fields
        FindAndClearSend(firstSel, contact.FirstName);
        FindAndClearSend(lastSel, contact.LastName);
        FindAndClearSend(emailSel, contact.Email);
        FindAndClearSend(phoneSel, contact.Phone);
    }
    // Collect visible validation/error messages on the page.
    // Returns distinct, visible messages from common alert/inline selectors
    // and falls back to HTML5 input.validationMessage for inputs.
    public IReadOnlyCollection<string> GetVisibleValidationMessages(int timeoutSeconds = 3)
    {
        var msgs = new List<string>();

        try
        {
            // Common selectors for inline or server-side error messages
            var selectors = new[] { ".invalid-feedback", ".text-danger", ".form-error", ".alert-danger" };

            foreach (var sel in selectors)
            {
                var elements = _driver.FindElements(By.CssSelector(sel));
                foreach (var e in elements)
                {
                    if (e.Displayed && !string.IsNullOrWhiteSpace(e.Text))
                        msgs.Add(e.Text.Trim());
                }
            }

            // Fallback: check HTML5 validation messages on inputs (browser native)
            var inputSelectors = new[]
            {
            "input.room-firstname[name='firstname'], input.form-control.room-firstname[name='firstname']",
            "input.room-lastname[name='lastname'], input.form-control.room-lastname[name='lastname']",
            "input.room-phone[name='phone'], input.form-control.room-phone[name='phone']",
            "input.room-email[name='email'], input.form-control.room-email[name='email']"
        };

            foreach (var sel in inputSelectors)
            {
                try
                {
                    var el = _driver.FindElement(By.CssSelector(sel));
                    var validation = ((IJavaScriptExecutor)_driver).ExecuteScript("return arguments[0].validationMessage;", el) as string;
                    if (!string.IsNullOrWhiteSpace(validation))
                        msgs.Add(validation.Trim());
                }
                catch
                {
                    // ignore missing inputs
                }
            }
        }
        catch
        {
            // swallow exceptions here — caller will handle empty result as "no visible messages"
        }

        return msgs.Distinct().ToList().AsReadOnly();
    }

    public void ClickReserveNowForBooking(int timeoutSeconds = 10)
    {
        ClickButtonByText("Reserve Now", timeoutSeconds);
    }

    public void ClickCancelBooking(int timeoutSeconds = 10)
    {
        ClickButtonByText("Cancel", timeoutSeconds);
    }

    private void ClickButtonByText(string buttonText, int timeoutSeconds = 10)
    {
        var xpath = $"//button[normalize-space() = '{buttonText}']";
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
                catch { return null; }
            });

            if (el == null) throw new InvalidOperationException($"Button '{buttonText}' not found or not clickable.");

            ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].scrollIntoView({block: 'center'});", el);
            System.Threading.Thread.Sleep(150);

            try
            {
                el.Click();
                return;
            }
            catch (OpenQA.Selenium.ElementClickInterceptedException)
            {
                try
                {
                    var actions = new OpenQA.Selenium.Interactions.Actions(_driver);
                    actions.MoveToElement(el).Click().Perform();
                    return;
                }
                catch
                {
                    ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", el);
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            try
            {
                var ss = ((ITakesScreenshot)_driver).GetScreenshot();
                var file = $"click_{buttonText.Replace(" ", "_")}_failed_{DateTime.UtcNow:yyyyMMdd_HHmmss}.png";
                System.IO.File.WriteAllBytes(file, ss.AsByteArray);
                Console.WriteLine($"Click '{buttonText}' failed: {ex.Message}. Screenshot saved to {file}. URL: {_driver.Url}");
            }
            catch { /* ignore */ }

            throw;
        }
    }
    }