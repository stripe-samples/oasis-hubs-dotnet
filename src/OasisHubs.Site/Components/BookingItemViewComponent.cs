using Microsoft.AspNetCore.Mvc;
using OasisHubs.Site.Data;

namespace OasisHubs.Site.Components;

[ViewComponent]
public class BookingItemViewComponent : ViewComponent
{
   public IViewComponentResult Invoke(Booking userBooking) {
      return View(new BookingItemVModel(userBooking));
   }

   public record BookingItemVModel(Booking UserBooking);
}
