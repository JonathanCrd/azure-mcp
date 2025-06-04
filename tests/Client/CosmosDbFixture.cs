using System;
using System.Threading.Tasks;
using Azure.Identity;
using AzureMcp.Tests.Client.Helpers;
using Microsoft.Azure.Cosmos;
using Xunit;

namespace AzureMcp.Tests.Client;

public class CosmosDbFixture : IAsyncLifetime
{
    public async ValueTask InitializeAsync()
    {
        // Usar LiveTestSettingsFixture para obtener ResourceBaseName
        var settingsFixture = new LiveTestSettingsFixture();
        await settingsFixture.InitializeAsync();

        CosmosClient client = new(
            accountEndpoint: $"https://{settingsFixture.Settings.ResourceBaseName}.documents.azure.com:443/",
            tokenCredential: new DefaultAzureCredential()
        );
        Container container = client.GetContainer("ToDoList", "Items");
        var item = new { id = Guid.NewGuid().ToString(), title = "Test Task", completed = false };
        await container.UpsertItemAsync(item, new PartitionKey(item.id));
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
