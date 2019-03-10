using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Microsoft.Owin.Security.DataHandler.Encoder;
using System.Threading;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Coded.Events.WebApi.Utils;

namespace Coded.Events.WebApi.Providers
{
    public class OpenIdConnectSigningKeyResolver
    {
        public readonly OpenIdConnectConfiguration openIdConfig;


        public OpenIdConnectSigningKeyResolver(string authority)
        {
            //Implemented IDocumentRetriever
            //var cm = new ConfigurationManager<OpenIdConnectConfiguration>(
            //$"{authority.TrimEnd('/')}/.well-known/openid-configuration", 
            //new OpenIdConnectConfigurationRetriever(),
            //new TestServerDocumentRetriever());

            //var cm = new ConfigurationManager<OpenIdConnectConfiguration>(
            //$"{authority.TrimEnd('/')}/.well-known/openid-configuration",
            //new OpenIdConnectConfigurationRetriever());
            //openIdConfig = AsyncHelper.RunSync(async () => await cm.GetConfigurationAsync(CancellationToken.None));

            TestIConfRetriever openIdConnectConfigurationRetriever = new TestIConfRetriever(authority);
            openIdConfig = openIdConnectConfigurationRetriever.GetAsGetConfiguration(CancellationToken.None);
        }

        public SecurityKey[] GetSigningKey(string kid)
        {
            // Find the security token which matches the identifier $"https://{Configuration["auth0:domain"]}/"
            string symmetricKeyAsBase64 = ConfigurationManager.AppSettings["as:AudienceSecret"];
            var keyByteArray = TextEncodings.Base64Url.Decode(symmetricKeyAsBase64);

            return new[] { new SymmetricSecurityKey(keyByteArray) };

            //var v = openIdConfig.JsonWebKeySet.GetSigningKeys();
            //foreach (var p in v)
            //{
            //    Console.WriteLine(p);
            //}
            //Console.WriteLine("Done");
            //return new[] { openIdConfig.JsonWebKeySet.GetSigningKeys().FirstOrDefault(t => t.KeyId == kid) };
        }
    }
}
