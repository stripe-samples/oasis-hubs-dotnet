using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OasisHubs.Site.Data;

namespace OasisHubs.Site.Pages;

[Authorize]
public class Bookings : PageModel {
   private readonly IDbContextFactory<OasisHubsDbContext> _dbContextFactory;
   private readonly UserManager<OasisHubsUser> _userManager;
   private readonly ILogger<Bookings> _logger;

   public IEnumerable<Booking> UserBookings { get; set; } = default!;

   public Bookings(IDbContextFactory<OasisHubsDbContext> dbContextFactory,
      UserManager<OasisHubsUser> userManager, ILogger<Bookings> logger) {

      this._dbContextFactory = dbContextFactory;
      this._userManager = userManager;
      this._logger = logger;
   }

   public async Task<IActionResult> OnGet() {
      var user = await _userManager.GetUserAsync(HttpContext.User);

      if (user == null) {
         this._logger.LogError("User is null");
         return RedirectToPage("Error");
      }

      await using var context = await this._dbContextFactory.CreateDbContextAsync();

      UserBookings = await context.Bookings
         .Include(b => b.Rental)
         .Include(b => b.Renter)
         .Where(b => b.RenterId == user.Id).ToListAsync();
      return Page();
   }
}
