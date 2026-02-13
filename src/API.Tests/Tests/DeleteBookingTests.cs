using System.Net;
using System.Text.Json;
using FluentAssertions;
using RestSharp;
using Xunit;
using Xunit.Abstractions;

public class DeleteBookingTests : TestBase
{
    private readonly ITestOutputHelper _output;

    public DeleteBookingTests(ITestOutputHelper output) => _output = output;

    // API-14 — Delete booking — authenticated
    [Fact(DisplayName = "API-14 - Delete booking with valid authentication")]
    public async Task DeleteBooking_WithAuth_RemovesBooking()
    {
        // Arrange: create booking and get valid token
        var bookingClient = new BookingClient();
        var booking = CreateRandomBooking();
        var id = await BookingTestHelper.CreateBookingAndGetIdAsync(bookingClient, booking);
        _output.WriteLine($"[CREATE] booking id: {id}");

        var token = await new AuthClient().GetTokenAsync();
        _output.WriteLine($"[AUTH] Token (len): {token.Length}");

        // Act: DELETE /booking/{id} with auth
        var deleteResp = await bookingClient.DeleteBookingAsync(id, token);

        _output.WriteLine($"[DELETE] Status: {(int)deleteResp.StatusCode} - {deleteResp.StatusCode}");
        _output.WriteLine($"[DELETE] Body  : {deleteResp.Content}");

        // Assert: DELETE returns 200 or 201
        deleteResp.StatusCode
            .Should()
            .BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);

        // Act: GET /booking/{id} afterwards
        var getResp = await BookingTestHelper.GetBookingByIdWithRetryAsync(id);
        _output.WriteLine($"[GET after DELETE] Status: {(int)getResp.StatusCode} - {getResp.StatusCode}");
        _output.WriteLine($"[GET after DELETE] Body  : {getResp.Content}");

        // Known flakiness: if 418 persists even after retries, document as known issue
        if (getResp.StatusCode == (HttpStatusCode)418)
        {
            throw new Xunit.Sdk.XunitException(
                $"Known issue: GET /booking/{id} returned 418 I'm a Teapot after DELETE. " +
                $"Response: {getResp.Content}");
        }

        // Expected: booking no longer exists (404)
        getResp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
    [Fact(DisplayName = "API-15 - Block booking deletion without authentication")]
    public async Task DeleteBooking_WithoutAuth_IsRejected()
    {
        // Arrange
        var bookingClient = new BookingClient();
        var booking = CreateRandomBooking();
        var id = await BookingTestHelper.CreateBookingAndGetIdAsync(bookingClient, booking);
        _output.WriteLine($"[CREATE] booking id: {id}");

        // Act: DELETE /booking/{id} without auth
        var client = ApiClientFactory.Create(Settings.ApiBaseUrl);
        var deleteReq = new RestRequest($"booking/{id}", Method.Delete)
            .AddHeader("Accept", "application/json"); // no auth

        var deleteResp = await client.ExecuteAsync(deleteReq);

        _output.WriteLine($"[DELETE no auth] Status: {(int)deleteResp.StatusCode} - {deleteResp.StatusCode}");
        _output.WriteLine($"[DELETE no auth] Body  : {deleteResp.Content}");

        // Assert: deletion is blocked
        deleteResp.StatusCode
            .Should()
            .BeOneOf(new[] { HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden });
    }


    [Fact(
    DisplayName = "API-15b - GET after forbidden DELETE (known 418 issue)",
    Skip = "Known flaky issue: GET /booking/{id} sometimes returns 418 after forbidden DELETE in demo environment")]
    public async Task DeleteBooking_GetAfterForbiddenDelete_KnownIssue()
    {
        var bookingClient = new BookingClient();
        var booking = CreateRandomBooking();
        var id = await BookingTestHelper.CreateBookingAndGetIdAsync(bookingClient, booking);

        var client = ApiClientFactory.Create(Settings.ApiBaseUrl);
        var deleteReq = new RestRequest($"booking/{id}", Method.Delete)
            .AddHeader("Accept", "application/json");
        var deleteResp = await client.ExecuteAsync(deleteReq);
        deleteResp.StatusCode
            .Should()
            .BeOneOf(new[] { HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden });

        var getResp = await BookingTestHelper.GetBookingByIdWithRetryAsync(id);
        if (getResp.StatusCode == (HttpStatusCode)418)
        {
            throw new Xunit.Sdk.XunitException(
                $"Known issue: GET /booking/{id} returned 418 I'm a Teapot after forbidden DELETE. " +
                $"Response: {getResp.Content}");
        }

        getResp.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
