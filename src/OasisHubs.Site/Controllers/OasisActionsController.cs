using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OasisHubs.Site.Data;
using OasisHubs.Site.Pages;
using Stripe;

namespace OasisHubs.Site.Controllers;

[ApiController]
[Route("actions")]
public class OasisActionsController : Controller {
   private readonly UserManager<OasisHubsUser> _userManager;
   private readonly IStripeClient _stripeClient;
   private readonly LinkGenerator _linkGenerator;
   private readonly ILogger<Pricing> _logger;

   public OasisActionsController(UserManager<OasisHubsUser> userManager, IStripeClient stripeClient, LinkGenerator linkGenerator, ILogger<Pricing> logger) {
      this._userManager = userManager;
      this._stripeClient = stripeClient;
      this._linkGenerator = linkGenerator;
      this._logger = logger;
   }

   [Authorize]
   [HttpPost("create-portal-session")]
   public async Task<IActionResult> CreatePortalSession() {
      var user = await _userManager.GetUserAsync(User);
      if (user == null) {
         return RedirectToPage("/SignIn");
      }

      var basePageUri = _linkGenerator.GetUriByPage(this.HttpContext, "/Index");
      var options = new Stripe.BillingPortal.SessionCreateOptions
      {
         Customer = user.StripeCustomerId,
         ReturnUrl = basePageUri
      };
      var service = new Stripe.BillingPortal.SessionService(_stripeClient);
      var portalSession = await service.CreateAsync(options);

      _logger.LogInformation("Billing portal session created for Stripe customer ({StripeCustomerId})" , user.StripeCustomerId);
      return Redirect(portalSession.Url);
   }

   [Authorize(policy: "is_host_policy")]
   [HttpPost("express-dashboard-login")]
   public async Task<IActionResult> ExpressDashboardLogin() {
      var user = await _userManager.GetUserAsync(User);
      if (user == null) {
         return RedirectToPage("/SignIn");
      }
      var loginLinkService = new LoginLinkService();
      var loginLink = await loginLinkService.CreateAsync(user.StripeAccountId);
      _logger.LogInformation("Express Dashboard login created for Stripe account ({StripeAccountId})" , user.StripeAccountId);
      return Redirect(loginLink.Url);
   }
}
