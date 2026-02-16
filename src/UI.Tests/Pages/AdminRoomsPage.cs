using System;
using System.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace UI.Tests.Pages
{
    public class AdminRoomsPage
    {
        private readonly IWebDriver _driver;
        private readonly WebDriverWait _wait;
        private readonly string _baseUrl;

        // Adjust selectors based on your actual DOM
        private readonly By _pageAnchor = By.XPath("//h2[normalize-space()='Rooms']"); // or any unique element on /admin/rooms
        private readonly By _roomNameInput = By.Id("roomName");
        private readonly By _roomPriceInput = By.Id("roomPrice");
        private readonly By _typeSelect = By.Id("type");
        private readonly By _accessibleSelect = By.Id("accessible");

        private readonly By _wifiCheckbox = By.CssSelector("wifiCheckbox");
        private readonly By _tvCheckbox = By.CssSelector("tvCheckbox");
        private readonly By _radioCheckbox = By.CssSelector("radioCheckbox");
        private readonly By _refCheckbox = By.CssSelector("refCheckbox");
        private readonly By _safeCheckbox = By.CssSelector("safeCheckbox");

        private readonly By _createButton = By.Id("createRoom");

        public AdminRoomsPage(IWebDriver driver, string baseUrl, TimeSpan? timeout = null)
        {
            _driver = driver ?? throw new ArgumentNullException(nameof(driver));
            _baseUrl = baseUrl.TrimEnd('/');
            _wait = new WebDriverWait(_driver, timeout ?? TimeSpan.FromSeconds(10));
        }

        public void GoTo()
        {
            _driver.Navigate().GoToUrl($"{_baseUrl}/admin/rooms");
            // wait anchor element of /admin/rooms
            _wait.Until(d =>
            {
                try
                {
                    var el = d.FindElements(_pageAnchor).FirstOrDefault(e => e.Displayed);
                    return el != null;
                }
                catch { return false; }
            });
        }

        // Helper to find visible & enabled element
        private IWebElement Find(By by) =>
            _wait.Until(d =>
            {
                try
                {
                    var e = d.FindElement(by);
                    return (e.Displayed && e.Enabled) ? e : null;
                }
                catch { return null; }
            });

        private void SetCheckbox(By by, bool shouldBeChecked)
        {
            try
            {
                var el = _driver.FindElement(by);
                if (el.Selected != shouldBeChecked)
                    el.Click();
            }
            catch
            {
                // ignore missing checkbox in this POM
            }
        }

        public void CreateRoom(int roomNumber, int price)
        {
            // fill room#
            var roomNameEl = Find(_roomNameInput);
            roomNameEl.Clear();
            roomNameEl.SendKeys(roomNumber.ToString());

            // fill price
            var roomPriceEl = Find(_roomPriceInput);
            roomPriceEl.Clear();
            roomPriceEl.SendKeys(price.ToString());

            // select first option for Type
            var typeEl = Find(_typeSelect);
            var typeSel = new SelectElement(typeEl);
            typeSel.SelectByIndex(0);

            // select first option for Accessible
            var accEl = Find(_accessibleSelect);
            var accSel = new SelectElement(accEl);
            accSel.SelectByIndex(0);

            // room details: only WiFi
            //SetCheckbox(_wifiCheckbox, true);
            //SetCheckbox(_tvCheckbox, false);
            //SetCheckbox(_radioCheckbox, false);
            //SetCheckbox(_refCheckbox, false);
            //SetCheckbox(_safeCheckbox, false);

            // click Create
            var createEl = Find(_createButton);
            createEl.Click();
        }

        public IWebElement? WaitForRoomRow(int roomNumber, int price, int timeoutSeconds = 10)
        {
            // e.g.: p#roomName123 + p#roomPrice123 dentro de div[data-testid='roomlisting']
            var roomNameId = $"roomName{roomNumber}";
            var roomPriceId = $"roomPrice{price}";

            var rowXpath =
                $"//div[@data-testid='roomlisting' and " +
                $".//p[@id='{roomNameId}'] and " +
                $".//p[@id='{roomPriceId}']]";

            var localWait = new WebDriverWait(_driver, TimeSpan.FromSeconds(timeoutSeconds));

            try
            {
                return localWait.Until(d =>
                {
                    try
                    {
                        var rows = d.FindElements(By.XPath(rowXpath));
                        return rows.FirstOrDefault(r => r.Displayed);
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
    }
}
