using Microsoft.AspNetCore.Mvc;

namespace OasisHubs.Site.Components;

[ViewComponent]
public class HeaderViewComponent : ViewComponent
{
    public IViewComponentResult Invoke() => View();
}

[ViewComponent]
public class FooterViewComponent : ViewComponent
{
    public IViewComponentResult Invoke() => View();

}

[ViewComponent]
public class CustomerReviewsViewComponent : ViewComponent
{
    public IViewComponentResult Invoke() => View();
}

[ViewComponent]
public class DashNavViewComponent : ViewComponent
{
    public IViewComponentResult Invoke() => View();
}
