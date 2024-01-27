using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OasisHubs.Site.Data;
using Stripe;

namespace OasisHubs.Site.Pages;

public class SignUpModel : PageModel {
   private readonly UserManager<OasisHubsUser> _userManager;
   private readonly SignInManager<OasisHubsUser> _signInManager;
   private readonly IStripeClient _stripeClient;
   private readonly ILogger<SignUpModel> _logger;

   [BindProperty] [Required] public string Name { get; set; } = string.Empty;

   [BindProperty]
   [Required]
   [EmailAddress]
   public string Email { get; set; } = string.Empty;

   [BindProperty]
   [Required]
   [DataType(DataType.Password)]
   public string Password { get; set; } = string.Empty;

   [TempData] public string ErrorMessage { get; set; } = string.Empty;

   public SignUpModel(UserManager<OasisHubsUser> userManager,
      SignInManager<OasisHubsUser> signInManager, IStripeClient stripeClient,
      ILogger<SignUpModel> logger) {
      this._userManager = userManager;
      this._signInManager = signInManager;
      this._stripeClient = stripeClient;
      this._logger = logger;
   }

   public void OnGet() {
   }

   public async Task<IActionResult> OnPostAsync() {
      if (ModelState.IsValid) {
         var customerService = new CustomerService(_stripeClient);
         var customers = await customerService.ListAsync(new() { Email = Email });

         if (customers.Any()) {
            ErrorMessage = "An account with that email already exists.";
            return Page();
         }

         var options = new CustomerCreateOptions { Name = Name, Email = Email };

         var newCustomer = await customerService.CreateAsync(options);
         var newUser = new OasisHubsUser {
            UserName = Email,
            Email = Email,
            EmailConfirmed = true,
            StripeCustomerId = newCustomer.Id
         };

         var createResult = await _userManager.CreateAsync(newUser, Password);
         if (createResult.Succeeded) {
            await _userManager.AddClaimAsync(newUser, new Claim(ClaimsConstants.OASIS_USER_TYPE, "customer"));
            await _signInManager.SignInAsync(newUser, isPersistent: false);
            return RedirectToPage("/Index");
         }

         this._logger.LogWarning("Unable to create user");
         foreach (var error in createResult.Errors) {
            ModelState.AddModelError(string.Empty, error.Description);
         }
      }

      return Page();
   }
}
