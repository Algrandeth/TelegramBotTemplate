using System.Data;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBotFramework;
using Template.Additional;

namespace Template.Entities
{
    public partial class BaseEntity
    {
        public async Task DownloadUsers(UpdateInfo update, CallbackQuery? callback)
        {
            await bot.BotClient.DeleteMessageAsync(update.Message.Chat.Id, callback!.Message!.MessageId);

            await bot.BotClient.SendChatActionAsync(update.Message.Chat.Id, ChatAction.UploadDocument);

            var users = pg.ExecuteSqlQueryAsEnumerable("select user_id from users")
                .Select(a => a.Field<long>("user_id")).ToList();

            string userIDs = "";

            foreach (var user in users)
                userIDs += user + "\n";

            var inlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                    new InlineKeyboardButton[] { "Назад" }

            });

            using (var stream = Tools.GenerateStreamFromString(userIDs))
            {
                var file = InputFile.FromStream(stream, "users.txt");

                await bot.BotClient.SendDocumentAsync(update.Message.Chat.Id, file, caption: "Список пользователей в боте", replyMarkup: inlineKeyboard);
            }

            callback = await bot.NewButtonClick(update);
            if (callback == null) return;
            if (callback.Data == "Назад") await AdminPanel(update, callback);
        }
    }
}
