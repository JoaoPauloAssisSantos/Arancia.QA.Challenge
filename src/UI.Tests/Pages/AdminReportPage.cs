using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using System;
using System.Globalization;
using System.Linq;

namespace UI.Tests.Pages
{
    public class AdminReportPage
    {
        private readonly IWebDriver _driver;
        private readonly WebDriverWait _wait;
        private readonly string _baseUrl;

        private static readonly By _bookingEventContent = By.CssSelector(".rbc-event-content");
        private static readonly By _calendarHeader = By.CssSelector(".rbc-toolbar-label");
        private static readonly By _nextButton = By.XPath("//span[contains(@class,'rbc-btn-group')]/button[normalize-space()='Next']");
        private static readonly By _backButton = By.XPath("//span[contains(@class,'rbc-btn-group')]/button[normalize-space()='Back']");
        private static readonly By _reportLink = By.Id("reportLink");

        public AdminReportPage(IWebDriver driver, string baseUrl, TimeSpan? timeout = null)
        {
            _driver = driver ?? throw new ArgumentNullException(nameof(driver));
            _baseUrl = baseUrl.TrimEnd('/');
            _wait = new WebDriverWait(_driver, timeout ?? TimeSpan.FromSeconds(10));
        }

        public void GoTo()
        {
            _driver.Navigate().GoToUrl($"{_baseUrl}/admin/report");
            _wait.Until(_ => ((IJavaScriptExecutor)_driver).ExecuteScript("return document.readyState")?.ToString() == "complete");
            // wait for calendar toolbar header to be visible
            _wait.Until(d =>
            {
                try
                {
                    var el = d.FindElements(_calendarHeader).FirstOrDefault(e => e.Displayed);
                    return el != null;
                }
                catch { return false; }
            });
        }

        /// <summary>
        /// Parses the current calendar month/year from the toolbar label (e.g. "April 2026").
        /// </summary>
        public (int year, int month) GetCurrentMonthYear()
        {
            var labelEl = _wait.Until(d =>
            {
                try
                {
                    return d.FindElements(_calendarHeader).FirstOrDefault(e => e.Displayed);
                }
                catch { return null; }
            });

            if (labelEl == null)
                throw new InvalidOperationException("Could not find calendar header label on Admin Report page.");

            var text = labelEl.Text.Trim(); // e.g. "April 2026"
            if (string.IsNullOrWhiteSpace(text))
                throw new InvalidOperationException("Calendar header label is empty.");

            // Expected format: "MMMM yyyy"
            if (!DateTime.TryParseExact(text, "MMMM yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                throw new InvalidOperationException($"Unable to parse calendar header '{text}' as 'MMMM yyyy'.");

            return (dt.Year, dt.Month);
        }

        /// <summary>
        /// Navigates the calendar using Next/Back until the target month/year is displayed,
        /// or until maxSteps is reached.
        /// </summary>
        public void NavigateToMonth(DateTime targetDate, int maxSteps = 24)
        {
            var targetYear = targetDate.Year;
            var targetMonth = targetDate.Month;

            for (int i = 0; i < maxSteps; i++)
            {
                var (curYear, curMonth) = GetCurrentMonthYear();
                if (curYear == targetYear && curMonth == targetMonth)
                    return;

                // decide direction: if target is after current, click Next; else click Back
                if (new DateTime(targetYear, targetMonth, 1) > new DateTime(curYear, curMonth, 1))
                {
                    // click Next
                    var nextBtn = _driver.FindElements(_nextButton).FirstOrDefault(e => e.Displayed && e.Enabled);
                    if (nextBtn == null)
                        throw new InvalidOperationException("Next button not found on calendar toolbar.");
                    nextBtn.Click();
                }
                else
                {
                    // click Back
                    var backBtn = _driver.FindElements(_backButton).FirstOrDefault(e => e.Displayed && e.Enabled);
                    if (backBtn == null)
                        throw new InvalidOperationException("Back button not found on calendar toolbar.");
                    backBtn.Click();
                }

                // small wait for UI to update the month
                _wait.Until(d =>
                {
                    try
                    {
                        var lbl = d.FindElements(_calendarHeader).FirstOrDefault(e => e.Displayed);
                        return lbl != null;
                    }
                    catch { return false; }
                });
            }

            throw new TimeoutException($"Could not navigate calendar to {targetDate:MMMM yyyy} within {maxSteps} steps.");
        }

        /// <summary>
        /// Waits for a booking event with exact text "FullName - Room: RoomName"
        /// and returns the IWebElement if found, or null on timeout.
        /// </summary>
        public IWebElement? WaitForBookingEvent(string guestFullName, string roomName, int timeoutSeconds = 20)
        {
            var expectedText = $"{guestFullName} - Room: {roomName}";
            var localWait = new WebDriverWait(_driver, TimeSpan.FromSeconds(timeoutSeconds));

            try
            {
                return localWait.Until(d =>
                {
                    try
                    {
                        var events = d.FindElements(_bookingEventContent);
                        return events.FirstOrDefault(e =>
                            e.Displayed &&
                            string.Equals(e.Text.Trim(), expectedText, StringComparison.OrdinalIgnoreCase));
                    }
                    catch
                    {
                        return null;
                    }
                });
            }
            catch
            {
                return null;
            }
        }
        public void ClickReport()
        {
            // Direct navigation to the report page
            var el = _wait.Until(ExpectedConditions.ElementIsVisible(_reportLink));
            el.Click();
        }
    }
}
