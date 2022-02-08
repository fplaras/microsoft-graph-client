using Microsoft.Extensions.Configuration;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Graph.Module
{
    /***
     * Permissions on the tenant have to be configured for the graph client.
     * */
    public class GraphService
    {
        private readonly IConfiguration _config;
        private readonly IConfidentialClientApplication _confidentialClientApplication;
        private readonly GraphServiceClient _graphClient;
        //private readonly ClientCredentialProvider _authProvider;
        private readonly string[] scopes = { "https://graph.microsoft.com/.default" };
        public GraphService(IConfiguration config)
        {
            _config = config;
            _confidentialClientApplication = ConfidentialClientApplicationBuilder
                .Create(_config["AzureAdB2c:ClientId"])
                .WithTenantId(_config["AzureAdB2c:TenantId"])
                .WithClientSecret(_config["AzureAdB2c:ClientSecret"])
                .Build();
            //_authProvider = _authProvider ?? new ClientCredentialProvider(_confidentialClientApplication);

            _graphClient = new GraphServiceClient(new DelegateAuthenticationProvider(async (requestMessage) =>
            {
                var authResult = await _confidentialClientApplication
                                        .AcquireTokenForClient(scopes)
                                        .ExecuteAsync();

                requestMessage.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", authResult.AccessToken);
            }));
        }

        public async Task<User> CreateB2CUser(User newUser)
        {
            try
            {
                var newB2cUser = new User
                {
                    AccountEnabled = newUser.AccountEnabled,
                    DisplayName = newUser.DisplayName,
                    GivenName = newUser.GivenName,
                    Surname = newUser.Surname,
                    Identities = newUser.Identities,
                    PasswordProfile = newUser.PasswordProfile,
                    PasswordPolicies = newUser.PasswordPolicies,
                    AdditionalData = newUser.AdditionalData,
                    OtherMails = newUser.OtherMails,
                    Mail = newUser.Mail
                };

                User userCreated = await _graphClient.Users
                        .Request()
                        .AddAsync(newB2cUser);

                return userCreated;
            }
            catch (Exception ex)
            {

                throw ex;
            }

        }

        private async Task<User> GetUser(string objectId, string attr = null)
        {
            User createdUser = await _graphClient.Users[objectId].Request().Select(attr ?? string.Empty).GetAsync();
            return createdUser;
        }

        private async Task<List<User>> GetUserList(string attr = null)
        {
            var users = await _graphClient.Users.Request().Select(attr ?? string.Empty).GetAsync();
            return users.ToList();
        }

        public async Task DeleteUser(string objectId)
        {
            await _graphClient.Users[objectId].Request().DeleteAsync();
        }

        public async Task UpdateUser(string objectId, bool? isAdmin)
        {
            var user = new User
            {
                AdditionalData = new Dictionary<string, object>
                {
                    { ExtensionClaims.GetAdminRoleKey(_config), isAdmin }
                }
            };
            await _graphClient.Users[objectId].Request().UpdateAsync(user);
        }

        /**
         * Example query for retrieving users
         */

        //Retrieve user with custom attribute
        public async Task<User> RetrieveUserDetails(string id)
        {
            var user = await GetUser(id,
                string.Format("{0},{1},{2},{3},{4},{5},{6}", ExtensionClaims.GetAdminRoleKey(_config),
                "displayName",
                "surname",
                "givenName",
                "id",
                "mail", 
                "userPrincipalName"));

            return user;
        }


    }
}
