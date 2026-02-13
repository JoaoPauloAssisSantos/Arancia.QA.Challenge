using Bogus;
public abstract class TestBase
{
    protected Faker Faker { get; } = new Faker();
    protected Booking CreateRandomBooking()
    {
        return new Booking
        {
            firstname = Faker.Person.FirstName,
            lastname = Faker.Person.LastName,
            totalprice = Faker.Random.Int(50, 500),
            depositpaid = Faker.Random.Bool(),
            bookingdates = new BookingDates { checkin = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-dd"), checkout = DateTime.UtcNow.AddDays(4).ToString("yyyy-MM-dd") },
            additionalneeds = "Breakfast"
        };
    }
}
