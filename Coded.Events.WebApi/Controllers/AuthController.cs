using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web.Http;
using System.Web.Http.Description;

namespace Coded.Events.WebApi.Controllers
{
    [RoutePrefix(".well-known")]
    public class AuthController : BaseApiController
    {
        [AllowAnonymous]
        [Route("openid-configuration")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public HttpResponseMessage Get()
        {
            string path = string.Format("{0}{1}", AppDomain.CurrentDomain.BaseDirectory, "openid-configuration.json");

            var resultset = Utils.AsyncHelper.GetEmbeddedResource(path); 
            HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(resultset, Encoding.UTF8, "application/json");

            return response;
        }

        [Route("jwks.json")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public HttpResponseMessage GetJwks()
        {
            string path = string.Format("{0}{1}", AppDomain.CurrentDomain.BaseDirectory, "jwks.json");
            var resultset = Utils.AsyncHelper.GetEmbeddedResource(path);
            HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(resultset, Encoding.UTF8, "application/json");

            return response;
        }
    }
}
