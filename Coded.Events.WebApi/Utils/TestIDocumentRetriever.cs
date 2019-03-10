using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Protocols;

namespace Coded.Events.WebApi.Utils
{
    public class TestIDocumentRetriever : IDocumentRetriever
    {
        private readonly IDictionary localdict = new Dictionary<string, string>
        {
            [ "http://127.0.0.1:59822/.well-known/openid-configuration"] = string.Format("{0}{1}", AppDomain.CurrentDomain.BaseDirectory, "openid-configuration.json"),
            [ "http://127.0.0.1:59822/.well-known/jwks.json"] = string.Format("{0}{1}", AppDomain.CurrentDomain.BaseDirectory, "jwks.json"),
            ["http://localhost:59822/.well-known/openid-configuration"] = string.Format("{0}{1}", AppDomain.CurrentDomain.BaseDirectory, "openid-configuration.json"),
            ["http://localhost:59822/.well-known/jwks.json"] = string.Format("{0}{1}", AppDomain.CurrentDomain.BaseDirectory, "jwks.json")
        };

        public Task<string> GetDocumentAsync(string address, CancellationToken cancel)
        {
            return Task.FromResult(AsyncHelper.GetEmbeddedResource((string)localdict[address]));
        }

    }
}
