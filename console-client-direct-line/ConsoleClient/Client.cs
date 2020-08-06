using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Connector.DirectLine;

namespace CSharpNetCore_DirectLine_Client
{
    class Client
    {
        static void Main(string[] args)
        {
            Task.WaitAll(Run());
        }

        public static async Task Run()
        {

            var endpoint = "https://<your bot name>.azurewebsites.net/.bot/";
            var secret = "<Your bot  Direct Line secret>";

            var userId = Guid.NewGuid().ToString();
            var userName = "Fred";

            Console.WriteLine("Connecting...");

            var tokenClient = new DirectLineClient(
            new Uri(endpoint),
            new DirectLineClientCredentials(secret));
            var tokenConversation = await tokenClient.Tokens.GenerateTokenForNewConversationAsync().ConfigureAwait(false);

            var client = new DirectLineClient(
                new Uri(endpoint),
                new DirectLineClientCredentials(tokenConversation.Token));

            await client.StreamingConversations.ConnectAsync(
                tokenConversation.ConversationId,
                ReceiveActivities).ConfigureAwait(false);

            var conversation = await client.StreamingConversations.StartConversationAsync().ConfigureAwait(false);

            Console.WriteLine($"Connected to conversation {conversation.ConversationId}");

            Console.Write("> ");
            var message = Console.ReadLine();

            while (message != "end")
            {
                try
                {
                    var response = await client.StreamingConversations.PostActivityAsync(conversation.ConversationId,
                        new Activity()
                        {
                            Type = "message",
                            Text = message,
                            From = new ChannelAccount()
                            {
                                Id = userId,
                                Name = userName
                            }
                        }).ConfigureAwait(false);
                }
                catch (OperationException ex)
                {
                    Console.WriteLine($"OperationException when calling PostActivityAsync: ({ex.StatusCode})");
                }
                Console.Write("> ");
                message = Console.ReadLine();
            }

            client.StreamingConversations.Disconnect();
        }

        public static void ReceiveActivities(ActivitySet activitySet)
        {
            if (activitySet != null)
            {
                foreach (var a in activitySet.Activities)
                {
                    if (a.Type == ActivityTypes.Message && a.From.Id.ToLowerInvariant().Contains("bot"))
                    {
                        Console.WriteLine($"<Bot>: {a.Text}");
                        if (a.Attachments.Any())
                        {
                            foreach (var attachment in a.Attachments)
                            {
                                var stream = attachment.Content as Stream;
                                Task.Run(async () => {
                                    int count;
                                    int total = 0;
                                    byte[] buffer = new byte[4096];
                                    do
                                    {
                                        count = await stream.ReadAsync(buffer, 0, 4096);
                                        total += count;
                                    } while (count > 0);
                                    Console.WriteLine($"  Read stream of length: {total}");
                                });
                            }
                        }
                    }
                }
            }
        }
    }
}
