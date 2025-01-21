using System.Drawing;
using ZXing;
using ZXing.Common;
using ZXing.QrCode;
using ZXing.Windows.Compatibility;
using Microsoft.Extensions.Configuration;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

//Build configuration
var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory) // Set the base directory
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true) // Load appsettings.json
    .Build();

// Retrieve the bot token
string botToken = configuration["TelegramBotToken"];

if (string.IsNullOrEmpty(botToken))
{
    Console.WriteLine("Bot token not found in configuration.");
    return;
}

// Initialize the bot client
var botClient = new TelegramBotClient(botToken);

Console.WriteLine("Bot is starting...");
var cancellationToken = new CancellationTokenSource().Token;
var receiverOptions = new ReceiverOptions();

botClient.StartReceiving(
    updateHandler: HandleUpdateAsync,
    errorHandler: HandleErrorAsync,
    receiverOptions: receiverOptions,
    cancellationToken: cancellationToken
);

Console.WriteLine("Bot is running. Press Enter to exit.");
Console.ReadLine();

// Handle incoming updates
async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    if (update.Message is not { } message) return;

    var chatId = message.Chat.Id;
    string? response;

    if (message.Type == MessageType.Text)
    {
        response = "QR codty engiziniz";

        await botClient.SendMessage(
        chatId: chatId,
        text: response,
        cancellationToken: cancellationToken
    );
    }
    else if (message.Type == MessageType.Photo)
    {
        var fileId = message.Photo[^1].FileId;
        var file = await botClient.GetFile(fileId);
        var filePath = file.FilePath;
        // Download the image
        var imagePath = $"temp_{Guid.NewGuid()}.jpg";
        await using (var stream = new FileStream(imagePath, FileMode.Create))
            await botClient.DownloadFile(filePath, stream);

        byte[] imageBytes = System.IO.File.ReadAllBytes(imagePath);

        response = imagePath;

        string[] result = ReadQRCode(imageBytes).Split(',');
        System.IO.File.Delete(imagePath);
        string lat = result[0];
        string lon = result[1];
        response = $"https://osmand.net/map/?pin={lat},{lon}#9/{lat}/{lon}";

        await botClient.SendMessage(
            chatId: chatId,
            text: response,
            cancellationToken: cancellationToken
        );
    }
}

// Error handler: Log detailed error information
Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
{
    Console.WriteLine("An error occurred during bot operation:");
    Console.WriteLine($"Message: {exception.Message}");

    if (exception.InnerException != null)
    {
        Console.WriteLine("Inner Exception:");
        Console.WriteLine($"Message: {exception.InnerException.Message}");
        Console.WriteLine($"Stack Trace: {exception.InnerException.StackTrace}");
    }

    Console.WriteLine("Stack Trace:");
    Console.WriteLine(exception.StackTrace);

    return Task.CompletedTask;
}

string? ReadQRCode(byte[] byteArray)
{
    Bitmap target;
    target = ByteToBitmap(byteArray);
    BitmapLuminanceSource source = new BitmapLuminanceSource(target);
    var bitmap = new BinaryBitmap(new HybridBinarizer(source));
    var reader = new QRCodeReader();
    var result = reader.decode(bitmap);
    return result?.Text;
}

Bitmap ByteToBitmap(byte[] byteArray)
{
    Bitmap target;
    using (var stream = new MemoryStream(byteArray))
    {
        target = new Bitmap(stream);
    }

    return target;
}

string imagePath = "Capture.png";

if (!System.IO.File.Exists(imagePath))
{
    Console.WriteLine("File does not exist. Please check the path.");
    return;
}
