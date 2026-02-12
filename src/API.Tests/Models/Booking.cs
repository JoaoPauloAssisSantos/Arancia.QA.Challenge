public class Booking
{
    public string? firstname { get; set; }
    public string? lastname { get; set; }
    public int totalprice { get; set; }
    public bool depositpaid { get; set; }
    public BookingDates? bookingdates { get; set; }
    public string? additionalneeds { get; set; }
}
