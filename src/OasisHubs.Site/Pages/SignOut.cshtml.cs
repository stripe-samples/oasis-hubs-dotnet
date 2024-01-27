using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OasisHubs.Site.Data;

namespace OasisHubs.Site.Pages;

public class SignOutModel : PageModel
{

    private readonly SignInManager<OasisHubsUser> _signInManager;
    private readonly ILogger<SignOutModel> _logger;

    public SignOutModel(SignInManager<OasisHubsUser> signInManager, ILogger<SignOutModel> logger)
    {
        _signInManager = signInManager;
        _logger = logger;
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        await _signInManager.SignOutAsync();
        _logger.LogInformation("User logged out.");

        if (returnUrl != null)
        {
            return LocalRedirect(returnUrl);
        }
        else
        {
            return RedirectToPage("/Index");
        }
    }
}