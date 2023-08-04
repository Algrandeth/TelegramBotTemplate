using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using TelegramBotFramework;

namespace Template.Entities
{
    public partial class BaseEntity
    {
        public static PgProvider pg = new(Bot.DatabaseConnectionString);
        public static string sqlQuery = "";

        public BaseEntity(Bot nb) => bot = nb;

        public readonly Bot bot;


        /// <summary> Start command handler </summary>
        public async Task Start(UpdateInfo update)
        {
            var replyMsg = "👋 <b>Hi</b>";

            await bot.BotClient.SendTextMessageAsync(update.Message.Chat.Id, replyMsg, parseMode: ParseMode.Html);
        }
    }
}
