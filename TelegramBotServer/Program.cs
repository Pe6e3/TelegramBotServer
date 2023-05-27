using Newtonsoft.Json.Linq;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using System.Threading;
using Microsoft.Data.SqlClient;
using System.Text;

namespace DevSpaceConsole
{
    public class Program
    {
        private static string _token { get; set; } = "5673535292:AAH6GJUPM-F2QFASxaYtyG3i6kmx8Tjgl4s";
        private static TelegramBotClient _client;
        private static Timer _timer;

        public static void Main()
        {

            _client = new TelegramBotClient(_token);

            using CancellationTokenSource cts = new();

            // StartReceiving does not block the caller thread. Receiving is done on the ThreadPool.
            ReceiverOptions receiverOptions = new()
            {
                AllowedUpdates = Array.Empty<UpdateType>() // receive all update types except ChatMember related updates
            };
            _client.StartReceiving(updateHandler:     HandleUpdateAsync,
                                  pollingErrorHandler:HandlePollingErrorAsync,
                                  receiverOptions:    receiverOptions,
                                  cancellationToken:  cts.Token);

            Console.WriteLine("Start");

            _timer = new Timer(GetMessagesFromDb, null, 3000, 3000);

            Console.ReadLine();
            cts.Cancel();// Send cancellation request to stop bot
        }

        private static void GetMessagesFromDb(object state)
        {
            // Discard the result
            SendMessage();
        }
        private async static Task SendMessage()
        {
            //your connection string 
            string connString = "Data Source=(local);Initial Catalog=DevSpaceBot;Integrated Security=True;Trust Server Certificate=true";
            //create instanace of database connection
            SqlConnection conn = new SqlConnection(connString);
            try
            {
                //open connection
                conn.Open();
                //create a new SQL Query using StringBuilder
                StringBuilder strBuilder = new StringBuilder();

                strBuilder.Append("SELECT * FROM Messages WHERE IsSended = 0");
                string sqlQuery = strBuilder.ToString();

                SqlCommand cmd = new SqlCommand(sqlQuery, conn);
                SqlDataReader rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    long id = Convert.ToInt32(rdr["Id"]);
                    long chatId = Convert.ToInt64(rdr["ChatId"]);
                    string message = Convert.ToString(rdr["MessageText"]);
                    Console.WriteLine(chatId);
                    Console.WriteLine(message);
                    await SendMessageToTelegram(chatId, message);
                    await UpdateMessage(id);
                }

                strBuilder.Clear(); // clear all the string
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
            }
        }
        private async static Task UpdateMessage(long id)
        {
            //your connection string 
            string connString = "Data Source=(local);Initial Catalog=DevSpaceBot;Integrated Security=True;Trust Server Certificate=true";
            //create instanace of database connection
            SqlConnection conn = new SqlConnection(connString);
            try
            {
                //open connection
                conn.Open();
                //create a new SQL Query using StringBuilder

                StringBuilder strBuilder2 = new StringBuilder();
                strBuilder2.Append("update Messages set IsSended = 1 where Id = ");
                strBuilder2.Append(id);
                string sqlQuery2 = strBuilder2.ToString();

                SqlCommand cmd2 = new SqlCommand(sqlQuery2, conn);
                cmd2.ExecuteNonQuery();
                strBuilder2.Clear(); // clear all the string

            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
            }
        }
        public static async Task SendMessageToTelegram(long chatId, string messageText)
        {
            // Echo received message text
            await _client.SendTextMessageAsync(
                chatId: chatId,
                text: messageText);
        }
        private static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            // Only process Message updates: https://core.telegram.org/bots/api#message
            if (update.Message is not { } message)
                return;
            // Only process text messages
            if (message.Text is not { } messageText)
                return;

            var chatId = message.Chat.Id;

            Console.WriteLine($"Received a '{messageText}' message in chat {chatId}.");
            SendMessageToTelegram(chatId, messageText);
        }

        private static Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            Console.WriteLine(ErrorMessage);
            return Task.CompletedTask;
        }
    }
}




//private static void GetMessagesFromDb(object state)
//{
//    // Your connection string
//    string connString = "Data Source=(local);Initial Catalog=DevSpaceBot;Integrated Security=True;Trust Server Certificate=true";

//    // Create instance of database connection
//    using (SqlConnection conn = new SqlConnection(connString))
//    {
//        try
//        {
//            // Open connection
//            conn.Open();

//            // Create a new SQL Query
//            string sqlQuery = "SELECT * FROM Messages WHERE IsSended = 0";

//            using (SqlCommand command = new SqlCommand(sqlQuery, conn))
//            {
//                // Execute the query and get the result
//                using (SqlDataReader reader = command.ExecuteReader())
//                {
//                    // Read and display each row of the result
//                    while (reader.Read())
//                    {
//                        int id = (int)reader["Id"];
//                        string messageText = (string)reader["MessageText"];
//                        DateTime messageDate = (DateTime)reader["MessageDate"];
//                        int chatId = (int)reader["ChatId"];
//                        int userId = (int)reader["UserId"];
//                        bool isOur = (bool)reader["IsOur"];

//                        Console.WriteLine($"Id: {id}");
//                        Console.WriteLine($"MessageText: {messageText}");
//                        Console.WriteLine($"MessageDate: {messageDate}");
//                        Console.WriteLine($"ChatId: {chatId}");
//                        Console.WriteLine($"UserId: {userId}");
//                        Console.WriteLine($"IsOur: {isOur}");
//                        Console.WriteLine("-----------------------------------");
//                    }
//                }
//            }
//        }
//        catch (Exception e)
//        {
//            Console.WriteLine("Error: " + e.Message);
//        }
//    }
//}