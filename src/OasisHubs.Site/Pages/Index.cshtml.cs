using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OasisHubs.Site.Data;

namespace OasisHubs.Site.Pages;

public class IndexModel : PageModel {
    private readonly UserManager<OasisHubsUser> _userManager;
    private readonly ILogger<IndexModel> _logger;

    public OasisHubsUser? OasisUser { get; set; }

    public IndexModel(UserManager<OasisHubsUser> userManager, ILogger<IndexModel> logger) {
        this._userManager = userManager;
        _logger = logger;
    }

    public async Task<IActionResult> OnGetAsync() {
        OasisUser = await _userManager.GetUserAsync(HttpContext.User);
        return Page();
    }
}
