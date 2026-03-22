using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Askyl.Dsm.WebHosting.Constants.Application;

namespace Askyl.Dsm.WebHosting.Ui.Authorization;

/// <summary>
/// Authorizes access only if the user has an active server-side session.
/// Checks for "DsmSid" in HttpContext.Session.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class AuthorizeSessionAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var session = context.HttpContext.Session;

        // Check if user is authenticated via session (DsmSid must exist)
        var sessionId = session.GetString(ApplicationConstants.DsmSessionKey);

        if (String.IsNullOrEmpty(sessionId))
        {
            context.Result = new ForbidResult();
        }
    }
}
