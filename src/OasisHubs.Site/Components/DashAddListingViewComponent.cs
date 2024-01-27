using Microsoft.AspNetCore.Mvc;

namespace OasisHubs.Site.Components;

[ViewComponent]
public class DashAddListingViewComponent : ViewComponent
{
   public IViewComponentResult Invoke() {
      return View(new DashAddListingModel());
   }

   public record DashAddListingModel();
}
