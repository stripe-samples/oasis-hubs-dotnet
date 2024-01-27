using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OasisHubs.Site.Data;
using OasisHubs.Site.Messaging;
using OasisHubs.Site.Policies;
using Paramore.Brighter;
using Paramore.Brighter.Extensions.DependencyInjection;
using Paramore.Brighter.MessagingGateway.RMQ;
using Paramore.Brighter.ServiceActivator.Extensions.DependencyInjection;
using Paramore.Brighter.ServiceActivator.Extensions.Hosting;
using RabbitMQ.Client;
using Serilog;
using Stripe;

namespace OasisHubs.Site;

internal static class Extensions {
   public static WebApplication ConfigureServices(this WebApplicationBuilder builder) {
      builder.Host.UseSerilog((context, _, configuration) => configuration
         .ReadFrom.Configuration(context.Configuration));

      builder.Services.AddCoreServices(builder.Configuration);
      builder.Services.AddMessagingService(builder.Configuration);

      builder.Services.AddDatabaseDeveloperPageExceptionFilter();

      builder.Services.AddAuthorizationBuilder()
         .AddPolicy("is_host_policy",
            policy => policy.AddRequirements(new HostAuthPolicyRequirement()))
         .AddPolicy("can_view_listings",
            policy => policy.AddRequirements(new CanViewListingsPolicyRequirement()));

      builder.Services.ConfigureApplicationCookie(options => {
         options.Cookie.HttpOnly = true;
         options.ExpireTimeSpan = TimeSpan.FromMinutes(20);
         options.SlidingExpiration = true;
         options.LoginPath = "/Signin";
         options.AccessDeniedPath = "/Pricing"; //TODO: Need a better option for this
      });

      builder.Services.AddSession(options => {
         options.IdleTimeout = TimeSpan.FromSeconds(20);
         options.Cookie.Name = "OasisHubsSession";
         options.Cookie.HttpOnly = true;
         options.Cookie.IsEssential = false;
         options.Cookie.SameSite = SameSiteMode.Strict;
      });

      builder.Services.Configure<HostOptions>(options =>
         options.ShutdownTimeout = TimeSpan.FromSeconds(20));

      builder.Services.Configure<RouteOptions>(options => {
         options.LowercaseQueryStrings = true;
         options.LowercaseUrls = true;
      });

      builder.Services.AddRazorPages(options => {
         options.Conventions.AuthorizePage("/Hosts/SignUp");
         options.Conventions.AuthorizeFolder("/Dashboard", "is_host_policy");
      });

      builder.Services.AddControllers();
      return builder.Build();
   }

   public static WebApplication ConfigurePipeline(this WebApplication app) {
      if (app.Environment.IsDevelopment()) {
         app.UseMigrationsEndPoint();
      }
      else {
         app.UseExceptionHandler("/Error");
         app.UseHsts();
      }

      app.UseStaticFiles();

      app.UseRouting();

      app.UseAuthentication();
      app.UseAuthorization();

      app.UseSession();

      app.MapControllers();
      app.MapRazorPages();

      return app;
   }

   public static IServiceCollection AddStripe(this IServiceCollection services,
      IConfiguration config) {
      StripeConfiguration.ApiKey = config.GetValue<string>("SecretKey");

      var appInfo = new AppInfo { Name = "Oasis Hubs", Version = "0.1.0" };
      StripeConfiguration.AppInfo = appInfo;

      services.AddHttpClient("Stripe");
      services.AddTransient<IStripeClient, StripeClient>(s => {
         var clientFactory = s.GetRequiredService<IHttpClientFactory>();
         var httpClient = new SystemNetHttpClient(
            httpClient: clientFactory.CreateClient("Stripe"),
            maxNetworkRetries: StripeConfiguration.MaxNetworkRetries,
            appInfo: appInfo,
            enableTelemetry: StripeConfiguration.EnableTelemetry);

         return new StripeClient(apiKey: StripeConfiguration.ApiKey, httpClient: httpClient);
      });

      return services;
   }

   public static IServiceCollection AddCoreServices(this IServiceCollection services,
      IConfiguration config) {
      services.AddDbContextPool<OasisHubsDbContext>(options =>
         options.UseSqlServer(config.GetConnectionString("OasisHubsSQLServer"),
            opts => opts.EnableRetryOnFailure()));

      services.AddPooledDbContextFactory<OasisHubsDbContext>(options =>
         options.UseSqlServer(config.GetConnectionString("OasisHubsSQLServer"),
            opts => opts.EnableRetryOnFailure()));

      services.AddIdentity<OasisHubsUser, IdentityRole>(options => {
            options.User.RequireUniqueEmail = true;
         })
         .AddDefaultTokenProviders()
         .AddEntityFrameworkStores<OasisHubsDbContext>();

      services.Configure<IdentityOptions>(options => {
         // Relax default password settings.
         options.Password.RequireDigit = false;
         options.Password.RequireLowercase = false;
         options.Password.RequireNonAlphanumeric = false;
         options.Password.RequireUppercase = false;
         options.Password.RequiredLength = 3;
         options.Password.RequiredUniqueChars = 0;
      });

      services.AddStripe(config.GetSection("Stripe"));
      services.AddDistributedMemoryCache();

      return services;
   }

   public static IServiceCollection AddMessagingService(this IServiceCollection services,
      IConfiguration configuration) {
      var rabbitConnectionString = configuration.GetConnectionString("OasisHubsRabbitMQ");
      if (rabbitConnectionString is null)
         throw new Exception("RabbitMQ Connection information missing");

      var rmqMessagingGatewayConnection = new RmqMessagingGatewayConnection {
         Name = "OasisHubsRMQConnection",
         AmpqUri = new AmqpUriSpecification(new Uri(rabbitConnectionString),
            connectionRetryCount: 5, retryWaitInMilliseconds: 250),
         //https://www.rabbitmq.com/tutorials/amqp-concepts.html#exchange-direct
         Exchange = new Exchange(MessagingConstants.DEFAULT_EXCHANGE, ExchangeType.Direct, true),
         DeadLetterExchange =
            new Exchange(MessagingConstants.DEFAULT_DLQ_EXCHANGE, ExchangeType.Fanout, true),
         Heartbeat = 15,
         PersistMessages = true
      };

      // Configure command processor
      services.AddBrighter(options => {
            options.HandlerLifetime = ServiceLifetime.Scoped;
            options.CommandProcessorLifetime = ServiceLifetime.Scoped;
            options.MapperLifetime = ServiceLifetime.Singleton;
         })
         .UseExternalBus(new RmqProducerRegistryFactory(
            rmqMessagingGatewayConnection,
            new[] {
               new RmqPublication {
                  Topic = new RoutingKey(MessagingConstants.HOST_UPDATED_TOPIC),
                  MaxOutStandingMessages = 20,
                  WaitForConfirmsTimeOutInMilliseconds = 10000,
                  MakeChannels = OnMissingChannel.Create
               },
               new RmqPublication {
                  Topic = new RoutingKey(MessagingConstants.SUBSCRIPTION_ACTIVATED_TOPIC),
                  MaxOutStandingMessages = 20,
                  WaitForConfirmsTimeOutInMilliseconds = 10000,
                  MakeChannels = OnMissingChannel.Create
               },
               new RmqPublication {
                  Topic = new RoutingKey(MessagingConstants.FUNDS_TRANSFER_TOPIC),
                  MaxOutStandingMessages = 20,
                  WaitForConfirmsTimeOutInMilliseconds = 10000,
                  MakeChannels = OnMissingChannel.Create
               }
            }).Create())
         .AutoFromAssemblies();

      // Configure activator
      var subscriptions = new Paramore.Brighter.Subscription[] {
         new RmqSubscription<ActivateHostAccountCommand>(
            new SubscriptionName(MessagingConstants.HOST_UPDATE_SUBSCRIPTION),
            new ChannelName(MessagingConstants.HOST_UPDATE_CHANNEL),
            new RoutingKey(MessagingConstants.HOST_UPDATED_TOPIC),
            deadLetterChannelName: new ChannelName(MessagingConstants.DEFAULT_DLQ_CHANNEL),
            deadLetterRoutingKey: MessagingConstants.DEFAULT_DLQ_ROUTING_KEY,
            requeueCount: 5,
            runAsync: true, isDurable: true,
            makeChannels: OnMissingChannel.Create),
         new RmqSubscription<ActivateCustomerSubscriptionCommand>(
            new SubscriptionName(MessagingConstants.SUBSCRIPTION_ACTIVATED_SUBSCRIPTION),
            new ChannelName(MessagingConstants.SUBSCRIPTION_ACTIVATED_CHANNEL),
            new RoutingKey(MessagingConstants.SUBSCRIPTION_ACTIVATED_TOPIC),
            deadLetterChannelName: new ChannelName(MessagingConstants.DEFAULT_DLQ_CHANNEL),
            deadLetterRoutingKey: MessagingConstants.DEFAULT_DLQ_ROUTING_KEY,
            requeueCount: 5,
            runAsync: true, isDurable: true,
            makeChannels: OnMissingChannel.Create),
         new RmqSubscription<InitiateFundsTransferCommand>(
            new SubscriptionName(MessagingConstants.FUNDS_TRANSFER_SUBSCRIPTION),
            new ChannelName(MessagingConstants.FUNDS_TRANSFER_CHANNEL),
            new RoutingKey(MessagingConstants.FUNDS_TRANSFER_TOPIC),
            deadLetterChannelName: new ChannelName(MessagingConstants.DEFAULT_DLQ_CHANNEL),
            deadLetterRoutingKey: MessagingConstants.DEFAULT_DLQ_ROUTING_KEY,
            requeueCount: 5,
            runAsync: true, isDurable: true,
            makeChannels: OnMissingChannel.Create)
      };
      services.AddServiceActivator(options => {
            var rmqMessageConsumerFactory =
               new RmqMessageConsumerFactory(rmqMessagingGatewayConnection);

            options.UseScoped = true;
            options.HandlerLifetime = ServiceLifetime.Scoped;
            options.MapperLifetime = ServiceLifetime.Singleton;
            options.CommandProcessorLifetime = ServiceLifetime.Scoped;
            options.ChannelFactory = new ChannelFactory(rmqMessageConsumerFactory);
            options.Subscriptions = subscriptions;
         })
         .UseInMemoryInbox()
         .AutoFromAssemblies();

      services.AddHostedService<ServiceActivatorHostedService>();

      return services;
   }

   public static void Deconstruct<T>(this IGrouping<string, T> grouping,
      out string groupKey, out IEnumerable<T> collection) {
      groupKey = grouping.Key;
      collection = grouping;
   }
}
