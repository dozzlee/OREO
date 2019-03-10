namespace Coded.Events.WebApi.Providers
{
    public class SimpleAuthorizationServerProviderOptions
    {
        public SimpleAuthorizationServerProvider.UserCredentialValidationFunction ValidateUserCredentialsFunction { get; internal set; }
        public SimpleAuthorizationServerProvider.ClientCredentialsValidationFunction ValidateClientCredentialsFunction { get; internal set; }
    }
}