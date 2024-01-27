using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OasisHubs.Site.Data;

namespace OasisHubs.Site.Pages.Dashboard;

public class Index : PageModel {
   private readonly IDbContextFactory<OasisHubsDbContext> _dbContextFactory;
   private readonly UserManager<OasisHubsUser> _userManager;
   private readonly ILogger<Index> _logger;

   public IEnumerable<Booking> GuestBookings { get; set; } = default!;
   public Index(UserManager<OasisHubsUser> userManager,ILogger<Index> logger, IDbContextFactory<OasisHubsDbContext> dbContextFactory) {
      this._userManager = userManager;
      this._logger = logger;
      this._dbContextFactory = dbContextFactory;
   }
   public async Task<IActionResult> OnGetAsync() {
      var user = await this._userManager.GetUserAsync(User);

      if (user == null) {
         this._logger.LogError("User is null");
         return RedirectToPage("Error");
      }

      await using var context = await this._dbContextFactory.CreateDbContextAsync();

      GuestBookings = await context.Bookings
         .Where(b => b.Rental.StripeAccountId == user.StripeAccountId)
         .Include(b => b.Rental)
         .Include(b => b.Renter)
      .ToListAsync();

      return Page();
   }
}
