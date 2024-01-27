using Microsoft.AspNetCore.Mvc;
using OasisHubs.Site.Data;
using Stripe;

namespace OasisHubs.Site.Components;

[ViewComponent]
public class DashListingViewComponent : ViewComponent {

   public IViewComponentResult Invoke(HubRental rental) {
      return View(new DashListingModel(rental));
   }

   public record DashListingModel(HubRental Rental);
}
