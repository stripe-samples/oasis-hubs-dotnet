using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OasisHubs.Site.Data;
using Paramore.Brighter;
using Paramore.Brighter.Inbox;
using Paramore.Brighter.Inbox.Attributes;

namespace OasisHubs.Site.Messaging;

public class ActivateCustomerSubscriptionRequestHandler : RequestHandlerAsync<
   ActivateCustomerSubscriptionCommand> {
   private readonly IDbContextFactory<OasisHubsDbContext> _dbContextFactory;
   private readonly UserManager<OasisHubsUser> _userManager;
   private readonly ILogger<ActivateCustomerSubscriptionCommand> _logger;

   public ActivateCustomerSubscriptionRequestHandler(
      IDbContextFactory<OasisHubsDbContext> dbContextFactory,
      UserManager<OasisHubsUser> userManager, ILogger<ActivateCustomerSubscriptionCommand> logger) {
      this._dbContextFactory = dbContextFactory;
      this._userManager = userManager;
      this._logger = logger;
   }

   [UseInboxAsync(step:0, contextKey: typeof(ActivateCustomerSubscriptionRequestHandler), onceOnly: true, onceOnlyAction: OnceOnlyAction.Throw)]
   public override async Task<ActivateCustomerSubscriptionCommand> HandleAsync(
      ActivateCustomerSubscriptionCommand command,
      CancellationToken cancellationToken = new()) {

      if (command.CustomerSubscription == null) {
         _logger.LogError("Subscription information missing from command.");
         throw new ArgumentNullException(nameof(command), "Subscription information missing.");
      }

      await using var context =
         await this._dbContextFactory.CreateDbContextAsync(cancellationToken);

      var customer = await context.Users
         .FirstOrDefaultAsync(u => u.StripeCustomerId == command.CustomerSubscription.StripeCustomerId, cancellationToken: cancellationToken);

      if (customer is { IsEnabled: true }) {
         customer.HasSubscriptionActive = true;
         await context.SaveChangesAsync(cancellationToken);

          var userRecord =  await this._userManager.FindByEmailAsync(customer.Email!);
          if (userRecord is not null) {
             var userClaims = await this._userManager.GetClaimsAsync(userRecord);
             var subClaim = userClaims.FirstOrDefault(c => c.Type == ClaimsConstants.OASIS_SUBSCRIPTION_ACTIVE);
             userRecord.ActiveSubscriptionId = command.CustomerSubscription.StripeSubscriptionId;

             if (subClaim is not null) {
                await this._userManager.ReplaceClaimAsync(userRecord, subClaim, new Claim(ClaimsConstants.OASIS_SUBSCRIPTION_ACTIVE, "true"));
             }
             else {
                await this._userManager.AddClaimAsync(userRecord, new Claim(ClaimsConstants.OASIS_SUBSCRIPTION_ACTIVE, "true"));
             }
          }
          else {
             this._logger.LogWarning("Matching user record not found in database for email {Email}", customer.Email);
          }
      }
      else {
         this._logger.LogWarning("Active customer not found {StripeCustomerId}", command.CustomerSubscription.StripeCustomerId);
      }

      return await base.HandleAsync(command, cancellationToken);
   }
}
