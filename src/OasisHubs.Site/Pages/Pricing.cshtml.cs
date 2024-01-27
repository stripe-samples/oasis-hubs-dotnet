using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OasisHubs.Site.Data;
using Stripe;
using Stripe.Checkout;

namespace OasisHubs.Site.Pages;

public class Pricing : PageModel {
   private readonly UserManager<OasisHubsUser> _userManager;
   private readonly IStripeClient _stripeClient;
   private readonly LinkGenerator _linkGenerator;
   private readonly ILogger<Pricing> _logger;
   private const string _tierMetaKey = "hub.tier";

   public IEnumerable<Product> HubTierListings { get; set; } = Enumerable.Empty<Product>();

   public Pricing(UserManager<OasisHubsUser> userManager, IStripeClient stripeClient,
      LinkGenerator linkGenerator, ILogger<Pricing> logger) {
      this._userManager = userManager;
      this._stripeClient = stripeClient;
      this._linkGenerator = linkGenerator;
      this._logger = logger;
   }

   public async Task<IActionResult> OnGetAsync() {
      var productsService = new ProductService(this._stripeClient);

      var options = new ProductListOptions {
         Expand = new() { "data.default_price" }, Active = true
      };

      var products = await productsService.ListAsync(options);
      if (products.Any()) {
         HubTierListings = products.Where(p => p.Metadata.ContainsKey(_tierMetaKey));
      }

      return Page();
   }

   public async Task<IActionResult> OnPostAsync() {
      var user = await _userManager.GetUserAsync(User);
      if (user == null) {
         return Redirect("/SignIn?returnUrl='/pricing'");
      }

      var lookupKey = Request.Form["lookupKey"].ToString();
      if (string.IsNullOrEmpty(lookupKey)) {
         _logger.LogWarning("Price ID wasn't supplied");
         return Page();
      }

      var plOptions = new PriceListOptions {
         LookupKeys = new List<string> { lookupKey, $"{lookupKey}_tiered" }
      };

      var priceService = new PriceService(this._stripeClient);
      var prices = await priceService.ListAsync(plOptions);
      var lineItems = prices.Select(p => new SessionLineItemOptions {
         Price = p.Id, Quantity = !p.LookupKey.EndsWith("_tiered") ? 1 : null
      }).ToList();


      var basePageUri = _linkGenerator.GetUriByPage(this.HttpContext, "/Index");
      var scOptions = new SessionCreateOptions {
         Customer = user.StripeCustomerId,
         CustomerUpdate = new SessionCustomerUpdateOptions { Address = "auto" },
         LineItems = lineItems,
         Mode = "subscription",
         AutomaticTax = new SessionAutomaticTaxOptions { Enabled = true },
         SuccessUrl = $"{basePageUri}/PaymentComplete?session_id={{CHECKOUT_SESSION_ID}}",
         CancelUrl = $"{basePageUri}",
         ConsentCollection = new() { Promotions = "auto" },
         AllowPromotionCodes = true
      };
      var sessionService = new SessionService(this._stripeClient);
      var session = await sessionService.CreateAsync(scOptions);
      return Redirect(session.Url);
   }
}
