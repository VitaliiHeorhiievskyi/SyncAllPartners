using Azure.Core;
using Azure.Identity;
using Dapper;
using Microsoft.Data.SqlClient;

using StreamWriter writer = new StreamWriter("log.txt", false);

// Specify the connectionString to the 'AdSync' prod database.
// Note: ConnectionString must not contain 'Authentication=Active Directory Managed Identity'.
var connectionString = "";

// Specify the ADSyncPreSharedKeyValue.
var ADSyncPreSharedKeyValue = "";

// Get an accessToken to connect to the db.
var accessToken = await new DefaultAzureCredential().GetTokenAsync(new TokenRequestContext(new string[] { "https://database.windows.net//.default" }));

using var connection = new SqlConnection(connectionString)
{
    AccessToken = accessToken.Token
};

await connection.OpenAsync();

// Get all partner ids that are already synchronized.
var partnerIds = await connection.QueryAsync<Guid>("SELECT Id FROM Partners WHERE GroupsSyncEnabled = 1;");

var httpClient = new HttpClient();
httpClient.BaseAddress = new Uri("https://adsync.skykick.com/");
httpClient.DefaultRequestHeaders.Add("x-sk-authorization", ADSyncPreSharedKeyValue);

foreach (var partnerId in partnerIds)
{
    // Sync partner.
    var response = await httpClient.PutAsync($"api/v1/ADSync/partner/{partnerId}/sync", null);

    if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
    {
        await writer.WriteLineAsync($"Partner {partnerId} sync successfully completed.");
    }
    else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
    {
        await writer.WriteLineAsync($"Invalid ADSyncPreSharedKeyValue: {ADSyncPreSharedKeyValue}");
        return;
    }
    else
    {
        await writer.WriteLineAsync($"Partner {partnerId} sync failed.");
    }
}