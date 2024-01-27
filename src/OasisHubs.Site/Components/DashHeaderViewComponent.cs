using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OasisHubs.Site.Data;

namespace OasisHubs.Site.Components;

[ViewComponent]
public class DashHeaderViewComponent : ViewComponent
{
   private readonly UserManager<OasisHubsUser> _userManager;

   public DashHeaderViewComponent(UserManager<OasisHubsUser> userManager) {
      this._userManager = userManager;
   }
   public async Task<IViewComponentResult> InvokeAsync() {
      var oasisUser = await this._userManager.GetUserAsync(HttpContext.User);
      return View(new DashHeaderModel(oasisUser));
   }

   public record DashHeaderModel(OasisHubsUser? OasisUser);
}
