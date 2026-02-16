public static class Settings
{
    public static string AutomationTestingBaseUrl =>
        Environment.GetEnvironmentVariable("AUTOMATION_TESTING_BASE_URL")
        ?? "https://automationintesting.online";
}