using Paramore.Brighter;
using Stripe;

namespace OasisHubs.Site.Messaging;

public class ActivateHostAccountCommand : Command {
   public SlimAccount Account { get; init; }

   public ActivateHostAccountCommand(Account sourceAccount) : base(Guid.NewGuid())
      => this.Account = new SlimAccount(sourceAccount.Id, sourceAccount.Email,
         sourceAccount.DetailsSubmitted, sourceAccount.ChargesEnabled,
         sourceAccount.Metadata.GetValueOrDefault("owner.customer.id", string.Empty));

   public ActivateHostAccountCommand() : base(Guid.NewGuid()) {
      this.Account = default!;
   }

   public record SlimAccount(string StripeAccountId, string Email, bool DetailsSubmitted, bool ChargesEnabled, string CustomerId);
}
