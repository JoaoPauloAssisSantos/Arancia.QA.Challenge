using Bogus;

namespace Arancia.Test.API.Clients;
    public static class RoomFactory
    {
        private static readonly Faker Faker = new();
        // Create a default valid room with randomized values (overrides optional)
        public static Room Create(
            string? roomName = null,
            string? type = null,
            bool? accessible = null,
            string? image = null,
            string? description = null,
            int? roomPrice = null,
            string[]? features = null)
        {
            return new Room
            {
                RoomName = roomName ?? Faker.Random.Int(100, 999).ToString(),
                Type = type ?? Faker.PickRandom(new[] { "Single", "Double", "Suite" }),
                Accessible = accessible ?? Faker.Random.Bool(),
                Image = image ?? "https://blog.postman.com/wp-content/uploads/2014/07/logo.png",
                Description = description ?? $"Room created by tests - {Faker.Lorem.Sentence()}",
                RoomPrice = roomPrice ?? Faker.Random.Int(50, 300),
                Features = features ?? new[] { "WiFi", "Safe" }
            };
        }

        // Convenience: create a Room and serialize to JSON (camelCase)
        public static string CreateJson(
            string? roomName = null,
            string? type = null,
            bool? accessible = null,
            string? image = null,
            string? description = null,
            int? roomPrice = null,
            string[]? features = null)
        {
            var room = Create(roomName, type, accessible, image, description, roomPrice, features);
            var opts = new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase };
            return System.Text.Json.JsonSerializer.Serialize(room, opts);
        }
    }
