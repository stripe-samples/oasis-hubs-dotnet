namespace OasisHubs.Site.Data;

public class Booking {
   public string Id { get; set; } = Guid.NewGuid().ToString("N");

   public OasisHubsUser Renter { get; set; } = default!;
   public required string RenterId { get; set; }

   public HubRental Rental { get; set; } = default!;
   public required string RentalId { get; set; }

   public required int Hours { get; set; }
   public required DateTimeOffset ReservedDateUtc { get; set; }
}
