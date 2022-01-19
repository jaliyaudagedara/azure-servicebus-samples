using AzServiceBusSimpleRequestReply.Shared;
using Azure.Messaging.ServiceBus;
using System.Text.Json;
using static System.Console;

await using ServiceBusClient serviceBusClient = new(Configuration.CONNECTION_STRING);
ServiceBusProcessor serviceBusProcessor = serviceBusClient.CreateProcessor(Configuration.QUEUE_NAME);

serviceBusProcessor.ProcessMessageAsync += async args =>
{
    // Message received
    ApplicationMessage applicationMessage = JsonSerializer.Deserialize<ApplicationMessage>(args.Message.Body.ToString());

    WriteLine($"Message Received: {applicationMessage}.\n");

    // Process the message/Update the Output
    applicationMessage.Output = $"Hello {applicationMessage.Input}!.";

    // Sending the reply
    ServiceBusSender serviceBusSender = serviceBusClient.CreateSender(args.Message.ReplyTo);
    ServiceBusMessage serviceBusMessage = new(JsonSerializer.Serialize(applicationMessage));
    await serviceBusSender.SendMessageAsync(serviceBusMessage);
};

serviceBusProcessor.ProcessErrorAsync += async args =>
{
    WriteLine($"Exception for Session: '{args.Exception.Message}'.");
};

WriteLine("Receiver Starting...Press any character to gracefully exit.");

await serviceBusProcessor.StartProcessingAsync();

ReadKey();

WriteLine("Stopping...");

await serviceBusProcessor.StopProcessingAsync();

WriteLine("Stopped!");