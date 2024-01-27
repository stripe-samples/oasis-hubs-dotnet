using System.Net.Mime;
using System.Runtime.Serialization;
using System.Text.Json;
using Paramore.Brighter;

namespace OasisHubs.Site.Messaging;

public class ActivateCustomerSubscriptionMessageMapper : IAmAMessageMapper<ActivateCustomerSubscriptionCommand> {
   public Message MapToMessage(ActivateCustomerSubscriptionCommand request) {
      var header = new MessageHeader(messageId: request.Id, topic: MessagingConstants.SUBSCRIPTION_ACTIVATED_TOPIC,
         contentType: MediaTypeNames.Application.Json, messageType: MessageType.MT_COMMAND);

      var payload = JsonSerializer.Serialize(request, JsonSerialisationOptions.Options);
      var body = new MessageBody(payload);

      var message = new Message(header, body);
      return message;
   }

   public ActivateCustomerSubscriptionCommand MapToRequest(Message message) {
      using var jDoc= JsonSerializer.Deserialize<JsonDocument>(message.Body.Value, JsonSerialisationOptions.Options);
      if (jDoc is null)
         throw new SerializationException($"{nameof(ActivateCustomerSubscriptionMessageMapper)} could not deserialize command");

      var guid = jDoc.RootElement.GetProperty("id").GetGuid();
      var slimSubscription = jDoc.RootElement.GetProperty("customerSubscription").Deserialize<ActivateCustomerSubscriptionCommand.SlimSubscription>(JsonSerialisationOptions.Options);
      var command = new ActivateCustomerSubscriptionCommand {
         Id = guid,
         CustomerSubscription = slimSubscription ?? throw new SerializationException($"{nameof(ActivateCustomerSubscriptionMessageMapper)} could not deserialize command")
      };

      return command;
   }
}
