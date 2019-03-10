namespace Auth0
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Text;
    using System.Linq;
    using DotNetOpenAuth.AspNet.Clients;
    using DotNetOpenAuth.Messaging;
    using Newtonsoft.Json;
    using System.Collections;
    using Microsoft.Web.WebPages.OAuth;
    using Newtonsoft.Json.Linq;

    public class OpenAuthClient : OAuth2Client
    {
        private const string AuthorizationEndpoint = @"https://{0}/authorize";
        private const string TokenEndpoint = @"https://{0}/oauth/token";
        private const string UserInfo = @"https://{0}/userinfo?access_token={1}";

        private readonly string appId;
        private readonly string appSecret;
        private readonly string domain;
        private readonly string connection;

        public OpenAuthClient(string appId, string appSecret, string domain, string connection = "")
            : this("Auth0", appId, appSecret, domain, connection)
        {
        }

        public OpenAuthClient(string name, string appId, string appSecret, string domain, string connection = "")
            : base(name)
        {
            this.appId = appId;
            this.appSecret = appSecret;
            this.domain = domain;
            this.connection = connection;
        }

         public static void RegisterAllProviders(string appId, string appSecret, string domain)
         {
             var client = new Client(appId, appSecret, domain);
             var connections = client.GetConnections().Where(c => c.Enabled);
             foreach (var c in connections)
             {
                 OAuthWebSecurity.RegisterClient(new OpenAuthClient(c.Name, appId, appSecret, domain, c.Name), c.Name, new Dictionary<string, object>());
             }
        }

         public static void RegisterAllSocialProviders(string appId, string appSecret, string domain)
         {
             var friendlyNames = new Dictionary<string, string>
                 {
                     {"google-oauth2", "Google"},
                     {"windowslive",   "Live ID"}
                 };

             var client = new Client(appId, appSecret, domain);
             var connections = client.GetSocialConnections().Where(c => c.Enabled);
             foreach (var c in connections)
             {
                 string friendlyName;
                 if (!friendlyNames.TryGetValue(c.Name, out friendlyName))
                 {
                     friendlyName = c.Name;
                 }
                 OAuthWebSecurity.RegisterClient(new OpenAuthClient(c.Name, appId, appSecret, domain, c.Name), friendlyName, new Dictionary<string, object>());
             }
         }

        protected override Uri GetServiceLoginUrl(Uri returnUrl)
        {
            var builder = new UriBuilder(string.Format(AuthorizationEndpoint, this.domain));
            builder.AppendQueryArgument("client_id", this.appId);
            builder.AppendQueryArgument("response_type", "code");
            builder.AppendQueryArgument("redirect_uri", returnUrl.AbsoluteUri);
            builder.AppendQueryArgument("connection", this.connection);

            return builder.Uri;
        }

        protected override IDictionary<string, string> GetUserData(string accessToken)
        {
            var request = WebRequest.Create(string.Format(UserInfo, this.domain, accessToken));

            using (var response = request.GetResponse())
            {
                using (var responseStream = response.GetResponseStream())
                {
                    using (var streamReader = new StreamReader(responseStream))
                    {
                        var values
                            = JsonConvert.DeserializeObject<Dictionary<string, object>>(streamReader.ReadToEnd());

                        // map user_id to id as DNOA needs it
                        values["id"] = values["user_id"];

                        var result = values.Where(kv => kv.Value is string)
                                            .ToDictionary(kv => kv.Key, kv => (string)kv.Value);
                        var identities = ((JArray)values["identities"]).OfType<JObject>().ToList();

                        for (var i = 0; i < identities.Count; i++)
                        {
                            foreach (var pv in identities[i])
                            {
                                result["identity_" + i + "_" + pv.Key] = pv.Value.ToString();
                            }
                        }
                        return result;
                    }
                }
            }
        }

        protected override string QueryAccessToken(Uri returnUrl, string authorizationCode)
        {
            var entity = new StringBuilder()
                                .Append(string.Format("client_id={0}&", this.appId))
                                .Append(string.Format("redirect_uri={0}&", returnUrl.AbsoluteUri))
                                .Append(string.Format("client_secret={0}&", this.appSecret))
                                .Append(string.Format("code={0}&", authorizationCode))
                                .Append("grant_type=authorization_code")
                                .ToString();

            WebRequest tokenRequest = WebRequest.Create(string.Format(TokenEndpoint, this.domain));
            tokenRequest.ContentType = "application/x-www-form-urlencoded";
            tokenRequest.ContentLength = entity.Length;
            tokenRequest.Method = "POST";

            using (Stream requestStream = tokenRequest.GetRequestStream())
            using (var writer = new StreamWriter(requestStream))
            {
                writer.Write(entity);
                writer.Flush();
            }

            HttpWebResponse tokenResponse = (HttpWebResponse)tokenRequest.GetResponse();

            if (tokenResponse.StatusCode == HttpStatusCode.OK)
            {
                using (Stream responseStream = tokenResponse.GetResponseStream())
                using (StreamReader response = new StreamReader(responseStream))
                {
                    var tokenData = JsonConvert.DeserializeObject<OAuth2AccessTokenData>(response.ReadToEnd());
                    if (tokenData != null)
                    {
                        return tokenData.AccessToken;
                    }
                }
            }

            return null;
        }
    }
}