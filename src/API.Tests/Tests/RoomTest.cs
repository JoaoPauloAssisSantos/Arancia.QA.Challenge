using Arancia.Test.API.Clients;
using FluentAssertions;
using RestSharp;
using System.Net;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

public class RoomTests : TestBase
{
    public RoomTests(ITestOutputHelper output) => InitTestBase(output);
    [Fact(DisplayName = "API-21 - Create Room")]
    public async Task CreateRoom()
    { }

    [Fact(DisplayName = "API-22 - Create Room And Get Room")]
    public async Task CreateRoomAndGetRoom()
    { }

    [Fact(DisplayName = "API-23 - Create Room Without valid Auth")]
    public async Task CreateRoomWithoutvalidAuth()
    { }

    [Fact(DisplayName = "API-23 - Get Room Without valid Auth")]
    public async Task GetRoomWithoutValidAuth()
    { }

    [Fact(DisplayName = "API-24 - Update Room with valid Auth")]
    public async Task UpdateRoomWithValidAuth()
    { }

    [Fact(DisplayName = "API-24 - Update Room without valid Auth")]
    public async Task UpdateRoomWithoutValidAuth()
    { }

    [Fact(DisplayName = "API-24 - Delete Room with valid Auth")]
    public async Task DeleteRoomWithValidAuth_RemovesRoom()
    { }

    [Fact(DisplayName = "API-24 - Delete Room without valid Auth")]
    public async Task DeleteRoomWithValidAuthout_RemovesRoom()
    { }

    [Fact(DisplayName = "API-24 - Create room and book")]
    public async Task CreateRoomAndBook_BookingCreated()
    { }
}