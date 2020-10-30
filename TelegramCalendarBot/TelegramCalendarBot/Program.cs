using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;

using Telegram.Bot;
using Telegram.Bot.Args;

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace TelegramCalendarBot
{
    class Program
    {
        static CalendarService calendar;
        static ITelegramBotClient telegram;

        static string TELEGRAM_TOKEN;

        static async Task<int> Main(string[] args)
        {
            Console.WriteLine("Before starting download your Google APIs credentials and insert the credentials.json file in the program repositry (if done type yes, else the program won't start)");
            string response = Console.ReadLine();
            
            if (response.Equals("yes"))
            {
                Console.WriteLine("Insert the telegram token of your bot: ");
                TELEGRAM_TOKEN = Console.ReadLine();
                try
                {
                    await GoogleLog(); 
                    await TelegramLog();
                }
                catch
                {
                    Console.WriteLine("Non è stato possibile connettersi ai sevizi richiesti, controlla i dati inseriti e riprova");
                    return -1;
                }

                Console.ReadKey();
                return 0;
            }
            else return -1;
      
        }
        static async Task<Task> GoogleLog()
        {
            string[] Scopes = { CalendarService.Scope.CalendarReadonly };
            string ApplicationName = "BotterOne";
            UserCredential credentials;
            string credPath = "token.json";

            using (var stream = new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
            {
                credentials = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
                Console.WriteLine("Credential file saved to: " + credPath);
            }

            calendar = new CalendarService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credentials,
                ApplicationName = ApplicationName,
            });
            Console.WriteLine("Logged in Google as " + calendar.Name);

            return Task.CompletedTask;
        }

        static async Task<Task> TelegramLog()
        {
            telegram = new TelegramBotClient(TELEGRAM_TOKEN);
            Console.WriteLine("Turn on telegram bot: " + telegram.BotId);

            telegram.OnMessage += TelegramHandler;
            telegram.StartReceiving();

            return Task.CompletedTask;
        }
        static EventsResource.ListRequest GetEvents()
        {
            EventsResource.ListRequest request = calendar.Events.List("primary");
            request.TimeMin = DateTime.Now;
            request.ShowDeleted = false;
            request.SingleEvents = true;
            request.MaxResults = 5; //this is the number of events the bot will send
            request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

            return request;
        }

        static async Task PublishEventTelegram(EventsResource.ListRequest request, MessageEventArgs e)
        {
            Events events = request.Execute();
            foreach (Event _event in events.Items)
            {
                string eventResume = _event.Summary + " dalle alle ore: " + _event.Start.DateTime + ", alle ore: " + _event.End.DateTime + "\n!" + _event.HangoutLink;
                await telegram.SendTextMessageAsync(e.Message.Chat, eventResume);
            }
        }

        static async void TelegramHandler(object sender, MessageEventArgs e)
        {
            if (e.Message.Text.Contains("eventi") || e.Message.Text.Contains("Eventi") || e.Message.Text.Contains("events") || e.Message.Text.Contains("Events"))
            {
                await PublishEventTelegram(GetEvents(), e);
                Console.WriteLine("Sent the events to telegram in the chat: " + e.Message.Chat.Title);
            }
        }
    }
}
