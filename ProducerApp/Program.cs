using Azure.Messaging.ServiceBus;
using ProducerApp.Model;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

var connectionString =
    Environment.GetEnvironmentVariable("AZURE_SERVICE_BUS_CONNECTION_STRING")
    ?? builder.Configuration["AzureServiceBus:ConnectionString"];

var queueName =
    Environment.GetEnvironmentVariable("AZURE_SERVICE_BUS_QUEUE_NAME")
    ?? builder.Configuration["AzureServiceBus:QueueName"]
    ?? "sender-receiver-q";

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Azure Service Bus connection string is not configured.");
}

app.MapPost("/api/messages", async (UserMessage userMessage) =>
{
    if (string.IsNullOrWhiteSpace(userMessage.Name) ||
        string.IsNullOrWhiteSpace(userMessage.Email) ||
        string.IsNullOrWhiteSpace(userMessage.Message))
    {
        return Results.BadRequest(new
        {
            message = "Name, email and message are required."
        });
    }

    await using var client = new ServiceBusClient(connectionString);

    ServiceBusSender sender = client.CreateSender(queueName);

    var message = new ServiceBusMessage(
        BinaryData.FromObjectAsJson(userMessage));

    message.ContentType = "application/json";

    await sender.SendMessageAsync(message);

    return Results.Ok(new
    {
        success = true,
        message = "Message sent successfully."
    });
});

app.Run();