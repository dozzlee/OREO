using Coded.Events.WebApi.Utils;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.DataHandler.Encoder;
using System;
using System.Configuration;
//using System.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading;

namespace Coded.Events.WebApi.Providers
{
    public class SimpleJwtFormat : ISecureDataFormat<AuthenticationTicket>
    {

        private readonly string _issuer = string.Empty;
        private readonly OpenIdConnectConfiguration _openIdConfig;

        public SimpleJwtFormat(string issuer)
        {
            _issuer = issuer;

            TestIConfRetriever openIdConnectConfigurationRetriever = new TestIConfRetriever(issuer);
            _openIdConfig = openIdConnectConfigurationRetriever.GetAsGetConfiguration(CancellationToken.None);

        }

        //public static string GenerateToken(int expireMinutes)
        //{
        //    X509Certificate2 signingCert = new X509Certificate2("PFXFilePath", "password");
        //    X509SecurityKey privateKey = new X509SecurityKey(signingCert);
        //    var now = DateTime.UtcNow;
        //    var tokenHandler = new JwtSecurityTokenHandler();
        //    var tokenDescriptor = new SecurityTokenDescriptor
        //    {
        //        Expires = now.AddMinutes(Convert.ToInt32(expireMinutes)),
        //        SigningCredentials = new SigningCredentials(privateKey, SecurityAlgorithms.RsaSha256Signature)
        //    };
        //    JwtSecurityToken stoken = (JwtSecurityToken)tokenHandler.CreateToken(tokenDescriptor);
        //    string token = tokenHandler.WriteToken(stoken);
        //    return token;
        //}

        public string Protect(AuthenticationTicket data)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            string audienceId = ConfigurationManager.AppSettings["as:AudienceId"];

            string symmetricKeyAsBase64 = ConfigurationManager.AppSettings["as:AudienceSecret"];
            var keyByteArray = TextEncodings.Base64Url.Decode(symmetricKeyAsBase64);

            //var signingKey = new HmacSigningCredentials(keyByteArray);
            var securityKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(keyByteArray);
            securityKey.KeyId = ConfigurationManager.AppSettings["as:AudienceId"];

            var signingCredentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256Signature);

            var issued = data.Properties.IssuedUtc;

            var expires = data.Properties.ExpiresUtc;
            var token = new JwtSecurityToken(_issuer, audienceId, data.Identity.Claims, issued.Value.UtcDateTime, expires.Value.UtcDateTime, signingCredentials);

            var handler = new JwtSecurityTokenHandler();

            var jwt = handler.WriteToken(token);
            return jwt;
        }

        public AuthenticationTicket Unprotect(string protectedText)
        {
            string symmetricKeyAsBase64 = ConfigurationManager.AppSettings["as:AudienceSecret"];
            string audienceId = ConfigurationManager.AppSettings["as:AudienceId"];
            string authority = ConfigurationManager.AppSettings["Authority"];

            var keyByteArray = TextEncodings.Base64Url.Decode(symmetricKeyAsBase64);
            var signingKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(keyByteArray);

            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidAudience = audienceId,
                ValidIssuer = _issuer,
                IssuerSigningKey = signingKey,
                ValidateLifetime = true,
                ValidateAudience = true,
                ValidateIssuer = true,
                RequireSignedTokens = true,
                RequireExpirationTime = true,
                ValidateIssuerSigningKey = true,
                ClockSkew = TimeSpan.Zero
            };


            var handler = new JwtSecurityTokenHandler();
            SecurityToken token = null;

            // Unpack token
            JwtSecurityToken pt = handler.ReadJwtToken(protectedText);
            string rawToken = pt.RawData;

            var principal = handler.ValidateToken(rawToken, tokenValidationParameters, out token);

            var identity = principal.Identities;

            return new AuthenticationTicket(identity.First(), new AuthenticationProperties());
        }
    }
}