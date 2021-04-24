using Azure.Messaging.ServiceBus;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using static System.Console;

namespace AzServiceBusQueueSessions.Sender
{
    public record ApplicationMessage(string Message);

    class Program
    {
        static async Task Main(string[] args)
        {
            await using var client = new ServiceBusClient(Shared.Configuration.CONNECTION_STRING);

            ServiceBusSender sender = client.CreateSender(Shared.Configuration.QUEUE_NAME);

            List<string> sessionIds = new() { "S1", "S2" };

            for (var i = 1; i <= 5; i++)
            {
                foreach (var sessionId in sessionIds)
                {
                    var applicationMessage = new ApplicationMessage($"M{i}");
                    await SendMessageAsync(sessionId, applicationMessage, sender);
                }
            }
        }

        static async Task SendMessageAsync(string sessionId, ApplicationMessage applicationMessage, ServiceBusSender serviceBusSender)
        {
            var message = new ServiceBusMessage(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(applicationMessage)))
            {
                // Note: Since I am sending to a Session enabled Queue, SessionId should not be empty
                SessionId = sessionId,
                ContentType = "application/json"
            };

            await serviceBusSender.SendMessageAsync(message);

            WriteLine($"Message sent: Session '{message.SessionId}', Message = '{applicationMessage.Message}'");
        }
    }
}
