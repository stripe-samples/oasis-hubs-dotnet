using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OasisHubs.Site.Data;

namespace OasisHubs.Site.Pages;
[Authorize(Policy = "can_view_listings")]
public class ListingsModel : PageModel {
   private readonly IDbContextFactory<OasisHubsDbContext> _dbContextFactory;

   public IEnumerable<string> ListingCategories { get; init; } = new[] {
      "Apartment", "House", "Treehouse", "Mansion", "Boats", "Tiny Homes",
      "Mobile Home","Beachfront"
   };

   public IEnumerable<HubRental> Rentals { get; set; } = Enumerable.Empty<HubRental>();

   public ListingsModel(IDbContextFactory<OasisHubsDbContext> dbContextFactory) {
      this._dbContextFactory = dbContextFactory;
   }

   public async Task<IActionResult> OnGet() {

      await using var context = await this._dbContextFactory.CreateDbContextAsync();
      Rentals = context.HubRentals.Where(h => h.IsActive).ToList();
      return Page();
   }
}

