using AzServiceBusSimpleRequestReply.Shared;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using System.Text.Json;
using static System.Console;

ServiceBusAdministrationClient serviceBusAdministrationClient = new(Configuration.CONNECTION_STRING);
string replyQueueName = Guid.NewGuid().ToString();

try
{
    // Temporary Queue for Receiver to send their replies into
    await serviceBusAdministrationClient.CreateQueueAsync(new CreateQueueOptions(replyQueueName)
    {
        AutoDeleteOnIdle = TimeSpan.FromSeconds(300)
    });

    // Sending the message
    await using ServiceBusClient serviceBusClient = new(Configuration.CONNECTION_STRING);
    ServiceBusSender serviceBusSender = serviceBusClient.CreateSender(Configuration.QUEUE_NAME);

    ApplicationMessage applicationMessage = new("John");
    ServiceBusMessage serviceBusMessage = new(JsonSerializer.SerializeToUtf8Bytes(applicationMessage))
    {
        ContentType = "application/json",
        ReplyTo = replyQueueName,
    };
    await serviceBusSender.SendMessageAsync(serviceBusMessage);
    WriteLine($"Message Sent: {applicationMessage}.\n");

    // Creating a receiver and waiting for the Receiver to reply
    ServiceBusReceiver serviceBusReceiver = serviceBusClient.CreateReceiver(replyQueueName);
    ServiceBusReceivedMessage serviceBusReceivedMessage = await serviceBusReceiver.ReceiveMessageAsync(TimeSpan.FromSeconds(60));

    if (serviceBusReceivedMessage == null)
    {
        WriteLine("Error: Didn't receive a response.");
        return;
    }

    applicationMessage = JsonSerializer.Deserialize<ApplicationMessage>(serviceBusReceivedMessage.Body.ToString());
    WriteLine($"Reply Received: {applicationMessage}.\n");
}
finally
{
    await serviceBusAdministrationClient.DeleteQueueAsync(replyQueueName);
}

ReadLine();