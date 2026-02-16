using Bogus;

public static class ContactFactory
{
    private static readonly Faker Faker = new Faker();

    public static Contact CreateRandomContact()
    {
        return new Contact
        {
            FirstName = Faker.Name.FirstName(),
            LastName = Faker.Name.LastName(),
            Email = Faker.Internet.Email(),
            Phone = Faker.Phone.PhoneNumber("###########")
        };
    }
}

public class Contact
{
    public string FirstName { get; init; } = "";
    public string LastName { get; init; } = "";
    public string Email { get; init; } = "";
    public string Phone { get; init; } = "";
}

