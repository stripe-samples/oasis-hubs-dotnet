using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OasisHubs.Site.Data;

namespace OasisHubs.Site.Pages;
public class SignInModel : PageModel
{
    private readonly SignInManager<OasisHubsUser> _signInManager;
    private readonly ILogger<SignInModel> _logger;

    [BindProperty]
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [BindProperty]
    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Remember me?")]
    public bool RememberMe { get; set; }

    public string ReturnUrl { get; set; } = string.Empty;

    [TempData]
    public string ErrorMessage { get; set; } = string.Empty;

    public SignInModel(SignInManager<OasisHubsUser> signInManager, ILogger<SignInModel> logger)
    {
        this._signInManager = signInManager;
        this._logger = logger;
    }

    public IActionResult OnGet(string? returnUrl = null)
    {
        if (!string.IsNullOrEmpty(ErrorMessage))
        {
            ModelState.AddModelError(string.Empty, ErrorMessage);
        }

        returnUrl ??= Url.Content("/Index");
        if (User?.Identity?.IsAuthenticated ?? false)
            return LocalRedirect(returnUrl);

        ReturnUrl = returnUrl;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        returnUrl ??= Url.Content("/");

        if (ModelState.IsValid)
        {
            var result = await _signInManager.PasswordSignInAsync(Email, Password, RememberMe, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                _logger.LogInformation("User logged in.");
                return LocalRedirect(returnUrl);
            }
            // if (result.IsLockedOut) {
            //     _logger.LogWarning("User account locked out.");
            //     return RedirectToPage("./Lockout");
            // }
            else
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return Page();
            }

        }
        return Page();
    }
}

