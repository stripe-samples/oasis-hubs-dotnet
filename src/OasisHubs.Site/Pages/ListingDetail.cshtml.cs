using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OasisHubs.Site.Data;
using Stripe;

namespace OasisHubs.Site.Pages;

[Authorize(Policy = "can_view_listings")]
public class ListingDetailModel : PageModel {
   private readonly IDbContextFactory<OasisHubsDbContext> _dbContextFactory;
   private readonly UserManager<OasisHubsUser> _userManager;
   private readonly IStripeClient _stripeClient;
   private readonly ILogger<ListingDetailModel> _logger;

   [BindProperty(SupportsGet = true)] public string ReferenceCode { get; set; } = string.Empty;
   public HubRental? Rental { get; set; }

   public OasisHubsUser? OasisUser { get; set; }

   public ListingDetailModel(IDbContextFactory<OasisHubsDbContext> dbContextFactory,
      UserManager<OasisHubsUser> userManager, IStripeClient stripeClient, ILogger<ListingDetailModel> logger) {
      this._dbContextFactory = dbContextFactory;
      this._userManager = userManager;
      this._stripeClient = stripeClient;
      this._logger = logger;
   }

   public async Task<IActionResult> OnGetAsync() {
      if (string.IsNullOrEmpty(ReferenceCode)) return RedirectToPage("/listings");

      OasisUser = await _userManager.GetUserAsync(HttpContext.User);

      await using var context = await this._dbContextFactory.CreateDbContextAsync();
      Rental = context.HubRentals
         .FirstOrDefault(h => h.IsActive && h.ReferenceCode.ToUpper() == ReferenceCode.ToUpper());

      if (Rental == null)
         return RedirectToPage("/listings");

      return Page();
   }

   public async Task<IActionResult> OnPostAsync() {
      OasisUser = await _userManager.GetUserAsync(HttpContext.User);

      var renterId = OasisUser!.Id;
      if (string.IsNullOrEmpty(renterId)) {
         return RedirectToPage("/signin");
      }

      var checkInDate = DateTimeOffset.Parse(Request.Form["checkInDate"].ToString());

      var hours = int.Parse(Request.Form["hours"].ToString());
      var booking = new Booking {
         RenterId = renterId,
         RentalId = Request.Form["rentalId"].ToString(),
         Hours = hours,
         ReservedDateUtc = checkInDate.ToUniversalTime()
      };

      await using var context = await this._dbContextFactory.CreateDbContextAsync();
      context.Bookings.Add(booking);
      await context.SaveChangesAsync();

      // retrieve subscription
      var subscriptionService = new SubscriptionService(this._stripeClient);
      var subscription = await subscriptionService.GetAsync(OasisUser.ActiveSubscriptionId);
      if (subscription is not null) {

         var subItem = subscription.Items.Data.Single(s => s.Price.LookupKey.EndsWith("_tiered"));

            // report on usage
            var ucOptions = new UsageRecordCreateOptions
            {
               Quantity = hours,
               //Timestamp = DateTime.UtcNow,
               Action = "increment"
            };
            var idempotencyKey = Guid.NewGuid().ToString("N");
            var requestOptions = new RequestOptions
            {
               IdempotencyKey = idempotencyKey
            };

            var usageRecordService = new UsageRecordService(this._stripeClient);
            await usageRecordService.CreateAsync(subItem.Id, ucOptions, requestOptions);

            return RedirectToPage("/bookings");
      }

      this._logger.LogError("Subscription not found {SubscriptionId}",OasisUser.ActiveSubscriptionId);
      return Page();
   }
}
