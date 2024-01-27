using Paramore.Brighter;
using Subscription = Stripe.Subscription;

namespace OasisHubs.Site.Messaging;

public class ActivateCustomerSubscriptionCommand : Command {
   public SlimSubscription CustomerSubscription { get; init; }

   public ActivateCustomerSubscriptionCommand(Subscription newSubscription) :base(Guid.NewGuid()) {
      this.CustomerSubscription =
         new SlimSubscription(newSubscription.Id, newSubscription.CustomerId);
   }

   public ActivateCustomerSubscriptionCommand() : base(Guid.NewGuid()) {
      this.CustomerSubscription = default!;
   }

   public record SlimSubscription(string StripeSubscriptionId, string StripeCustomerId);
}
