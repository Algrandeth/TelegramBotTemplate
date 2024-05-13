using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBotFramework;

namespace Template.Entities
{
    public partial class CommandHandler
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

            var replyMsg = $"Админ-панель<a href=\"https://i.pinimg.com/564x/c9/4d/ab/c94dab0f12a851df1edd4efb15f0b8c9.jpg\">.</a>";
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
                case "Рассылка": await Mailing(update/*, nextCallbackQuery*/); return;
                case "Статистика": await UserStatistics(update/*, nextCallbackQuery*/); return;
                case "Выгрузка пользователей": await DownloadUsers(update/*, nextCallbackQuery*/); return;
            }
        }
    }
}
