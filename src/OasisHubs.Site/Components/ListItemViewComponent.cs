using Microsoft.AspNetCore.Mvc;
using OasisHubs.Site.Data;

namespace OasisHubs.Site.Components;

[ViewComponent]
public class ListItemViewComponent : ViewComponent
{
   public IViewComponentResult Invoke(HubRental rental) {
      return View(new ListItemVModel(rental));
   }

   public record ListItemVModel(HubRental Rental);
}
