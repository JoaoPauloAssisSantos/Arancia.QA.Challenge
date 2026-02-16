using Arancia.Test.API.Clients;
using Bogus;
using Xunit.Abstractions;
namespace Arancia.Test.API.Helpers;

public abstract class TestBase
{
    protected Faker Faker { get; } = new Faker();
    // Test output and auth client (initialized by derived test class)
    protected ITestOutputHelper Output { get; private set; } = null!;
    protected IAuthClient AuthClient { get; private set; } = null!;

    /// <summary>
    /// Call this from the test class constructor to initialize Output and AuthClient.
    /// Optionally select implementation via env var AUTH_IMPL = "automation" or "restful" (default).
    /// </summary>
    protected void InitTestBase(ITestOutputHelper output)
    {
        Output = output;

        var impl = Environment.GetEnvironmentVariable("AUTH_IMPL")?.Trim().ToLowerInvariant();
        AuthClient = impl switch
        {
            "automation" => new AutomationTestingAuthClient(), // if you need the other impl
            _ => new RestfulBookerAuthClient(),
        };
    }

    protected Booking CreateRandomBooking()
    {
        return new Booking
        {
            roomid = Faker.Random.Int(1, 100),
            firstname = Faker.Person.FirstName,
            lastname = Faker.Person.LastName,
            totalprice = Faker.Random.Int(50, 500),
            depositpaid = Faker.Random.Bool(),
            bookingdates = new BookingDates
            {
                checkin = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-dd"),
                checkout = DateTime.UtcNow.AddDays(4).ToString("yyyy-MM-dd")
            },
            additionalneeds = "Breakfast"
        };
    }
}
