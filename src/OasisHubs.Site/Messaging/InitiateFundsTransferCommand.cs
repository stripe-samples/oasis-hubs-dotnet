using Paramore.Brighter;
using Stripe;

namespace OasisHubs.Site.Messaging;

public class InitiateFundsTransferCommand : Command {
   public SlimInvoice Invoice { get; init; }
   public InitiateFundsTransferCommand(Invoice invoice) : base(Guid.NewGuid()) {
      this.Invoice = new SlimInvoice(invoice.Id, invoice.ChargeId,invoice.PeriodStart, invoice.PeriodEnd, invoice.Total);
   }
   public InitiateFundsTransferCommand() : base(Guid.NewGuid()) {
      this.Invoice = default!;
   }

   public record SlimInvoice(string InvoiceId, string ChargeId, DateTime Start, DateTime End, long Total);
}
