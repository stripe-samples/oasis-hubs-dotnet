using System.Net.Mime;
using System.Runtime.Serialization;
using System.Text.Json;
using Paramore.Brighter;

namespace OasisHubs.Site.Messaging;

public class InitiateFundsTransferMessageMapper: IAmAMessageMapper<InitiateFundsTransferCommand> {
   public Message MapToMessage(InitiateFundsTransferCommand request) {
      var header = new MessageHeader(messageId: request.Id, topic: MessagingConstants.FUNDS_TRANSFER_TOPIC,
         contentType: MediaTypeNames.Application.Json, messageType: MessageType.MT_COMMAND);

      var payload = JsonSerializer.Serialize(request, JsonSerialisationOptions.Options);
      var body = new MessageBody(payload);

      var message = new Message(header, body);
      return message;
   }

   public InitiateFundsTransferCommand MapToRequest(Message message) {
      using var jDoc= JsonSerializer.Deserialize<JsonDocument>(message.Body.Value, JsonSerialisationOptions.Options);
      if (jDoc is null)
         throw new SerializationException($"{nameof(InitiateFundsTransferMessageMapper)} could not deserialize command");

      var guid = jDoc.RootElement.GetProperty("id").GetGuid();
      var slimInvoice = jDoc.RootElement.GetProperty("invoice").Deserialize<InitiateFundsTransferCommand.SlimInvoice>(JsonSerialisationOptions.Options);
      var command = new InitiateFundsTransferCommand {
         Id = guid,
         Invoice = slimInvoice ?? throw new SerializationException($"{nameof(InitiateFundsTransferMessageMapper)} could not deserialize command")
      };
      
      return command;
   }
}
