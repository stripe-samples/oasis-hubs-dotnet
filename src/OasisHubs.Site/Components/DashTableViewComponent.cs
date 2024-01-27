using Microsoft.AspNetCore.Mvc;
using OasisHubs.Site.Data;

namespace OasisHubs.Site.Components;

[ViewComponent]
public class DashTableViewComponent : ViewComponent
{
   public IViewComponentResult Invoke(Booking guestBooking) {
      return View(new BookingItemVModel(guestBooking));
   }

   public record BookingItemVModel(Booking GuestBooking);
}
