namespace OasisHubs.Site.Data;

public class HubRental {
   public string Id { get; set; } = Guid.NewGuid().ToString("N");
   public required string Title { get; set; } = string.Empty;
   public string Description { get; set; } = string.Empty;
   public string Location { get; set; } = string.Empty;
   public int Capacity { get; set; }
   public RentalType HubType { get; set; } = RentalType.Other;
   public RentalTier HubTier { get; set; } = RentalTier.Basic;
   public string ImageUrl { get; set; } = string.Empty;
   public string StripeAccountId { get; set; } = string.Empty;
   public required string ReferenceCode { get; set; } = string.Empty;
   public bool IsActive { get; set; } = true;
}

public enum RentalTier {
   Basic,
   Standard,
   Premium
}

public enum RentalType {
   EntireSpace,
   Room,
   Desk,
   Other
}
