using Microsoft.AspNetCore.Mvc;
using OasisHubs.Site.Messaging;
using Paramore.Brighter;
using Stripe;
using Subscription = Stripe.Subscription;

namespace OasisHubs.Site.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WebhooksController : ControllerBase {
   private readonly IAmACommandProcessor _commandProcessor;
   private readonly IConfigurationSection _stripeConfiguration;
   private readonly ILogger<WebhooksController> _logger;

   public WebhooksController(IAmACommandProcessor commandProcessor,
      IConfiguration configuration, ILogger<WebhooksController> logger) {
      this._commandProcessor = commandProcessor;
      this._stripeConfiguration =
         configuration.GetRequiredSection("Stripe");
      this._logger = logger;
   }

   [HttpPost("stripe/platform")]
   public async Task<IActionResult> PlatformHandler() {
      var payload = await new StreamReader(Request.Body).ReadToEndAsync();
      try {
         var stripeEvent = EventUtility.ConstructEvent(payload,
            Request.Headers["Stripe-Signature"],
            this._stripeConfiguration.GetValue<string>("WebhookSecret")
         );

         switch (stripeEvent.Type) {
            case Events.InvoicePaid: {
               var invoice = (stripeEvent.Data.Object as Invoice)!;
               if (invoice is { Status: "paid" }) {
                  this._logger.LogDebug(
                     "Initiating funds transfer for paid invoice ({InvoiceId})",
                     invoice.Id);
                  await this._commandProcessor.PostAsync(new InitiateFundsTransferCommand(invoice));
               }
               break;
            }
            case Events.CustomerSubscriptionCreated: {
               var newSubscription = (stripeEvent.Data.Object as Subscription)!;

               if (newSubscription is { Status: "active" }) {
                  this._logger.LogDebug(
                     "A new stripe subscription has been activated ({SubscriptionId})",
                     newSubscription.Id);
                  await this._commandProcessor.PostAsync(
                     new ActivateCustomerSubscriptionCommand(newSubscription));
               }

               break;
            }
            case Events.CustomerSubscriptionUpdated: {
               var updatedSubscription = (stripeEvent.Data.Object as Subscription)!;
               if (updatedSubscription is { Status: "active" }) {
                  this._logger.LogDebug(
                     "A new stripe subscription has been activated ({SubscriptionId})",
                     updatedSubscription.Id);
                  await this._commandProcessor.PostAsync(
                     new ActivateCustomerSubscriptionCommand(updatedSubscription));
               }
               break;
            }
            default:
               _logger.LogInformation("Unhandled event type: {StripeEvent}", stripeEvent.Type);
               break;
         }

         _logger.LogInformation("Webhook notification with type: {EventType} found for {EventId}",
            stripeEvent.Type, stripeEvent.Id);
         return Ok();
      }
      catch (StripeException ex) {
         _logger.LogError(ex, "There was an issue processing this webhook request");
         return BadRequest();
      }
   }

   [HttpPost("stripe/connect")]
   public async Task<IActionResult> ConnectHandler() {
      var payload = await new StreamReader(Request.Body).ReadToEndAsync();
      try {
         var stripeEvent = EventUtility.ConstructEvent(payload,
            Request.Headers["Stripe-Signature"],
            this._stripeConfiguration.GetValue<string>("WebhookSecret")
         );

         switch (stripeEvent.Type) {
            case Events.AccountUpdated: {
               var updatedAccount = (stripeEvent.Data.Object as Account)!;
               if (updatedAccount is { DetailsSubmitted: true, ChargesEnabled: true })
                  await this._commandProcessor.PostAsync(
                     new ActivateHostAccountCommand(updatedAccount));

               break;
            }

            default:
               _logger.LogInformation("Unhandled event type: {StripeEvent}", stripeEvent.Type);
               break;
         }

         _logger.LogInformation("Webhook notification with type: {EventType} found for {EventId}",
            stripeEvent.Type, stripeEvent.Id);
         return Ok();
      }
      catch (StripeException ex) {
         _logger.LogError(ex, "There was an issue processing this webhook request");
         return BadRequest();
      }
   }
}
