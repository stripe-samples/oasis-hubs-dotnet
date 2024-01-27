using System.Net.Mime;
using System.Runtime.Serialization;
using System.Text.Json;
using Paramore.Brighter;

namespace OasisHubs.Site.Messaging;

public class ActiveHostAccountMessageMapper : IAmAMessageMapper<ActivateHostAccountCommand> {
   public Message MapToMessage(ActivateHostAccountCommand request) {
      var header = new MessageHeader(messageId: request.Id, topic: MessagingConstants.HOST_UPDATED_TOPIC,
         contentType: MediaTypeNames.Application.Json, messageType: MessageType.MT_COMMAND);

      var payload = JsonSerializer.Serialize(request, JsonSerialisationOptions.Options);
      var body = new MessageBody(payload);

      var message = new Message(header, body);
      return message;
   }

   public ActivateHostAccountCommand MapToRequest(Message message) {
      using var jDoc= JsonSerializer.Deserialize<JsonDocument>(message.Body.Value, JsonSerialisationOptions.Options);
      if (jDoc is null)
         throw new SerializationException($"{nameof(ActiveHostAccountMessageMapper)} could not deserialize command");

      var guid = jDoc.RootElement.GetProperty("id").GetGuid();
      var slimAccount = jDoc.RootElement.GetProperty("account").Deserialize<ActivateHostAccountCommand.SlimAccount>(JsonSerialisationOptions.Options);
      var command = new ActivateHostAccountCommand {
         Id = guid,
         Account = slimAccount ?? throw new SerializationException($"{nameof(ActiveHostAccountMessageMapper)} could not deserialize command")
      };

      return command;
   }
}
