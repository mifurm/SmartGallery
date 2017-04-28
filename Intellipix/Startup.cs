using System;
using System.Configuration;
using System.IdentityModel.Tokens;
using System.Threading.Tasks;
using Microsoft.Azure;
using Microsoft.IdentityModel.Protocols;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.Notifications;
using Microsoft.Owin.Security.OpenIdConnect;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Owin;
using SmartGallery.Data.Repositories;

[assembly: OwinStartup(typeof(SmartGallery.Web.Startup))]

namespace SmartGallery.Web
{
    public class Startup
    {
        // App config settings
        private static string clientId = ConfigurationManager.AppSettings["b2c:ClientId"];
        private static string aadInstance = ConfigurationManager.AppSettings["b2c:AadInstance"];
        private static string tenant = ConfigurationManager.AppSettings["b2c:Tenant"];
        private static string redirectUri = ConfigurationManager.AppSettings["b2c:RedirectUri"];

        // B2C policy identifiers
        public static string SignUpSignInPolicyId = ConfigurationManager.AppSettings["b2c:SignUpSignInPolicyId"];

        public void Configuration(IAppBuilder app)
        {
            if (clientId != null && aadInstance != null && tenant != null && redirectUri != null && redirectUri != null)
            {
                ConfigureAuth(app);
                System.Web.Helpers.AntiForgeryConfig.UniqueClaimTypeIdentifier = System.Security.Claims.ClaimTypes.NameIdentifier;
            }

            var commentRepo = new CommentRepository(
                        CloudConfigurationManager.GetSetting("documentdb:host"),
                        CloudConfigurationManager.GetSetting("documentdb:key"),
                        CloudConfigurationManager.GetSetting("documentdb:dbname"),
                        CloudConfigurationManager.GetSetting("documentdb:commentcollection")
                        );
            commentRepo.InitializeAsync().Wait();
            string photoContainer = CloudConfigurationManager.GetSetting("storage:photocontainer");
            string thumbContainer = CloudConfigurationManager.GetSetting("storage:thumbnailcontainer");
            var blobClient = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("storage:connectionstring")).CreateCloudBlobClient();
            blobClient.GetContainerReference(photoContainer).CreateIfNotExists(BlobContainerPublicAccessType.Blob);
            blobClient.GetContainerReference(thumbContainer).CreateIfNotExists(BlobContainerPublicAccessType.Blob);
        }

        public void ConfigureAuth(IAppBuilder app)
        {
            app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);

            app.UseCookieAuthentication(new CookieAuthenticationOptions());

            // Configure OpenID Connect middleware for each policy
            app.UseOpenIdConnectAuthentication(CreateOptionsFromPolicy(SignUpSignInPolicyId));
        }

        // Used for avoiding yellow-screen-of-death
        private Task AuthenticationFailed(AuthenticationFailedNotification<OpenIdConnectMessage, OpenIdConnectAuthenticationOptions> notification)
        {
            notification.HandleResponse();
            if (notification.Exception.Message == "access_denied")
            {
                notification.Response.Redirect("/");
            }
            else
            {
                notification.Response.Redirect("/Home/Error?message=" + notification.Exception.Message);
            }

            return Task.FromResult(0);
        }

        private OpenIdConnectAuthenticationOptions CreateOptionsFromPolicy(string policy)
        {
            return new OpenIdConnectAuthenticationOptions
            {
                // For each policy, give OWIN the policy-specific metadata address, and
                // set the authentication type to the id of the policy
                MetadataAddress = String.Format(aadInstance, tenant, policy),
                AuthenticationType = policy,

                // These are standard OpenID Connect parameters, with values pulled from web.config
                ClientId = clientId,
                RedirectUri = redirectUri,
                PostLogoutRedirectUri = redirectUri,
                Notifications = new OpenIdConnectAuthenticationNotifications
                {
                    AuthenticationFailed = AuthenticationFailed
                },
                Scope = "openid",
                ResponseType = "id_token",

                // This piece is optional - it is used for displaying the user's name in the navigation bar.
                TokenValidationParameters = new TokenValidationParameters
                {
                    NameClaimType = "name",
                    SaveSigninToken = true //important to save the token in boostrapcontext
                }
            };
        }
    }
}
