using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;

public class AdminAuthPage
{
    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;
    private readonly string _baseUrl;
    private readonly TimeSpan _timeout = TimeSpan.FromSeconds(10);

    private readonly By _usernameInput = By.Id("username");
    private readonly By _passwordInput = By.Id("password");
    private readonly By _loginButton = By.Id("doLogin");
    private readonly By _logoutButton = By.CssSelector("button.btn.btn-outline-danger");


    public AdminAuthPage(IWebDriver driver, string baseUrl, TimeSpan? timeout = null)
    {
        _driver = driver ?? throw new ArgumentNullException(nameof(driver));
        _baseUrl = (baseUrl ?? throw new ArgumentNullException(nameof(baseUrl))).TrimEnd('/');
        _wait = new WebDriverWait(_driver, timeout ?? _timeout);
    }
    public void GoToLogin()
    {
        // adjust path if your admin login is at a different URL
        _driver.Navigate().GoToUrl($"{_baseUrl}/admin");
    }

    public void LoginAsAdmin()
    {
        // optional: navigate to login page first
        GoToLogin();

        // wait & fill username
        var userEl = _wait.Until(d =>
        {
            try
            {
                var e = d.FindElement(_usernameInput);
                return (e.Displayed && e.Enabled) ? e : null;
            }
            catch { return null; }
        });
        userEl.Clear();
        userEl.SendKeys("admin");

        // wait & fill password
        var passEl = _wait.Until(d =>
        {
            try
            {
                var e = d.FindElement(_passwordInput);
                return (e.Displayed && e.Enabled) ? e : null;
            }
            catch { return null; }
        });
        passEl.Clear();
        passEl.SendKeys("password");

        // click Login
        var btn = _wait.Until(d =>
        {
            try
            {
                var e = d.FindElement(_loginButton);
                return (e.Displayed && e.Enabled) ? e : null;
            }
            catch { return null; }
        });
        btn.Click();
    }
    public void Login(string username, string password)
    {
        GoToLogin();

        var userEl = _wait.Until(d =>
        {
            try
            {
                var e = d.FindElement(_usernameInput);
                return (e.Displayed && e.Enabled) ? e : null;
            }
            catch { return null; }
        });
        userEl.Clear();
        userEl.SendKeys(username);

        var passEl = _wait.Until(d =>
        {
            try
            {
                var e = d.FindElement(_passwordInput);
                return (e.Displayed && e.Enabled) ? e : null;
            }
            catch { return null; }
        });
        passEl.Clear();
        passEl.SendKeys(password);

        var btn = _wait.Until(d =>
        {
            try
            {
                var e = d.FindElement(_loginButton);
                return (e.Displayed && e.Enabled) ? e : null;
            }
            catch { return null; }
        });
        btn.Click();
    }

    public void Logout()
    {
        // waits until the logout button is visible and clicks it
        var btn = _wait.Until(d =>
        {
            try
            {
                var candidates = d.FindElements(_logoutButton);
                return candidates.FirstOrDefault(e =>
                    e.Displayed &&
                    e.Text.Trim().Equals("Logout", StringComparison.OrdinalIgnoreCase));
            }
            catch { return null; }
        });

        if (btn == null)
            throw new InvalidOperationException("Logout button not found on admin page.");

        btn.Click();
    }
}