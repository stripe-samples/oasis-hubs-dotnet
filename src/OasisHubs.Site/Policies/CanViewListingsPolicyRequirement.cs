using Microsoft.AspNetCore.Authorization;

namespace OasisHubs.Site.Policies;

public class CanViewListingsPolicyRequirement : AuthorizationHandler<CanViewListingsPolicyRequirement>, IAuthorizationRequirement {

   protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, CanViewListingsPolicyRequirement requirement) {
      var canView = context.User.HasClaim(c => c is { Type: ClaimsConstants.OASIS_SUBSCRIPTION_ACTIVE, Value: "true" } or { Type: ClaimsConstants.OASIS_USER_TYPE, Value: "host" } );
      if (canView) {
         context.Succeed(requirement);
      }
      return Task.CompletedTask;
   }
}
