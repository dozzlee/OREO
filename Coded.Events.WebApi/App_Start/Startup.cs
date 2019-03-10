using Coded.Events.WebApi.Infrastructure;
using Coded.Events.WebApi.Providers;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.DataHandler.Encoder;
using Microsoft.Owin.Security.Jwt;
using Microsoft.Owin.Security.OAuth;
using Owin;
using System;
using System.Configuration;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web.Http;

[assembly: OwinStartup(typeof(Coded.Events.WebApi.App_Start.Startup))]
namespace Coded.Events.WebApi.App_Start
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            HttpConfiguration httpConfig = new HttpConfiguration();

            // require authentication for all controllers
            httpConfig.Filters.Add(new AuthorizeAttribute());

            //Token Handlers
            ConfigureOAuthTokenGeneration(app);
            ConfigureOAuthTokenConsumption(app);

            GlobalConfiguration.Configure(WebApiConfig.Register);
            IdentityModelEventSource.ShowPII = true;
            app.UseCors(Microsoft.Owin.Cors.CorsOptions.AllowAll);
            app.UseWebApi(httpConfig);
            ApplicationDbContext.InitializeDataStore();
        }

        public static void ConfigureOAuthTokenGeneration(IAppBuilder app)
        {
            // Configure the db context and user manager to use a single instance per request
            app.CreatePerOwinContext(ApplicationDbContext.Create);
            app.CreatePerOwinContext<ApplicationUserManager>(ApplicationUserManager.Create);
            app.CreatePerOwinContext<ApplicationRoleManager>(ApplicationRoleManager.Create);
            SimpleJwtFormat _tokenFormat = new SimpleJwtFormat("http://localhost:59822");

            var OAuthServerOptions = new OAuthAuthorizationServerOptions()
            {
                AllowInsecureHttp = true,
                ApplicationCanDisplayErrors = true,
                TokenEndpointPath = new PathString("/oauth/token"),
                AccessTokenExpireTimeSpan = TimeSpan.FromMinutes(1),
                Provider = new SimpleAuthorizationServerProvider(new SimpleAuthorizationServerProviderOptions()
                {
                    ValidateUserCredentialsFunction = ValidateUserAsync
                }),
                RefreshTokenProvider = new SimpleRefreshTokenProvider(),
                AccessTokenFormat = _tokenFormat
            };

            app.UseOAuthAuthorizationServer(OAuthServerOptions);
            app.UseOAuthBearerAuthentication(new OAuthBearerAuthenticationOptions()
            {
                AccessTokenFormat = _tokenFormat
            });
        }

        private void ConfigureOAuthTokenConsumption(IAppBuilder app)
        {

            var issuer = "http://localhost:59822";
            string audienceId = ConfigurationManager.AppSettings["as:AudienceId"];
            byte[] audienceSecret = TextEncodings.Base64Url.Decode(ConfigurationManager.AppSettings["as:AudienceSecret"]);

            // Api controllers with an [Authorize] attribute will be validated with JWT
            var keyResolver = new OpenIdConnectSigningKeyResolver(issuer);

            string symmetricKeyAsBase64 = ConfigurationManager.AppSettings["as:AudienceSecret"];
            var keyByteArray = TextEncodings.Base64Url.Decode(symmetricKeyAsBase64);

            app.UseJwtBearerAuthentication(
            new JwtBearerAuthenticationOptions
            {
                AuthenticationMode = AuthenticationMode.Active,
                AllowedAudiences = new[] { audienceId },
                TokenValidationParameters = new TokenValidationParameters()
                {
                    ValidAudience = audienceId,
                    ValidAudiences = new[] { audienceId },
                    ValidIssuer = issuer,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    IssuerSigningKey = new SymmetricSecurityKey(keyByteArray),
                    IssuerSigningKeyResolver = (token, securityToken, identifier, parameters) => keyResolver.GetSigningKey(identifier)
                }

            });
        }

        private static async Task<ClaimsIdentity> ValidateUserAsync(string username, string password)
        {
            var userManager = new ApplicationUserManager(new UserStore<ApplicationUser>(new ApplicationDbContext()));

            ApplicationUser user = await userManager.FindAsync(username, password);

            if (user == null) {
                return null;
            }

            ClaimsIdentity oAuthIdentity = await user.GenerateUserIdentityAsync(userManager, "JWT");
            oAuthIdentity.AddClaims(ExtendedClaimsProvider.GetClaims(user));
            oAuthIdentity.AddClaims(RolesFromClaims.CreateRolesBasedOnClaims(oAuthIdentity));

            return oAuthIdentity;
        }

    }
}