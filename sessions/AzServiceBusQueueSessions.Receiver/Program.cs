using Azure.Messaging.ServiceBus;
using System;
using System.Threading;
using System.Threading.Tasks;
using static System.Console;

namespace AzServiceBusQueueSessions.Receiver
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await using var client = new ServiceBusClient(Shared.Configuration.CONNECTION_STRING);

            //await RunWithSessionReceiver(client);

            await RunWithSessionProcessor(client);
        }

        private static async Task RunWithSessionReceiver(ServiceBusClient client)
        {
            var cts = new CancellationTokenSource();
            CancelKeyPress += (a, o) =>
            {
                WriteLine("---I am Dead!---");
                cts.Cancel();
            };

            do
            {
                // Here we are accepting the next Session which isn't locked by any other receiver
                ServiceBusSessionReceiver receiver = await client.AcceptNextSessionAsync(Shared.Configuration.QUEUE_NAME);

                WriteLine($"Receiver started for SessionId: '{receiver.SessionId}'.");

                ServiceBusReceivedMessage message = null;
                do
                {
                    message = await receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(1), cancellationToken: cts.Token);
                    if (message != null)
                    {
                        try
                        {
                            WriteLine($"Received: '{message.Body}', Ack: Complete");
                            await receiver.CompleteMessageAsync(message, cts.Token);
                        }
                        catch
                        {
                            WriteLine($"Received: '{message.Body}', Ack: Abondon");
                            await receiver.AbandonMessageAsync(message, cancellationToken: cts.Token);
                        }
                    }
                }
                while (message != null && !cts.IsCancellationRequested);
                await receiver.CloseAsync();
            }
            while (!cts.IsCancellationRequested);
        }

        private static async Task RunWithSessionProcessor(ServiceBusClient client)
        {
            var options = new ServiceBusSessionProcessorOptions()
            {
                MaxConcurrentSessions = 1
            };

            ServiceBusSessionProcessor processor = client.CreateSessionProcessor(Shared.Configuration.QUEUE_NAME, options);

            processor.SessionInitializingAsync += async args =>
            {
                WriteLine($"Initializing for Session: '{args.SessionId}' at '{DateTimeOffset.UtcNow}', SessionLockedUntil: '{args.SessionLockedUntil}'");
            };

            processor.SessionClosingAsync += async args =>
            {
                WriteLine($"Closing Session: '{args.SessionId}' at '{DateTimeOffset.UtcNow}'\n");
            };

            processor.ProcessMessageAsync += async args =>
            {
                WriteLine($"Received for Session: '{args.SessionId}', Message: '{args.Message.Body}', Ack: Complete");
            };

            processor.ProcessErrorAsync += async args =>
            {
                WriteLine($"Exception for Session: '{args.Exception.Message}'");
            };

            WriteLine("Starting...Press any character to gracefully exit.");

            await processor.StartProcessingAsync();

            ReadKey();

            WriteLine("Stopping...");

            await processor.StopProcessingAsync();

            WriteLine("Stopped!");
        }
    }
}