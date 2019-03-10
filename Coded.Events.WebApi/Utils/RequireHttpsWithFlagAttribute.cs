using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;

namespace Coded.Events.WebApi.Utils
{
    public class RequireHttpsWithFlagAttribute : RequireHttpsAttribute
    {
        public bool RequireSsl { get; set; }

        public RequireHttpsWithFlagAttribute()
        {
            // Assign from App specific configuration object
            RequireSsl = false;
        }

        public RequireHttpsWithFlagAttribute(bool requireSsl)
        {
            RequireSsl = requireSsl;
        }

        public override void OnAuthorization(AuthorizationFilterContext filterContext)
        {
            if (filterContext != null &&
                RequireSsl &&
                !filterContext.HttpContext.Request.IsHttps)
            {
                HandleNonHttpsRequest(filterContext);
            }
        }
    }
}
