using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Coded.Events.WebApi.Data;
using Coded.Events.WebApi.Entities;
using Coded.Events.WebApi.Infrastructure;
using Coded.Events.WebApi.Utils;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.OAuth;

namespace Coded.Events.WebApi.Providers
{
    public class SimpleAuthorizationServerProvider : OAuthAuthorizationServerProvider
    {
        public delegate Task<bool> ClientCredentialsValidationFunction(string clientid, string secret);
        public delegate Task<ClaimsIdentity> UserCredentialValidationFunction(string username, string password);
        public SimpleAuthorizationServerProviderOptions Options { get; private set; }

        public SimpleAuthorizationServerProvider(SimpleAuthorizationServerProviderOptions options)
        {
            if (options.ValidateUserCredentialsFunction == null)
            {
                throw new NullReferenceException("ValidateUserCredentialsFunction cannot be null");
            }
            Options = options;
        }

        public SimpleAuthorizationServerProvider(UserCredentialValidationFunction userCredentialValidationFunction)
        {
            Options = new SimpleAuthorizationServerProviderOptions()
            {
                ValidateUserCredentialsFunction = userCredentialValidationFunction
            };
        }

        public SimpleAuthorizationServerProvider(UserCredentialValidationFunction userCredentialValidationFunction, ClientCredentialsValidationFunction clientCredentialsValidationFunction)
        {
            Options = new SimpleAuthorizationServerProviderOptions()
            {
                ValidateUserCredentialsFunction = userCredentialValidationFunction,
                ValidateClientCredentialsFunction = clientCredentialsValidationFunction
            };
        }

        public override Task ValidateClientAuthentication(OAuthValidateClientAuthenticationContext context)
        {

            string client_Id = string.Empty;
            string clientSecret = string.Empty;
            Client client = null;

            // Get client from form
            if (!context.TryGetBasicCredentials(out client_Id, out clientSecret))
            {
                context.TryGetFormCredentials(out client_Id, out clientSecret);
            }

            using (AuthRepository _repo = new AuthRepository())
            {
                client = _repo.FindClient(client_Id);
            }

            if (client == null)
            {
                context.SetError("invalid_clientId", string.Format("Client '{0}' is not registered in the system.", context.ClientId));
                return Task.FromResult(0);
            }

            if (client.ApplicationType == Models.ApplicationTypes.NativeConfidential)
            {
                if (string.IsNullOrWhiteSpace(clientSecret))
                {
                    context.SetError("invalid_clientId", "Client secret should be sent.");
                    return Task.FromResult<object>(null);
                }
                else
                {
                    if (client.Secret != PasswordHasher.GetHash(clientSecret))
                    {
                        context.SetError("invalid_clientId", "Client secret is invalid.");
                        return Task.FromResult(0);
                    }
                }
            }

            if (!client.Active)
            {
                context.SetError("invalid_clientId", "Client is inactive.");
                return Task.FromResult(0);
            }

            context.OwinContext.Set<string>("as:clientAllowedOrigin", client.AllowedOrigin);
            context.OwinContext.Set<string>("as:clientRefreshTokenLifeTime", client.RefreshTokenLifeTime.ToString());

            context.Validated();
            return Task.FromResult(0);
        }

        public override async Task GrantResourceOwnerCredentials(OAuthGrantResourceOwnerCredentialsContext context)
        {
            if (Options.ValidateUserCredentialsFunction == null)
            {
                throw new NullReferenceException("ValidateUserCredentialsFunction cannot be null");
            }

            var identity = await Options.ValidateUserCredentialsFunction(context.UserName, context.Password);
            if (identity == null)
            {
                context.SetError("invalid_user", "Error authenticating user");
                await Task.FromResult(0);
            }

            // create metadata to pass to refresh token provider
            var props = new AuthenticationProperties(new Dictionary<string, string>()
            {
                { "as:client_id", context.ClientId }
            });

            var ticket = new AuthenticationTicket(identity, props);
            context.Validated(ticket);
        }

        public override Task GrantRefreshToken(OAuthGrantRefreshTokenContext context)
        {
            var originalClient = context.Ticket.Properties.Dictionary["as:client_id"];
            var currentClient = context.ClientId;

            // enforce client binding of refresh token
            if (originalClient != currentClient)
            {
                //context.Rejected();
                context.SetError("invalid_clientId", "Refresh token is issued to a different clientId.");
                return Task.FromResult(0);
            }

            // chance to change authentication ticket for refresh token requests
            var newIdentity = new ClaimsIdentity(context.Ticket.Identity);
            newIdentity.AddClaim(new Claim("newClaim", "refreshToken"));

            var newTicket = new AuthenticationTicket(newIdentity, context.Ticket.Properties);
            context.Validated(newTicket);
            return Task.FromResult(0);
        }

        public override Task TokenEndpoint(OAuthTokenEndpointContext context)
        {
            foreach (KeyValuePair<string, string> property in context.Properties.Dictionary)
            {
                context.AdditionalResponseParameters.Add(property.Key, property.Value);
            }

            return Task.FromResult<object>(null);
        }
    }
}
