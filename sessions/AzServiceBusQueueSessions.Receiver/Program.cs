using Azure.Messaging.ServiceBus;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AzServiceBusQueueSessions.Receiver
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await using var client = new ServiceBusClient(Shared.Configuration.CONNECTION_STRING);

            var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (a, o) => { cts.Cancel(); };

            do
            {
                // Here we are accepting the next Session which isn't locked by any other receiver
                ServiceBusSessionReceiver receiver = await client.AcceptNextSessionAsync(Shared.Configuration.QUEUE_NAME);

                Console.WriteLine($"Receiver started for SessionId: '{receiver.SessionId}'.");

                ServiceBusReceivedMessage message = null;
                do
                {
                    message = await receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(1), cancellationToken: cts.Token);
                    if (message != null)
                    {
                        try
                        {
                            Console.WriteLine($"Received: '{message.Body}', Ack: Complete");
                            await receiver.CompleteMessageAsync(message, cts.Token);
                        }
                        catch
                        {
                            Console.WriteLine($"Received: '{message.Body}', Ack: Abondon");
                            await receiver.AbandonMessageAsync(message, cancellationToken: cts.Token);
                        }
                    }
                }
                while (message != null && !cts.IsCancellationRequested);
                await receiver.CloseAsync();
            }
            while (!cts.IsCancellationRequested);
        }
    }
}