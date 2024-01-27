namespace OasisHubs.Site.Messaging;

public static class MessagingConstants {
   public const string DEFAULT_DLQ_EXCHANGE = "oasis.brighter.exchange.dlq";
   public const string DEFAULT_DLQ_ROUTING_KEY = "oasis.dlq";
   public const string DEFAULT_DLQ_CHANNEL = "OasisDLQ";
   public const string DEFAULT_EXCHANGE= "oasis.brighter.exchange";

   public const string HOST_UPDATED_TOPIC= "host.updated";
   public const string HOST_UPDATE_CHANNEL= "HostUpdates";
   public const string HOST_UPDATE_SUBSCRIPTION= "oasis.brighter.host.updates.subscription";

   public const string FUNDS_TRANSFER_TOPIC= "funds.transfer";
   public const string FUNDS_TRANSFER_CHANNEL= "FundsTranser";
   public const string FUNDS_TRANSFER_SUBSCRIPTION= "oasis.brighter.funds.transfer.subscription";

   public const string SUBSCRIPTION_ACTIVATED_TOPIC= "subscription.activated";
   public const string SUBSCRIPTION_ACTIVATED_CHANNEL= "SubscriptionActivated";
   public const string SUBSCRIPTION_ACTIVATED_SUBSCRIPTION= "oasis.brighter.subscription.activated.subscription";
}
