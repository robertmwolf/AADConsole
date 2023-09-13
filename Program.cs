using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Console_Interactive_MultiTarget
{
    internal class Program
    {
        private static PublicClientApplicationOptions appConfiguration = null;
        private static IConfiguration configuration;
        private static string MSGraphURL;

        // The MSAL Public client app
        private static IPublicClientApplication application;

        private static async Task Main(string[] args)
        {
            // Using appsettings.json for our configuration settings
            var builder = new ConfigurationBuilder()
                .SetBasePath(System.IO.Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");

            configuration = builder.Build();

            appConfiguration = configuration
                .Get<PublicClientApplicationOptions>();

            string[] scopes = new[] { "https://graph.microsoft.com/.default" };

            GraphServiceClient graphClient = await InitializeGraphServiceClient(appConfiguration, scopes);


            //call different endpoint
            UserCollectionResponse users = (UserCollectionResponse)await CallUsersAsync(graphClient);
            Console.WriteLine(users.Value);

            string userid = "281b2d29-d2b2-4431-be30-d51d4809ae42"; //object id of rowol account, is in the group and is authorized
            string unauthorizeduser = "91d3b422-2a90-433b-9fdd-f5009c1a9ae1";//object id of some other user  account, is NOT in the group and is unauthorized

            string ACEGroupID = "7bdc4358-5206-419c-be77-88764674c567"; //authorized group for ACE, retrieve from config

            string AceUserGroup = "7bdc4358-5206-419c-be77-88764674c567"; //This would be a group for ACE users
            string AceApproverGroup = "7bdc4358-5206-419c-be77-88764674c567"; //This is ACE approvers group, so it is higher level of access


            bool AuthorizedACEUser = await IsMember(graphClient, userid, ACEGroupID);
            //bool IsAuthorizedForVuln = await IsMember(graphClient, userid, VulnarabiltyGroupID);
            Console.WriteLine("Is this user authorized to use ACE user search? They should be {0}", AuthorizedACEUser);

            bool IsNOTAuthorizedForACE = await IsMember(graphClient, unauthorizeduser, ACEGroupID);
            Console.WriteLine("Is this user authorized to use ACE user search? They should not be {0}", IsNOTAuthorizedForACE);

            // TODO:
            // retrieve the list of searches we have onboarded (all the customers in DSR so far) from the config. ACE, SAS, etc.
            // This list has groups that we know have access to them
            // instantiate a list of searches we will perform for the user (empty for now)
            // for each cognitive search index we have onboarded
                // check if user is in the group (or groups) that has access to index. If so, add that index to the list of searches we will perform for the user
            // return the list of searches we will perform for the user to the orchestrator to perform and concatinate together

        }

        private static async Task<bool> IsMember(GraphServiceClient graphClient2, string userid, string groupid)
        {
            var requestBody = new Microsoft.Graph.DirectoryObjects.Item.CheckMemberGroups.CheckMemberGroupsPostRequestBody
            {
                GroupIds = new List<string>
                    {
                        groupid,
                    },
            };
            var result = await graphClient2.DirectoryObjects[userid].CheckMemberGroups.PostAsync(requestBody);
            return result.Value.Contains(groupid);
        }

        private static async Task<GraphServiceClient> InitializeGraphServiceClient(PublicClientApplicationOptions appConfiguration, string[] scopes)
        {
            var clientId = appConfiguration.ClientId;
            var tenantId = appConfiguration.TenantId;
            var clientSecret = "[Insert Secret Value For Test]";

            // using Azure.Identity;
            var options = new ClientSecretCredentialOptions
            {
                AuthorityHost = AzureAuthorityHosts.AzurePublicCloud,
            };

            // https://learn.microsoft.com/dotnet/api/azure.identity.clientsecretcredential
            var clientSecretCredential = new ClientSecretCredential(
                tenantId, clientId, clientSecret, options);

            var graphClient = new GraphServiceClient(clientSecretCredential, scopes);
            return graphClient;
        }

        private static async Task<object> CallUsersAsync(GraphServiceClient graphClient)
        {
            var res = await graphClient.Users.GetAsync();
            return res;
        }
    }
}
