using Bogus;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OasisHubs.Site.Data;
using Stripe;

namespace OasisHubs.Site.Pages.Hosts;

public class HostSignUpModel : PageModel {
   private readonly UserManager<OasisHubsUser> _userManager;
   private readonly SignInManager<OasisHubsUser> _signInManager;
   private readonly IStripeClient _stripeClient;
   private readonly LinkGenerator _linkGenerator;
   private readonly ILogger<SignUpModel> _logger;

   public OasisHubsUser? OasisUser { get; set; }

   public HostSignUpModel(UserManager<OasisHubsUser> userManager, SignInManager<OasisHubsUser> signInManager, IStripeClient stripeClient,
      LinkGenerator linkGenerator, ILogger<SignUpModel> logger) {
      this._userManager = userManager;
      this._signInManager = signInManager;
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

      if (currentUser.IsHost) {
         await _signInManager.RefreshSignInAsync(currentUser);
         return Redirect("/Index");
      }

      if (string.IsNullOrEmpty(currentUser.StripeAccountId)) {
         return Page();
      }

      // Generate new account link for onboarding
      var basePageUri = _linkGenerator.GetUriByPage(this.HttpContext, "/Index");
      var alcOptions = new AccountLinkCreateOptions {
         Account = currentUser.StripeAccountId,
         RefreshUrl = $"{basePageUri}hosts/refresh",
         ReturnUrl = $"{basePageUri}/hosts/complete",
         Type = "account_onboarding",
         Collect = "eventually_due"
      };

      var accountLinkService = new AccountLinkService(_stripeClient);
      var acLink = await accountLinkService.CreateAsync(alcOptions);
      return Redirect(acLink.Url);
   }

   public async Task<IActionResult> OnPostAsync() {
      var currentUser = await this._userManager.GetUserAsync(User);

      if (currentUser is null) {
         this._logger.LogDebug("User not found!");
         return Page();
      }

      // create express account
      var faker = new Faker("en_US");
      var companyName = faker.Company.CompanyName(0);
      var acOptions = new AccountCreateOptions {
         Country = "US",
         Type = "express",
         Email = currentUser.Email,
         Company = new AccountCompanyOptions {
            Name = companyName,
            Structure =
               "single_member_llc" //https://stripe.com/docs/connect/identity-verification#business-structure
         },
         Capabilities = new AccountCapabilitiesOptions {
            UsBankAccountAchPayments = new AccountCapabilitiesUsBankAccountAchPaymentsOptions {Requested = true},
            LinkPayments = new AccountCapabilitiesLinkPaymentsOptions { Requested = true },
            CardPayments = new AccountCapabilitiesCardPaymentsOptions { Requested = true },
            Transfers = new AccountCapabilitiesTransfersOptions { Requested = true }
         },
         BusinessType = "company",
         BusinessProfile = new AccountBusinessProfileOptions {
            Name = companyName,
            Mcc = "6513", //https://stripe.com/docs/connect/setting-mcc#list
            ProductDescription = "Remote work rental space",
            SupportEmail = currentUser.Email
         },
         TosAcceptance = new AccountTosAcceptanceOptions { ServiceAgreement = "full" },
         Metadata =
            new Dictionary<string, string> { ["owner.customer.id"] = currentUser.StripeCustomerId }
      };
      var accountService = new AccountService(this._stripeClient);
      var newExpressAccount = await accountService.CreateAsync(acOptions);

      // update user with express account Id
      currentUser.StripeAccountId = newExpressAccount.Id;
      await this._userManager.UpdateAsync(currentUser);


      // update Stripe customer with express account Id
      var cuOptions = new CustomerUpdateOptions {
         Metadata = new Dictionary<string, string> { ["host.account.id"] = newExpressAccount.Id }
      };
      var customerService = new CustomerService(this._stripeClient);
      await customerService.UpdateAsync(currentUser.StripeCustomerId, cuOptions);

      // Link account to platform
      var basePageUri = _linkGenerator.GetUriByPage(this.HttpContext, "/Index");
      var alcOptions = new AccountLinkCreateOptions {
         Account = newExpressAccount.Id,
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
