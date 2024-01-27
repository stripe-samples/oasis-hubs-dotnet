using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using OasisHubs.Site.Data;
using Paramore.Brighter;
using Paramore.Brighter.Inbox;
using Paramore.Brighter.Inbox.Attributes;
using Stripe;

namespace OasisHubs.Site.Messaging;

public class ActivateHostRequestHandler : RequestHandlerAsync<ActivateHostAccountCommand> {
   private readonly UserManager<OasisHubsUser> _userManager;
   private readonly IStripeClient _stripeClient;
   private readonly ILogger<ActivateHostRequestHandler> _logger;

   public ActivateHostRequestHandler(UserManager<OasisHubsUser> userManager, IStripeClient stripeClient,
      ILogger<ActivateHostRequestHandler> logger) {
      this._userManager = userManager;
      this._stripeClient = stripeClient;
      this._logger = logger;
   }

   [UseInboxAsync(step:0, contextKey: typeof(ActivateHostRequestHandler), onceOnly: true, onceOnlyAction: OnceOnlyAction.Throw)]
   public override async Task<ActivateHostAccountCommand> HandleAsync(
      ActivateHostAccountCommand command,
      CancellationToken cancellationToken = new()) {
      if (command.Account == null) {
         _logger.LogError("Account information missing from command.");
         throw new ArgumentNullException(nameof(command), "Account information missing.");
      }

      var hostUser = await this._userManager.FindByEmailAsync(command.Account.Email);

      if (hostUser != null) {
         if (!string.IsNullOrEmpty(command.Account.CustomerId)) {
            var customerService = new CustomerService(this._stripeClient);
            var stripeCustomer = await customerService.GetAsync(command.Account.CustomerId,
               cancellationToken: cancellationToken);

            await customerService.UpdateAsync(stripeCustomer.Id,
               new CustomerUpdateOptions {
                  Metadata = new Dictionary<string, string> { { ClaimsConstants.OASIS_USER_TYPE, "host" } }
               }, cancellationToken: cancellationToken);

            this._logger.LogInformation("Stripe customer updated with host metadata.");
            hostUser.IsHost = true;

            var userClaims = await this._userManager.GetClaimsAsync(hostUser);
            var filterClaims = userClaims.Where(c => c.Type == ClaimsConstants.OASIS_USER_TYPE).ToArray();

            if (filterClaims.Any()) {
               await this._userManager.RemoveClaimsAsync(hostUser, filterClaims);
            }

            await this._userManager.AddClaimAsync(hostUser, new Claim(ClaimsConstants.OASIS_USER_TYPE, "host"));

            this._logger.LogInformation("User ({UserId}) found and host enabled.", hostUser.Id);
         }
      }
      else {
         this._logger.LogError("HostUser for Stripe account ({StripeAccountId}) not found.",
            command.Account.StripeAccountId);
      }

      return await base.HandleAsync(command, cancellationToken);
   }
}
