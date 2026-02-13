using System.Text.Json;
using FluentAssertions;

public static class ResponseSchemaValidator
{
    public static void AssertBookingListSchema(string json)
    {
        using var doc = JsonDocument.Parse(json);

        // Root must be an array
        doc.RootElement.ValueKind.Should().Be(JsonValueKind.Array);

        bool found = false;

        foreach (var el in doc.RootElement.EnumerateArray())
        {
            // Each item must be an object
            el.ValueKind.Should().Be(JsonValueKind.Object);

            // Each object must have numeric bookingid
            if (el.TryGetProperty("bookingid", out var idEl))
            {
                idEl.ValueKind.Should().Be(JsonValueKind.Number);
                found = true;
            }
        }

        // Ensure at least one valid entry was found
        found.Should().BeTrue("booking list should contain at least one object with numeric bookingid");
    }


    public static void AssertBookingDetailsSchema(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.GetProperty("firstname").ValueKind.Should().Be(JsonValueKind.String);
        root.GetProperty("lastname").ValueKind.Should().Be(JsonValueKind.String);
        root.GetProperty("totalprice").ValueKind.Should().Be(JsonValueKind.Number);

        root.GetProperty("depositpaid").ValueKind
            .Should()
            .BeOneOf(JsonValueKind.True, JsonValueKind.False);

        var dates = root.GetProperty("bookingdates");
        dates.GetProperty("checkin").ValueKind.Should().Be(JsonValueKind.String);
        dates.GetProperty("checkout").ValueKind.Should().Be(JsonValueKind.String);

        if (root.TryGetProperty("additionalneeds", out var needs))
        {
            needs.ValueKind.Should().Be(JsonValueKind.String);
        }
    }


    public static void AssertAuthSchema(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // For success, token is present; for invalid credentials, may not
        if (root.TryGetProperty("token", out var tokenEl))
        {
            tokenEl.ValueKind.Should().Be(JsonValueKind.String);
        }
        else if (root.TryGetProperty("reason", out var reasonEl))
        {
            reasonEl.ValueKind.Should().Be(JsonValueKind.String);
        }
    }
}
