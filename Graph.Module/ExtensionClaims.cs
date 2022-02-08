using Microsoft.Extensions.Configuration;

namespace Graph.Module
{
    public class ExtensionClaims
    {
        public static string GetAdminRoleKey(IConfiguration config)
        {
            return "extension_" + config["AzureAdB2CExtension:ClientId"] + "_IsAdmin";
        }
    }
}
