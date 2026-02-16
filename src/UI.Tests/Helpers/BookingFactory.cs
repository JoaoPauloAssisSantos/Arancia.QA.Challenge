using Bogus;

namespace UI.Tests.Helpers
{
    public static class BookingFactory
    {
        private static readonly Faker Faker = new Faker();

        public static Booking CreateRandomBooking()
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
}
