using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OasisHubs.Site.Data;
using Stripe;

namespace OasisHubs.Site.Pages.Hosts;

public class Refresh : PageModel {
   private readonly UserManager<OasisHubsUser> _userManager;
   private readonly IStripeClient _stripeClient;
   private readonly LinkGenerator _linkGenerator;
   private readonly ILogger<Refresh> _logger;

   public Refresh(UserManager<OasisHubsUser> userManager, IStripeClient stripeClient,
      LinkGenerator linkGenerator, ILogger<Refresh> logger) {
      this._userManager = userManager;
      this._stripeClient = stripeClient;
      this._linkGenerator = linkGenerator;
      this._logger = logger;
   }

   public async Task<IActionResult> OnGetAsync() {
      var currentUser = await this._userManager.GetUserAsync(User);

      if (currentUser is null) {
         this._logger.LogDebug("User not found!");
         return Redirect("/Index");
      }

      // Generate new account link for onboarding
      var basePageUri = _linkGenerator.GetUriByPage(this.HttpContext, "/Index");
      var alcOptions = new AccountLinkCreateOptions {
         Account = currentUser.StripeAccountId,
         RefreshUrl = $"{basePageUri}/hosts/refresh",
         ReturnUrl = $"{basePageUri}/hosts/complete",
         Type = "account_onboarding",
         Collect = "eventually_due"
      };

      var accountLinkService = new AccountLinkService(_stripeClient);
      var acLink = await accountLinkService.CreateAsync(alcOptions);
      return Redirect(acLink.Url);
   }
}
