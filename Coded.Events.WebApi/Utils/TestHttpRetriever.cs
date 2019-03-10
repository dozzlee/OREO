using System;
using Microsoft.IdentityModel.Protocols;

namespace Coded.Events.WebApi.Utils
{
    public class TestHttpRetriever: HttpDocumentRetriever
    {
        public new bool RequireHttps
        {
            get;
            set;
        } = false;
    }
}
