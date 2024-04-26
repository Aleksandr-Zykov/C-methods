/*
Parameters:
channelUsername: Username of the public Telegram channel (e.g., "@ChannelName").
n: Number of posts to retrieve.

Functionality:
Creates a Telegram bot client using your bot token.
Retrieves information about the specified channel.
Fetches the last n messages from the channel.
Extracts the message ID and text (or caption if available) for each post.
Returns a list of tuples, where each tuple contains the message ID and text content.
*/

public async Task<List<Tuple<int, string>>> GetLastNPostsAsync(string channelUsername, int n)
{
    var botClient = new TelegramBotClient("YOUR_BOT_TOKEN");
    var channelInfo = await botClient.GetChatAsync(channelUsername);

    var posts = await botClient.GetChannelMessagesAsync(channelInfo.Id, limit: n);

    var result = new List<Tuple<int, string>>();
    foreach (var post in posts)
    {
        result.Add(Tuple.Create(post.MessageId, post.Text ?? post.Caption ?? ""));
    }

    return result;
}