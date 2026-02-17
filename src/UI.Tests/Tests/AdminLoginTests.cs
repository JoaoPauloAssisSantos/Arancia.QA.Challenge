using System;
using System.Linq;
using FluentAssertions;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using UI.Tests.Fixture;
using Xunit;
using Xunit.Abstractions;

public class AdminLoginTests : IClassFixture<WebDriverFixture>
{
    private readonly WebDriverFixture _fix;
    private readonly ITestOutputHelper _output;
    private readonly AdminAuthPage _login;
    private readonly string _baseUrl = "https://automationintesting.online";

    public AdminLoginTests(WebDriverFixture fix, ITestOutputHelper output)
    {
        _fix = fix ?? throw new ArgumentNullException(nameof(fix));
        _output = output ?? throw new ArgumentNullException(nameof(output));
        _login = new AdminAuthPage(_fix.Driver, "https://automationintesting.online");
    }

    [Fact(DisplayName = "UI-06 - Admin login with valid credentials")]
    public void AdminLogin_WithValidCredentials_ShowsAdminDashboard()
    {
        // Arrange
        _login.GoToLogin();

        // Act
        _login.LoginAsAdmin(); // uses admin / password

        // Assert
        var wait = new WebDriverWait(_fix.Driver, TimeSpan.FromSeconds(10));

        // Wait until either Rooms and Report menus are visible,
        // or we hit timeout (which will throw)
        IWebElement roomsMenu = wait.Until(d =>
        {
            try
            {
                var el = d.FindElements(By.XPath("//a[normalize-space()='Rooms']"))
                          .FirstOrDefault(e => e.Displayed);
                return el;
            }
            catch { return null; }
        });

        IWebElement reportMenu = wait.Until(d =>
        {
            try
            {
                var el = d.FindElements(By.XPath("//a[normalize-space()='Report']"))
                          .FirstOrDefault(e => e.Displayed);
                return el;
            }
            catch { return null; }
        });

        roomsMenu.Should().NotBeNull("Rooms menu should be visible after successful admin login");
        reportMenu.Should().NotBeNull("Report menu should be visible after successful admin login");
    }

    [Fact(DisplayName = "UI-07 - Admin login with invalid credentials shows error")]
    public void AdminLogin_WithInvalidCredentials_ShowsErrorAndStaysOnLogin()
    {
        // Arrange
        _login.GoToLogin();

        // Act
        // Use generic login method; if you don't have it, add Login(string,string) as shown above
        _login.Login("wronguser", "wrongpass");

        // Assert
        var wait = new WebDriverWait(_fix.Driver, TimeSpan.FromSeconds(8));

        // 1) Error message "Invalid credentials" should be visible
        IWebElement? errorEl = null;
        try
        {
            errorEl = wait.Until(d =>
            {
                try
                {
                    // try common error selectors + text match
                    var el = d.FindElements(By.XPath("//*[contains(text(),'Invalid credentials')]"))
                              .FirstOrDefault(e => e.Displayed);
                    if (el != null) return el;

                    var alerts = d.FindElements(By.CssSelector(".alert-danger, .text-danger, .form-error"));
                    return alerts.FirstOrDefault(e =>
                        e.Displayed &&
                        e.Text.IndexOf("Invalid credentials", StringComparison.OrdinalIgnoreCase) >= 0);
                }
                catch { return null; }
            });
        }
        catch (WebDriverTimeoutException)
        {
            // leave errorEl as null
        }

        errorEl.Should().NotBeNull("Invalid credentials error message should be shown for wrong admin login");

        // 2) Still on login page: username field should still be present
        IWebElement? userInput = null;
        try
        {
            userInput = _fix.Driver.FindElements(By.CssSelector("input[name='username'], input#username"))
                                   .FirstOrDefault(e => e.Displayed);
        }
        catch { }

        userInput.Should().NotBeNull("User should remain on login page after invalid credentials");

        // 3) Optional: ensure admin menus are NOT visible
        var roomsMenus = _fix.Driver.FindElements(By.XPath("//a[normalize-space()='Rooms']"));
        roomsMenus.Any(e => e.Displayed).Should().BeFalse("Rooms menu should not be visible after invalid login");

        var reportMenus = _fix.Driver.FindElements(By.XPath("//a[normalize-space()='Report']"));
        reportMenus.Any(e => e.Displayed).Should().BeFalse("Report menu should not be visible after invalid login");
    }

    [Fact(DisplayName = "UI-08 - Logout invalidates admin session")]
    public void AdminLogout_InvalidatesSession_AndBlocksProtectedUrl()
    {
        var wait = new WebDriverWait(_fix.Driver, TimeSpan.FromSeconds(10));
        // Arrange: login as admin
        _login.GoToLogin();
        _login.LoginAsAdmin();

        // sanity check: Rooms menu visible
        var roomsMenu = wait.Until(d =>
        {
            try
            {
                return d.FindElements(By.XPath("//a[normalize-space()='Rooms']"))
                        .FirstOrDefault(e => e.Displayed);
            }
            catch { return null; }
        });
        roomsMenu.Should().NotBeNull("Rooms menu should be visible after successful admin login");

        // Act: logout via POM
        _login.Logout();

        // Try to access a protected URL after logout
        var protectedUrl = $"{_baseUrl}/admin/rooms";
        _fix.Driver.Navigate().GoToUrl(protectedUrl);

        // Assert: either we are on login page (username input visible) OR redirected to homepage
        IWebElement? userInput = null;
        bool isOnHomePage = false;

        try
        {
            userInput = wait.Until(d =>
            {
                try
                {
                    var el = d.FindElements(By.CssSelector("input[name='username'], input#username"))
                              .FirstOrDefault(e => e.Displayed);
                    return el;
                }
                catch { return null; }
            });
        }
        catch (WebDriverTimeoutException)
        {
            // no username field found within timeout — check if we're on homepage
        }

        // If username input not found, allow homepage as valid redirect target
        if (userInput == null)
        {
            // simple heuristic: URL equals base URL or contains the homepage path, or page has booking form
            var current = _fix.Driver.Url ?? string.Empty;
            if (current.TrimEnd('/').Equals(_baseUrl.TrimEnd('/'), StringComparison.OrdinalIgnoreCase)
                || current.Contains("/home", StringComparison.OrdinalIgnoreCase)
                || _fix.Driver.PageSource.Contains("Book this room") // adjust if needed
               )
            {
                isOnHomePage = true;
            }
        }

        (userInput != null || isOnHomePage).Should().BeTrue("After logout, accessing a protected URL should redirect to login page or to the public homepage.");

        // And admin menus should not be visible anymore
        // Instead of asserting 'Rooms' menu disappears (public site may show it), assert admin-only controls are gone

        // Check for admin-only controls in the header or page (e.g., "Add Room", "Create room", "Edit", "Delete" buttons)
        var adminControlsSelectors = new[]
        {
    "//button[contains(normalize-space(.),'Add Room')]",
    "//a[contains(normalize-space(.),'Create room') or contains(normalize-space(.),'Create Room')]",
    "//button[contains(normalize-space(.),'New Room')]",
    "//button[contains(normalize-space(.),'Edit')]",
    "//button[contains(normalize-space(.),'Delete')]",
    "//a[contains(@href,'/admin/rooms') and contains(normalize-space(.),'Edit')]" // defensive
};

        bool anyAdminControlVisible = false;
        foreach (var sel in adminControlsSelectors)
        {
            try
            {
                var els = _fix.Driver.FindElements(By.XPath(sel));
                if (els.Any(e => e.Displayed))
                {
                    anyAdminControlVisible = true;
                    break;
                }
            }
            catch
            {
                // ignore selector issues and continue
            }
        }

        anyAdminControlVisible.Should().BeFalse("After logout, admin-specific controls (Add/Create/Edit/Delete room) must not be visible to anonymous users.");

        // Additionally ensure the Rooms link (if present) points to public listing URL, not admin route
        var roomsLink = _fix.Driver.FindElements(By.XPath("//a[normalize-space()='Rooms']")).FirstOrDefault();
        if (roomsLink != null)
        {
            var href = roomsLink.GetAttribute("href") ?? string.Empty;
            href.Contains("/admin").Should().BeFalse("Public 'Rooms' link must not point to an admin-only URL after logout.");
        }

        var reportMenus = _fix.Driver.FindElements(By.XPath("//a[normalize-space()='Report']"));
        reportMenus.Any(e => e.Displayed).Should().BeFalse("Report menu should not be visible after logout");
    }

    [Fact(DisplayName = "UI-10 - Unauthorized actions from UI are blocked")]
    public void UnauthorizedUser_CannotAccessAdminRoomsOrPerformDestructiveActions()
    {
        var protectedUrl = $"{_baseUrl}/admin/rooms";
        var wait = new WebDriverWait(_fix.Driver, TimeSpan.FromSeconds(10));

        // ARRANGE – ensure we start from a "not logged in" state
        _fix.Driver.Manage().Cookies.DeleteAllCookies();

        // Try to access a protected admin URL directly without logging in
        _fix.Driver.Navigate().GoToUrl(protectedUrl);

        // ASSERT 1: user should be on the admin login page (username input visible)
        IWebElement? usernameInput = null;
        try
        {
            usernameInput = wait.Until(d =>
            {
                try
                {
                    return d.FindElements(By.CssSelector("input[name='username'], input#username"))
                            .FirstOrDefault(e => e.Displayed);
                }
                catch { return null; }
            });
        }
        catch (WebDriverTimeoutException)
        {
            // we'll assert below
        }

        usernameInput.Should().NotBeNull(
            "Accessing a protected admin URL while not logged in should redirect to the admin login page");

        // ASSERT 2: admin menus / controls should not be visible
        var roomsMenus = _fix.Driver.FindElements(By.XPath("//a[normalize-space()='Rooms']"));
        roomsMenus.Any(e => e.Displayed).Should().BeFalse("Rooms menu should not be visible for unauthenticated users");

        var reportMenus = _fix.Driver.FindElements(By.XPath("//a[normalize-space()='Report']"));
        reportMenus.Any(e => e.Displayed).Should().BeFalse("Report menu should not be visible for unauthenticated users");

        // ASSERT 3 (optional): no delete icons should be visible on the page
        var deleteIcons = _fix.Driver.FindElements(By.CssSelector(".roomDelete, .fa-remove"));
        deleteIcons.Any(e => e.Displayed).Should().BeFalse(
            "Delete icons should not be visible or usable without admin authentication");
    }
}
