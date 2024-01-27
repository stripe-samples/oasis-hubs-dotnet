using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Diagnostics.CodeAnalysis;

namespace OasisHubs.Site.TagHelpers;

[HtmlTargetElement("*", Attributes = SecureAttributeName)]
[HtmlTargetElement("*", Attributes = SecurePolicyAttributeName)]
public class SecureTagHelper : TagHelper
{
    private const string SecureAttributeName = "secure";
    private const string SecurePolicyAttributeName = "secure-policy";

    private readonly IAuthorizationService _authz;


    [HtmlAttributeName(SecureAttributeName)]
    public bool RequiresAuthentication { get; set; }


    [HtmlAttributeName(SecurePolicyAttributeName)]
    public string RequiredPolicy { get; set; } = string.Empty;

    [HtmlAttributeNotBound]
    [ViewContext]
    public ViewContext ViewContext { get; set; } = default!;

    public SecureTagHelper(IAuthorizationService authz)
    {
        _authz = authz;
    }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        var showOutput = false;

        var user = ViewContext.HttpContext.User;
        if (!string.IsNullOrEmpty(RequiredPolicy))
        {
            // secure-policy="foo" & user is authorized for policy "foo"
            var cacheKey = SecurePolicyAttributeName + "." + RequiredPolicy;
            bool authorized;
            var cachedResult = ViewContext.ViewData[cacheKey];
            if (cachedResult != null)
            {
                authorized = (bool)cachedResult;
            }
            else
            {
                var authResult = await _authz.AuthorizeAsync(user, ViewContext, RequiredPolicy);
                authorized = authResult.Succeeded;
                ViewContext.ViewData[cacheKey] = authorized;
            }

            showOutput = authorized;
        }
        else if (RequiresAuthentication && (user?.Identity?.IsAuthenticated ?? false))
        {
            // secure="true" & user is authenticated
            showOutput = true;
        }

        if (!showOutput)
        {
            output.SuppressOutput();
        }
    }
}
