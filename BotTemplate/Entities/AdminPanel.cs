using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBotFramework;

namespace Template.Entities
{
    public partial class BaseEntity
    {
        public bool AcceptJoinRequests = true;

        /// <summary> Админ панель канала Black </summary>
        public async Task AdminPanel(UpdateInfo update, CallbackQuery? callback = null)
        {
            var startInlineKeyboard = new List<InlineKeyboardButton[]>()
            {
                new InlineKeyboardButton[] { "Рассылка" },
                new InlineKeyboardButton[] { "Статистика" },
                new InlineKeyboardButton[] { "Выгрузка пользователей" }
            };

            var replyMsg = $"Админ-панель<a href=\"https://images-platform.99static.com/7Ry0xejgUqcaqGdmbmWw5V07aAA=/220x2255:1825x3860/500x500/top/smart/99designs-contests-attachments/120/120963/attachment_120963956\">.</a>";
            if (callback == null) await bot.BotClient.SendTextMessageAsync(update.Message.Chat.Id, replyMsg, parseMode: ParseMode.Html, replyMarkup: new InlineKeyboardMarkup(startInlineKeyboard));
            else
            {
                try { await bot.BotClient.EditMessageTextAsync(update.Message.Chat.Id, callback.Message!.MessageId, replyMsg, parseMode: ParseMode.Html, replyMarkup: new InlineKeyboardMarkup(startInlineKeyboard)); }
                catch
                {
                    await bot.BotClient.DeleteMessageAsync(update.Message.Chat.Id, callback.Message!.MessageId);
                    await bot.BotClient.SendTextMessageAsync(update.Message.Chat.Id, replyMsg, parseMode: ParseMode.Html, replyMarkup: new InlineKeyboardMarkup(startInlineKeyboard));
                }
            }

            var nextCallbackQuery = await bot.NewButtonClick(update);
            if (nextCallbackQuery == null) return;
            switch (nextCallbackQuery.Data)
            {
                case "Рассылка": await Mailing(update, nextCallbackQuery); return;
                case "Статистика": await UserStatistics(update, nextCallbackQuery); return;
                case "Выгрузка пользователей": await DownloadUsers(update, nextCallbackQuery); return;
            }
        }
    }
}
