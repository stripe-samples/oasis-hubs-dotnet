using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OasisHubs.Site.Data;

namespace OasisHubs.Site.Pages.Hosts;

public class Complete : PageModel {
   private UserManager<OasisHubsUser> _userManager;
   private SignInManager<OasisHubsUser> _signInManager;

   public Complete(UserManager<OasisHubsUser> userManager,
      SignInManager<OasisHubsUser> signInManager) {
      this._userManager = userManager;
      this._signInManager = signInManager;
   }

   public async Task<IActionResult> OnGetAsync() {
      var userRecord = await _userManager.GetUserAsync(HttpContext.User);
      if (userRecord != null) {
         await _signInManager.RefreshSignInAsync(userRecord);
      }

      return RedirectToPage("/Index");
   }
}
