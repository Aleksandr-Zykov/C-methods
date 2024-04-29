using System;
using System.Threading;
using System.Net.Http;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;
using TL;

class Program
{
    static async Task Main(string[] args)
    {
        WTelegramBot reader = new WTelegramBot();

        //Example usage with reading posts and dowloading images
        WTelegramBot.TelegramReadChannelRequest request = new WTelegramBot.TelegramReadChannelRequest()
        {
            ApiId = "YOUR_API_ID_FROM_TELEGRAM_APPLICATION",
            ApiHash = "YOUR_API_HASH_FROM_TELEGRAM_APPLICATION",
            PhoneNumber = "YOUR_PHONE_NUMBER",
            //VerificationCode = "RECEIVED_CODE", //on the first run we don't use it
            ChannelUsername = "https://t.me/uniannet",
            NumberOfPosts = 2
        };
        var result = await reader.ReadRecentPosts(request);
        int imageIndex = 0;
        foreach (var item in result)
        {
            Console.WriteLine(item.Text);
            // Save image to file
            string filePath = @$"downloaded_image{imageIndex}.jpg"; // Change the file path as needed
            foreach (var image in item.Images)
            {
                imageIndex++;
                File.WriteAllBytes(filePath, image);
            }

            Console.WriteLine("Image downloaded successfully.");
        }
    }
}

public class WTelegramBot
{ 
    public async Task<List<TelegramPost>> ReadRecentPosts(TelegramReadChannelRequest request)
    {
        // Configure WTelegram client
        string Config(string what)
        {
            switch (what)
            {
                case "api_id": return request.ApiId;
                case "api_hash": return request.ApiHash;
                case "phone_number": return request.PhoneNumber;
                case "verification_code": return request.VerificationCode;
                case "password": return request.Password;
                default: return null;
            }
        }

        using var client = new WTelegram.Client(Config);

        if (request.VerificationCode == default)
        {
            client.LoginUserIfNeeded();
            Thread.Sleep(5000); //Waiting for code, time can be changed
            throw new VerificationCodeNeededException();
        }
        await client.LoginUserIfNeeded();

        // Resolve channel username
        var resolvedPeer = await client.Contacts_ResolveUsername(NormalizeChannelUsername(request.ChannelUsername));
        var channel = resolvedPeer.chats[resolvedPeer.peer.ID] as Channel;

        // Get recent posts
        var messages = await client.Messages_GetHistory(channel, limit: request.NumberOfPosts + 1);

        // Extract post data
        var posts = new List<TelegramPost>();
        foreach (Message message in messages.Messages.OfType<Message>())
        {
            var post = new TelegramPost
            {
                Text = message.message,
                Images = new List<byte[]>()
            };

            if (message.media is MessageMediaPhoto photo)
            {
                using var memoryStream = new MemoryStream();
                await client.DownloadFileAsync((Photo)photo.photo, memoryStream);
                post.Images.Add(memoryStream.ToArray());
            }

            posts.Add(post);
        }

        return posts;
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

        // Remove "@" if it's present
        if (username.StartsWith("@"))
        {
            username = username.Substring(1);
        }

        return username;
    }
    public class TelegramPost
    {
        public string Text { get; set; }
        public List<byte[]> Images { get; set; }
    }

    public class TelegramReadChannelRequest
    {
        public string ApiId { get; set; }
        public string ApiHash { get; set; }
        public string PhoneNumber { get; set; }
        public string VerificationCode { get; set; }
        public string Password { get; set; }
        public string ChannelUsername { get; set; }
        public int NumberOfPosts { get; set; }
    }

    public class VerificationCodeNeededException : Exception
    {
        public VerificationCodeNeededException() : base("Verification code is required.") { }

        public VerificationCodeNeededException(string message) : base(message) { }

        public VerificationCodeNeededException(string message, Exception innerException) : base(message, innerException) { }
    }
}