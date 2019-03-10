using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;

namespace Coded.Events.WebApi.Utils
{
    public class TestIConfRetriever : OpenIdConnectConfigurationRetriever
    {
        private string authority;

        public TestIConfRetriever(string authority)
        {

            if (!authority.EndsWith("/", StringComparison.Ordinal))
            {
                authority += "/";
            }

            authority += ".well-known/openid-configuration";

            this.authority = new Uri(string.Concat(authority)).ToString();

        }

        public new static async Task<OpenIdConnectConfiguration> GetAsync(string address, IDocumentRetriever retriever, CancellationToken cancel)
        {
            if (string.IsNullOrWhiteSpace(address))
                throw LogHelper.LogArgumentNullException(nameof(address));

            if (retriever == null)
            {
                throw LogHelper.LogArgumentNullException(nameof(retriever));
            }

            //string doc = await retriever.GetDocumentAsync(address, cancel).ConfigureAwait(false);
            TestIDocumentRetriever documentRetriever = new TestIDocumentRetriever();
            retriever = documentRetriever;
            string doc = await documentRetriever.GetDocumentAsync(address, cancel).ConfigureAwait(false);

            LogHelper.LogVerbose("LogMessages.IDX21811", doc);
            OpenIdConnectConfiguration openIdConnectConfiguration = JsonConvert.DeserializeObject<OpenIdConnectConfiguration>(doc);
            if (!string.IsNullOrEmpty(openIdConnectConfiguration.JwksUri))
            {
                LogHelper.LogVerbose("LogMessages.IDX21812", openIdConnectConfiguration.JwksUri);
                string keys = await retriever.GetDocumentAsync(openIdConnectConfiguration.JwksUri, cancel).ConfigureAwait(false);

                LogHelper.LogVerbose("LogMessages.IDX21813", openIdConnectConfiguration.JwksUri);
                openIdConnectConfiguration.JsonWebKeySet = JsonConvert.DeserializeObject<JsonWebKeySet>(keys);
                foreach (SecurityKey key in openIdConnectConfiguration.JsonWebKeySet.GetSigningKeys())
                {
                    openIdConnectConfiguration.SigningKeys.Add(key);
                }
            }

            return openIdConnectConfiguration;

            //return await OpenIdConnectConfigurationRetriever.GetAsync(address, retriever, cancel);
        }


        public OpenIdConnectConfiguration GetAsGetConfiguration(CancellationToken cancel)
        {
            string address = this.authority;

            Task<OpenIdConnectConfiguration> config;
            HttpDocumentRetriever retriever = new HttpDocumentRetriever();
            retriever.RequireHttps = false;

            config = GetAsync(address, retriever, CancellationToken.None);

            return config.Result;
        }
    }
}
