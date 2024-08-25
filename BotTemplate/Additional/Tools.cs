using System.Data;
using System.Runtime.CompilerServices;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBotFramework;
using Template.Monitoring;

namespace Template.Additional
{
    public static class Tools
    {
        private static readonly PgProvider pg = new(Bot.DatabaseConnectionString);
        private static string sqlQuery = "";


        public static async Task DeleteDeadUsers(ITelegramBotClient botClient, UpdateInfo update)
        {   
            var users = pg.ExecuteSqlQueryAsEnumerable("select user_id, username from users").Select(a => new User
            {
                Id = a.Field<long>("user_id"),
                Username = a.Field<string?>("username")
            }).ToList();

            var deletedUsers = 0;

            await botClient.EditMessageTextAsync(update.Message.Chat.Id, update.Message.MessageId, "Запущена очистка. \n\n*По готовности будет уведомление*.", parseMode: ParseMode.Markdown);

            _ = Task.Run(async () =>
            {
                foreach (var user in users)
                {
                    try
                    {
                        await botClient.SendChatActionAsync(user.Id, ChatAction.Typing);
                    }
                    catch (Exception)
                    {
                        await DeleteUserFromDB(user);
                        deletedUsers++;
                        Thread.Sleep(50);
                        continue;
                    }
                }

                await botClient.SendTextMessageAsync(update.Message.Chat.Id, $"*Очистка юзеров успешно завершена* \n\nУдалено юзеров: *{deletedUsers}*", parseMode: ParseMode.Markdown);
            });
        }


        public static List<InlineKeyboardButton[]> ApplyPagination<T>(this List<InlineKeyboardButton[]> keyboard, int page, List<T> list)
        {
            if (page > 1 && list.Count < 10)
                keyboard.Add(new InlineKeyboardButton[] { new("👈🏻") { CallbackData = $"Back" } });
            else if (list.Count == 10 && page > 1)
                keyboard.Add(new InlineKeyboardButton[]
                {
            new("👈🏻") { CallbackData = $"Back" }, new("👉🏻") { CallbackData = $"Next" }
                });
            else if (list.Count == 10 && page == 1)
                keyboard.Add(new InlineKeyboardButton[] { new("👉🏻") { CallbackData = $"Next" } });

            return keyboard;
        }


        public static Stream GenerateStreamFromString(string s)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }


        public static async Task AddUserToDB(User user)
        {
            sqlQuery = $@"select 1
                              from users
                              where user_id = {user.Id}";
            if (!pg.ExecuteSqlQueryAsEnumerable(sqlQuery).Any())
            {
                sqlQuery = $@"insert into users
                                          (user_id,
                                           username,
                                           created_at)
                                   values ({user.Id},
                                           '{user.Username}',
                                            {DateTime.Now.ToTimeStamp()})";
                pg.ExecuteSqlQueryAsEnumerable(sqlQuery);

                await Logger.LogMessage($"Добавлен пользователь {user.Id} {(user.Username != null ? $"@{user.Username}" : null)}");
            }
        }


        public static async Task DeleteUserFromDB(User user)
        {
            sqlQuery = $@"select 1
                              from users
                              where user_id = {user.Id}";
            if (pg.ExecuteSqlQueryAsEnumerable(sqlQuery).Any())
            {
                sqlQuery = $@"delete from users where user_id = {user.Id}";
                pg.ExecuteSqlQueryAsEnumerable(sqlQuery);

                await Logger.LogMessage($"Удален пользователь {user.Id} {(user.Username != null ? $"@{user.Username}" : "")}");
            }
        }
    }
}
