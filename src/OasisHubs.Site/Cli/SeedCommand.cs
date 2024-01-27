using System.Security.Claims;
using Bogus;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OasisHubs.Site.Data;
using Stripe;
using Stripe.TestHelpers;
using CustomerService = Stripe.CustomerService;

namespace OasisHubs.Site.Cli;

[Command(Name = "seed", Description = "Populates database with demo data")]
public class SeedCommand : CommandBase {
   private readonly UserManager<OasisHubsUser> _userManager;
   private readonly IDbContextFactory<OasisHubsDbContext> _dbContextFactory;
   private readonly ILogger<SeedCommand> _logger;

   private readonly PriceService _priceService;
   private readonly ProductService _productsService;
   private readonly CustomerService _customerService;
   private readonly AccountService _accountService;
   private readonly TestClockService _testClockService;
   private readonly Faker _faker;

   private const string _actMetaKey = "host.account.id";
   private const string _custMetaKey = "oasis.customer.id";

   private const string _placeHolderDescription =
      "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat.";

   public SeedCommand(UserManager<OasisHubsUser> userManager, IStripeClient stripeClient,
      IDbContextFactory<OasisHubsDbContext> dbContextFactory, ILogger<SeedCommand> logger) {
      this._userManager = userManager;
      this._productsService = new ProductService(stripeClient);
      this._priceService = new PriceService(stripeClient);
      this._customerService = new CustomerService(stripeClient);
      this._accountService = new AccountService(stripeClient);
      this._testClockService = new TestClockService(stripeClient);
      this._dbContextFactory = dbContextFactory;
      this._logger = logger;
      this._faker = new Faker("en_US");
   }

   public async Task OnExecuteAsync() {
      var proceed =
         Prompt.GetYesNo("Proceed with seeding this database?",
            defaultAnswer: false, promptColor: ConsoleColor.Blue);

      if (!proceed) return;

      this._logger.LogInformation("Seeding database ...");

      await using var context = await this._dbContextFactory.CreateDbContextAsync();
      await context.Database.EnsureCreatedAsync();

      await CreateUsers();
      await CreateHubTierProductsAsync();

      this._logger.LogInformation("Seeding completed!");
   }

   private async Task CreateUsers() {
      this._logger.LogInformation("Creating users ...");

      // Host User 1
      await CreateUser("Cecil Phillip", "cecil@test.com", addExpressAccount: true);

      // Host User 2
      await CreateUser("Phil Host", "phil@test.com", addExpressAccount: true);

      // Customer 1
      await CreateUser("Jonathan Smith", "jon@test.com");

      // Customer 2
      await CreateUser("Jaime Renter", "jaime@test.com");

      // Customer 3
      await CreateUser("Benjamin Westminster", "ben@test.com", addTestClock: true);

      // Customer 4
      await CreateUser("Chronos Titan", "chronos@test.com", addTestClock: true);
   }

   private async Task CreateUser(string name, string email, bool addExpressAccount = false,
      bool addTestClock = false) {
      var ccOptions = new CustomerCreateOptions {
         Name = name,
         Email = email,
         Description = "Faker User Account",
         PaymentMethod = "pm_card_visa",
         Address = new AddressOptions {
            Line1 = this._faker.Address.StreetAddress(),
            City = this._faker.Address.City(),
            State = this._faker.Address.StateAbbr(),
            Country = "US"
         },
         InvoiceSettings =
            new CustomerInvoiceSettingsOptions { DefaultPaymentMethod = "pm_card_visa" }
      };

      if (addTestClock) {
         this._logger.LogInformation("Creating test clock ...");
         var tcCreateOptions = new TestClockCreateOptions {
            Name = $"Subscription Clock ({name})", FrozenTime = DateTimeOffset.UtcNow.DateTime
         };

         var newTestClock = await this._testClockService.CreateAsync(tcCreateOptions);
         ccOptions.TestClock = newTestClock.Id;
         this._logger.LogDebug(
            "Created test clock ({TestClock}) attached to user ({Username}).", newTestClock.Id,
            ccOptions.Name);
      }

      var newCustomer = await _customerService.CreateAsync(ccOptions);
      var newUser = new OasisHubsUser {
         UserName = ccOptions.Email,
         Email = ccOptions.Email,
         EmailConfirmed = true,
         StripeCustomerId = newCustomer.Id
      };

      await this._userManager.CreateAsync(newUser, "test");

      // add claim
      await _userManager.AddClaimAsync(newUser, new Claim(ClaimsConstants.OASIS_USER_TYPE, "customer"));
      this._logger.LogDebug("Created user {CustomerName}.", ccOptions.Name);

      // update Stripe customer with oasis customer Id
      var cuOptions = new CustomerUpdateOptions {
         Metadata = new Dictionary<string, string> { [_custMetaKey] = newUser.Id }
      };

      if (addExpressAccount) {
         this._logger.LogInformation("Created express account ...");

         // create express account
         var companyName = this._faker.Company.CompanyName(0);
         var acOptions = new AccountCreateOptions {
            Country = "US",
            Email = newUser.Email,
            Type = "express",
            Company = new AccountCompanyOptions {
               Name = companyName,
               Structure =
                  "single_member_llc", //https://stripe.com/docs/connect/identity-verification#business-structure
               Address = new AddressOptions {
                  Line1 = "address_full_match",
                  City = "Miami",
                  State = "FL",
                  PostalCode = "33109",
                  Country = "US"
               }
            },
            BusinessProfile = new AccountBusinessProfileOptions {
               Name = companyName,
               Mcc = "6513", //https://stripe.com/docs/connect/setting-mcc#list
               ProductDescription = "Remote work rental space",
               SupportEmail = newUser.Email
            },
            BusinessType = "company",
            Capabilities = new AccountCapabilitiesOptions {
               UsBankAccountAchPayments =
                  new AccountCapabilitiesUsBankAccountAchPaymentsOptions { Requested = true },
               LinkPayments = new AccountCapabilitiesLinkPaymentsOptions { Requested = true },
               CardPayments = new AccountCapabilitiesCardPaymentsOptions { Requested = true },
               Transfers = new AccountCapabilitiesTransfersOptions { Requested = true }
            },
            TosAcceptance = new AccountTosAcceptanceOptions { ServiceAgreement = "full" },
            Metadata = new Dictionary<string, string> { ["owner.customer.id"] = newCustomer.Id }
         };

         var newExpressAccount = await _accountService.CreateAsync(acOptions);
         this._logger.LogDebug(
            "Created express account for {ExpressBusinessName}.", acOptions.BusinessProfile.Name);

         // update user with express account Id
         newUser.StripeAccountId = newExpressAccount.Id;
         await _userManager.UpdateAsync(newUser);

         // update Stripe customer with express
         cuOptions.Metadata[_actMetaKey] = newExpressAccount.Id;
         await CreateRentalHubsAsync(newUser.StripeAccountId, companyName);
      }

      await _customerService.UpdateAsync(newCustomer.Id, cuOptions);
   }

   private async Task CreateRentalHubsAsync(string expressAccountId, string companyName) {
      if (string.IsNullOrEmpty(expressAccountId)) {
         _logger.LogWarning("Cannot create hubs. Express account user not found!");
         return;
      }

      this._logger.LogInformation("Creating Hubs ...");
      await using var context = await this._dbContextFactory.CreateDbContextAsync();
      await context.Database.EnsureCreatedAsync();

      // Hub Rental #1
      await CreateHubAsync(context,
         new HubRental {
            Title = $"Two Room Condo by {companyName}",
            Description = _placeHolderDescription,
            Capacity = this._faker.Random.Number(1, 10),
            HubType = this._faker.PickRandom<RentalType>(),
            HubTier = this._faker.PickRandom<RentalTier>(),
            Location = this._faker.Address.State(),
            ImageUrl = "https://images.unsplash.com/photo-1522708323590-d24dbb6b0267",
            StripeAccountId = expressAccountId,
            ReferenceCode = ReferenceCodeGenerator.GetUniqueKey()
         });

      // Hub Rental #2
      await CreateHubAsync(context,
         new HubRental {
            Title = $"Cute Ranch with huge yard by {companyName}",
            Description = _placeHolderDescription,
            Capacity = this._faker.Random.Number(1, 10),
            HubType = this._faker.PickRandom<RentalType>(),
            HubTier = this._faker.PickRandom<RentalTier>(),
            Location = this._faker.Address.State(),
            ImageUrl = "https://images.unsplash.com/photo-1502672023488-70e25813eb80",
            StripeAccountId = expressAccountId,
            ReferenceCode = ReferenceCodeGenerator.GetUniqueKey()
         });

      // Hub Rental #3
      await CreateHubAsync(context,
         new HubRental {
            Title = $"The Canopy House by {companyName}",
            Description = _placeHolderDescription,
            Capacity = this._faker.Random.Number(1, 10),
            HubType = this._faker.PickRandom<RentalType>(),
            HubTier = this._faker.PickRandom<RentalTier>(),
            Location = this._faker.Address.State(),
            ImageUrl = "https://images.unsplash.com/photo-1534595038511-9f219fe0c979",
            StripeAccountId = expressAccountId,
            ReferenceCode = ReferenceCodeGenerator.GetUniqueKey()
         });

      // Hub Rental #4
      await CreateHubAsync(context,
         new HubRental {
            Title = $"Co-Working Desk by {companyName}",
            Description = _placeHolderDescription,
            Capacity = this._faker.Random.Number(1, 10),
            HubType = this._faker.PickRandom<RentalType>(),
            HubTier = this._faker.PickRandom<RentalTier>(),
            Location = this._faker.Address.State(),
            ImageUrl = "https://images.unsplash.com/photo-1512917774080-9991f1c4c750",
            StripeAccountId = expressAccountId,
            ReferenceCode = ReferenceCodeGenerator.GetUniqueKey()
         });

      // Hub Rental #5
      await CreateHubAsync(context,
         new HubRental {
            Title = $"Single Bedroom Apartment by {companyName}",
            Description = _placeHolderDescription,
            Capacity = this._faker.Random.Number(1, 10),
            HubType = RentalType.Room,
            HubTier = RentalTier.Standard,
            Location = this._faker.Address.State(),
            ImageUrl = "https://images.unsplash.com/photo-1554995207-c18c203602cb",
            StripeAccountId = expressAccountId,
            ReferenceCode = ReferenceCodeGenerator.GetUniqueKey()
         });
   }

   private async Task CreateHubAsync(OasisHubsDbContext context, HubRental rental) {
      if (rental == null) throw new ArgumentNullException(nameof(rental));
      // Save product in the database
      context.HubRentals.Add(rental);
      await context.SaveChangesAsync();
      this._logger.LogDebug("Add Hub rental record {RentalName} to database", rental.Title);
   }

   private async Task CreateHubTierProductsAsync() {
      this._logger.LogInformation("Creating product tiers ...");

      await CreateHubTierAsync("Oasis Basic", "Oasis Basic Tier", 3500,
         new[] { "Cable Internet", "Shared Workspace", "Coffee and Tea" }, "basic_tier",
         "oasis_basic_tier.png");

      await CreateHubTierAsync("Oasis Standard", "Oasis Standard Tier", 6000,
         new[] { "Standing Desk", "Private Office", "Snacks and Drinks" }, "standard_tier",
         "oasis_standard_tier.png");

      await CreateHubTierAsync("Oasis Premium", "Oasis Premium Tier", 12000,
         new[] {
            "High Speed Fiber Optic Internet", "Whiteboards", "Private Team Workspace", "Catering"
         }, "premium_tier", "oasis_premium_tier.png");
   }

   private async Task CreateHubTierAsync(string title, string description, long hourlyUnitPrice,
      IEnumerable<string> features, string priceLookupPrefix, string imageFileName) {
      // locate and upload image
      var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images",
         imageFileName);

      await using var stream = System.IO.File.OpenRead(imagePath);
      var fileCreateOptions =
         new FileCreateOptions { File = stream, Purpose = FilePurpose.BusinessLogo };
      var fileService = new FileService();
      var createdFile = await fileService.CreateAsync(fileCreateOptions);
      this._logger.LogDebug("Uploading image file ({ImageFileName}) to stripe.", imageFileName);

      // Create file link
      var fileLinkOptions = new FileLinkCreateOptions { File = createdFile.Id };
      var service = new FileLinkService();
      var fileLink = await service.CreateAsync(fileLinkOptions);


      // Create tier subscription in Stripe
      this._logger.LogDebug("Creating product {ProductName} and attaching image file.", title);
      var prodCreateOptions = new ProductCreateOptions {
         Name = title,
         Description = description,
         Images = new List<string> { fileLink.Url },
         Features = features.Select(f => new ProductFeatureOptions { Name = f }).ToList(),
         UnitLabel = "hour",
         Metadata =
            new Dictionary<string, string> { ["hub.tier"] = "true", ["tier.image"] = imageFileName }
      };

      var newHubProduct = await this._productsService.CreateAsync(prodCreateOptions);

      // Create flat price in product
      var priceCreateOptions = new PriceCreateOptions {
         Product = newHubProduct.Id,
         Nickname = newHubProduct.Name,
         Currency = "usd",
         UnitAmount = hourlyUnitPrice,
         LookupKey = $"{priceLookupPrefix}_usd",
         Recurring = new PriceRecurringOptions { Interval = "month", UsageType = "licensed" }
      };

      var newProductPrice = await this._priceService.CreateAsync(priceCreateOptions);

      // Update default price
      await this._productsService.UpdateAsync(newHubProduct.Id,
         new ProductUpdateOptions { DefaultPrice = newProductPrice.Id });
      this._logger.LogDebug("Price ({PriceId}) created and set as default.", newProductPrice.Id);

      // Create tiered pricing in product
      priceCreateOptions = new PriceCreateOptions {
         Product = newHubProduct.Id,
         Nickname = newHubProduct.Name,
         LookupKey = $"{priceLookupPrefix}_usd_tiered",
         Currency = "usd",
         Tiers = new List<PriceTierOptions> {
            new() { UnitAmount = 0, UpTo = 10 },
            new() { UnitAmount = hourlyUnitPrice / 10, UpTo = PriceTierUpTo.Inf }
         },
         Recurring = new PriceRecurringOptions { Interval = "month", UsageType = "metered" },
         TiersMode = "graduated", BillingScheme = "tiered"
      };

      newProductPrice = await this._priceService.CreateAsync(priceCreateOptions);
      this._logger.LogDebug("Metered price ({PriceId}) created ", newProductPrice.Id);
   }
}
