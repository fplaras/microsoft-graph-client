using Microsoft.Extensions.Configuration;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
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

        private async Task<User> CreateB2CUser(User newUser)
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

        public async Task<User> CreateUser(NewUserModel model, string createUser)
        {

            try
            {
                var user = new User
                {

                    AccountEnabled = true,
                    DisplayName = model.DisplayName,
                    GivenName = model.GivenName,
                    Surname = model.Surname,
                    Mail = model.Email,
                    Identities = new List<ObjectIdentity>()
                    {
                        new ObjectIdentity()
                        {
                            Issuer = _config["AzureAdB2c:Domain"],
                            SignInType = "emailAddress",
                            IssuerAssignedId = model.Email
                        }

                    },
                    PasswordProfile = new PasswordProfile
                    {
                        //asks user for their current password if set true
                        ForceChangePasswordNextSignIn = false,
                        Password = GetRandomPassword(16),
                    },
                    PasswordPolicies = "DisablePasswordExpiration",
                    AdditionalData = new Dictionary<string, object>
{
                        { ExtensionClaims.GetAdminRoleKey(_config), model.IsAdminRole ? true : null },
                    }
                };

                var createdUser = await CreateB2CUser(user);

                //Track new user in database
                //Email User using SendGrid

                return createdUser;
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        private static string GetRandomPassword(int length)
        {
            const string chars = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ.@!&*$";

            StringBuilder sb = new StringBuilder();
            Random rnd = new Random();

            for (int i = 0; i < length; i++)
            {
                int index = rnd.Next(chars.Length);
                sb.Append(chars[index]);
            }

            return sb.ToString();
        }

    }
}
