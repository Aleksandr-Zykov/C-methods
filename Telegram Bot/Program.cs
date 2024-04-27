using System;
using System.Net.Http;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

class Program
{
    static async Task Main(string[] args)
    {
        // Replace with your bot's token
        string botToken = "7111688662:AAHpe_vgl0JTyHOtYlWZafxJb2sjQIFrYFs";

        var poster = new TelegramBot(botToken);

        // Example usage:
        string channelUsername = "test123131241";
        string messageText = "This is a test message from the bot!";

        // Example with no media:
        await poster.SendChannelMessage(channelUsername, messageText, null);

        // Example with a single image:
        var singleImagePath = new List<string> { @"https://cdn.pixabay.com/photo/2018/09/18/16/45/forest-3686632_640.jpg" };
        await poster.SendChannelMessage(channelUsername, messageText, singleImagePath);

        // Example with multiple images:
        var multipleImagePaths = new List<string> { @"https://cdn.pixabay.com/photo/2018/09/18/16/45/forest-3686632_640.jpg", @"https://cdn.pixabay.com/photo/2023/12/07/19/45/tiger-8436227_960_720.jpg" };
        await poster.SendChannelMessage(channelUsername, messageText, multipleImagePaths);

        Console.WriteLine("Messages sent!");
        Console.ReadLine(); // Keep console open
    }
}

public class TelegramBot
{
    private readonly string _botToken;

    public TelegramBot(string botToken)
    {
        _botToken = botToken;
    }

    public async Task SendChannelMessage(string channelUsername, string messageText, List<string> mediaFilePathsOrUrls)
    {
        var botClient = new TelegramBotClient(_botToken);

        // Normalize channelUsername to ensure it starts with "@"
        channelUsername = NormalizeChannelUsername(channelUsername);

        ChatId chatId = new ChatId(channelUsername);

        try
        {
            if (mediaFilePathsOrUrls == null || mediaFilePathsOrUrls.Count == 0)
            {
                // Send text message only
                await botClient.SendTextMessageAsync(chatId, messageText);
            }
            else if (mediaFilePathsOrUrls.Count == 1)
            {
                string firstItem = mediaFilePathsOrUrls[0];
                if (IsUrl(firstItem))
                {
                    // Download image from URL and send
                    using var httpClient = new HttpClient();
                    using var response = await httpClient.GetAsync(firstItem);
                    using var stream = await response.Content.ReadAsStreamAsync();
                    InputFile inputFile = InputFile.FromStream(stream);
                    await botClient.SendPhotoAsync(chatId, inputFile, caption: messageText);
                }
                else
                {
                    // Send single image (local file)
                    using var fileStream = System.IO.File.OpenRead(firstItem);
                    InputFile inputFile = InputFile.FromStream(fileStream);
                    await botClient.SendPhotoAsync(chatId, inputFile, caption: messageText);
                }
            }
            else
            {
                // Send multiple images (up to 10 in an album)
                var multipartContent = new MultipartFormDataContent();
                var mediaGroup = new List<InputMediaPhoto>();

                for (int i = 0; i < mediaFilePathsOrUrls.Count; i++)
                {
                    string item = mediaFilePathsOrUrls[i];
                    if (IsUrl(item))
                    {
                        using var httpClient = new HttpClient();
                        using var response = await httpClient.GetAsync(item);

                        // Get content type to determine extension
                        var contentType = response.Content.Headers.ContentType;
                        var extension = contentType.MediaType.Split('/')[1]; // Extract extension from content type

                        // Copy stream to avoid reuse issues
                        using var stream = await response.Content.ReadAsStreamAsync();
                        var memoryStream = new MemoryStream();
                        await stream.CopyToAsync(memoryStream);
                        memoryStream.Position = 0; // Reset position for reading

                        var streamContent = new StreamContent(memoryStream);
                        multipartContent.Add(streamContent, $"file{i}", $"image{i}.{extension}");
                        mediaGroup.Add(new InputMediaPhoto(InputFile.FromStream(memoryStream, $"image{i}.{extension}")));
                    }
                    else
                    {
                        var fileStream = System.IO.File.OpenRead(item);
                        string fileName = System.IO.Path.GetFileName(item);
                        var streamContent = new StreamContent(fileStream);
                        multipartContent.Add(streamContent, $"file{i}", fileName);
                        mediaGroup.Add(new InputMediaPhoto(InputFile.FromStream(streamContent.ReadAsStream(), fileName)));
                    }
                }

                if (mediaGroup.Count > 0)
                {
                    mediaGroup[0].Caption = messageText; // Set caption on the first item
                    await botClient.SendMediaGroupAsync(chatId, mediaGroup);
                }
            }
        }
        catch (ApiRequestException ex)
        {
            // Handle API request errors
            Console.WriteLine($"Telegram API Error: {ex.Message}");
        }
        catch (IOException ex)
        {
            // Handle file I/O errors
            Console.WriteLine($"File I/O Error: {ex.Message}");
        }
        catch (Exception ex)
        {
            // Handle other unexpected errors
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
    }

    // Helper method to check if a string is a URL
    private bool IsUrl(string text)
    {
        return Uri.TryCreate(text, UriKind.Absolute, out var uriResult)
            && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }

    private string NormalizeChannelUsername(string username)
    {
        // Remove protocols and leading/trailing spaces
        username = username.Trim().Replace("https://", "").Replace("http://", "");

        // Extract username from t.me links
        var match = Regex.Match(username, @"t\.me/([^/]+)");
        if (match.Success)
        {
            username = match.Groups[1].Value;
        }

        // Ensure username starts with "@"
        if (!username.StartsWith("@"))
        {
            username = "@" + username;
        }

        return username;
    }
}