namespace Arancia.Test.API.Helpers;

public static class Settings
{
    // Base URLs (can be overridden via env vars)
    public static string RestfulBookerBaseUrl =>
        Environment.GetEnvironmentVariable("RESTFUL_BOOKER_BASE_URL") ?? "https://restful-booker.herokuapp.com";
    public static string AutomationTestingBaseUrl =>
    Environment.GetEnvironmentVariable("AUTOMATION_TESTING_BASE_URL") ?? "https://automationintesting.online";

    // API prefixes (if needed)
    public static string RestfulBookerApiBase => Combine(RestfulBookerBaseUrl, "");
    public static string AutomationTestingApiBase => Combine(AutomationTestingBaseUrl, "api");

    // Common endpoints (derived)
    public static string RestfulBookerPing => Combine(RestfulBookerApiBase, "ping");
    public static string RestfulBookerAuth => Combine(RestfulBookerApiBase, "auth");
    public static string RestfulBookerBooking => Combine(RestfulBookerApiBase, "booking");

    public static string AutomationTestingPing => Combine(AutomationTestingApiBase, "ping");
    public static string AutomationTestingAuth => Combine(AutomationTestingApiBase, "auth/login");
    public static string AutomationTestingBooking => Combine(AutomationTestingApiBase, "booking");

    private static string Combine(string baseUrl, string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return baseUrl.TrimEnd('/');
        return $"{baseUrl.TrimEnd('/')}/{path.TrimStart('/')}";
    }
}