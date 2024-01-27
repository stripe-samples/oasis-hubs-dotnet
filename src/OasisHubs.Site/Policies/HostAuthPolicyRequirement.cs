using Microsoft.AspNetCore.Authorization;

namespace OasisHubs.Site.Policies;


public class HostAuthPolicyRequirement : AuthorizationHandler<HostAuthPolicyRequirement>, IAuthorizationRequirement {

   protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, HostAuthPolicyRequirement requirement) {
        var isHost = context.User.HasClaim(c => c is { Type: ClaimsConstants.OASIS_USER_TYPE, Value: "host" });
        if (isHost) {
            context.Succeed(requirement);
        }
        return Task.CompletedTask;
    }
}
