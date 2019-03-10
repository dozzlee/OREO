using System;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Coded.Events.WebApi.Utils
{
    public class DynamicConfigurationManager : IConfigurationManager<OpenIdConnectConfiguration>
    {
        private ConfigurationManager<OpenIdConnectConfiguration> _inner;
        private string authority;

        public DynamicConfigurationManager(string authority)
        {
            this.authority = authority;
        }

        public async Task<OpenIdConnectConfiguration> GetConfigurationAsync(CancellationToken cancel)
        {
            if (_inner == null)
            {
                 //figure out which authority you need here...
                string metadataAddress = authority;

                if (!metadataAddress.EndsWith("/", StringComparison.Ordinal))
                {
                    metadataAddress += "/";
                }

                metadataAddress += ".well-known/openid-configuration";

                string endPoint = new Uri(string.Concat(metadataAddress)).ToString();

                TestIConfRetriever openIdConnectConfigurationRetriever = new TestIConfRetriever(authority);
                openIdConnectConfigurationRetriever.GetAsGetConfiguration(CancellationToken.None);

                _inner = new ConfigurationManager<OpenIdConnectConfiguration>(endPoint, openIdConnectConfigurationRetriever);
            }

            return await _inner.GetConfigurationAsync(cancel);
        }

        public void RequestRefresh()
        {
            _inner.RequestRefresh();
        }
    }
}
