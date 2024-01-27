using Microsoft.AspNetCore.Identity;

namespace OasisHubs.Site.Data;

public class OasisHubsUser : IdentityUser {
   public string StripeCustomerId { get; set; } = string.Empty;
   public string StripeAccountId { get; set; } = string.Empty;
   public string ActiveSubscriptionId { get; set; } = string.Empty;
   public bool IsHost { get; set; }
   public bool HasSubscriptionActive { get; set; }
   public bool IsEnabled { get; set; } = true;
   public virtual ICollection<IdentityUserClaim<string>> Claims { get; set; } = default!;
}
