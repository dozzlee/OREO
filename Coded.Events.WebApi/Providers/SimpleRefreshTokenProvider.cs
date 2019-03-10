using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Coded.Events.WebApi.Data;
using Coded.Events.WebApi.Entities;
using Coded.Events.WebApi.Utils;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Infrastructure;

namespace Coded.Events.WebApi.Providers
{
    public class SimpleRefreshTokenProvider : IAuthenticationTokenProvider
    {
        private static ConcurrentDictionary<string, AuthenticationTicket> _refreshTokens = new ConcurrentDictionary<string, AuthenticationTicket>();

        public void Create(AuthenticationTokenCreateContext context)
        {

        }

        public async Task CreateAsync(AuthenticationTokenCreateContext context)
        {
            var clientid = context.Ticket.Properties.Dictionary["as:client_id"];
            var refreshTokenId = Guid.NewGuid().ToString("n");

            if (string.IsNullOrEmpty(clientid))
            {
                await Task.FromResult(0);
            }

            AuthRepository _repo = new AuthRepository();
            var refreshTokenLifeTime = context.OwinContext.Get<string>("as:clientRefreshTokenLifeTime");

            var token = new RefreshToken()
            {
                Id = PasswordHasher.GetHash(refreshTokenId),
                ClientId = clientid,
                Subject = context.Ticket.Identity.Name,
                IssuedUtc = DateTime.UtcNow,
                ExpiresUtc = DateTime.UtcNow.AddSeconds(Convert.ToDouble(refreshTokenLifeTime))
            };

            context.Ticket.Properties.IssuedUtc = token.IssuedUtc;
            context.Ticket.Properties.ExpiresUtc = token.ExpiresUtc;

            token.ProtectedTicket = context.SerializeTicket();

            var result = await _repo.AddRefreshToken(token);

            if (result)
            {
                context.SetToken(refreshTokenId);

            }
            await Task.FromResult(0);
        }

        public void Receive(AuthenticationTokenReceiveContext context)
        {

        }

        public async Task ReceiveAsync(AuthenticationTokenReceiveContext context)
        {

            var allowedOrigin = context.OwinContext.Get<string>("as:clientAllowedOrigin");
            context.OwinContext.Response.Headers.Add("Access-Control-Allow-Origin", new[] { allowedOrigin });

            string hashedTokenId = PasswordHasher.GetHash(context.Token);

            using (AuthRepository _repo = new AuthRepository())
            {
                var refreshToken = await _repo.FindRefreshToken(hashedTokenId);

                if (refreshToken != null)
                {
                    //Get protectedTicket from refreshToken class
                    context.DeserializeTicket(refreshToken.ProtectedTicket);
                    var result = await _repo.RemoveRefreshToken(hashedTokenId);
                }
            }

            await Task.FromResult(0);
        }
    }
}
