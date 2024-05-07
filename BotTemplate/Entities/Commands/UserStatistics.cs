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
        /// <summary> Bot users count </summary>
        public async Task UserStatistics(UpdateInfo update, CallbackQuery? callback = null)
        {
            var totalUsersCount = pg.ExecuteSqlQueryAsEnumerable("select count(user_id) as count from users").First().Field<long>("count");

            var replyMsg = $"<b>Пользователей в боте:</b> <code>{totalUsersCount}</code>";

            var inlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                    new InlineKeyboardButton[] { "Удалить мертвых юзеров" },
                    new InlineKeyboardButton[] { "Назад" }
            });

            await bot.BotClient.EditMessageTextAsync(update.Message.Chat.Id, callback!.Message!.MessageId, replyMsg, parseMode: ParseMode.Html, replyMarkup: inlineKeyboard);

            var nextButton = await bot.NewButtonClick(update);
            if (nextButton == null) return;
            if (nextButton.Data == "Назад") await AdminPanel(update, nextButton);
            if (nextButton.Data == "Удалить мертвых юзеров")
            {
                await Tools.DeleteDeadUsers(bot.BotClient, update);
                await AdminPanel(update);
            }
        }
    }
}
