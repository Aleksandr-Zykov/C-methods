using System;
using Telegram.Bot;
using Telegram.Bot.Types;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        string botToken = "7111688662:AAHpe_vgl0JTyHOtYlWZafxJb2sjQIFrYFs";
        string channelUsername = "@test123131241";
        string message = "Hello, this is a test message!";

        try
        {
            await PublishPostAsync(botToken, channelUsername, message);
            Console.WriteLine("Message published successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error publishing message: {ex.Message}");
        }

        Console.ReadLine();
    }

    /* Parameters:
     * botToken: Your Telegram bot token.
     * channelUsername: Username of the Telegram channel where you have admin access (or your bot has).
     * message: The text content of the post to publish.
     * 
     * Functionality:
     * Creates a Telegram bot client using your bot token.
     * Sends the provided message to the specified channel.
     */
    public static async Task PublishPostAsync(string botToken, string channelUsername, string message)
    {
        var botClient = new TelegramBotClient(botToken);
        await botClient.SendTextMessageAsync(channelUsername, message);
    }
}