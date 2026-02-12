using System;
public static class Settings
{
    public static string ApiBaseUrl => Environment.GetEnvironmentVariable("API_BASE_URL") ?? "https://restful-booker.herokuapp.com";
}
