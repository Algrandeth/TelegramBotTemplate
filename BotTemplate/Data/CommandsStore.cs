using Telegram.Bot.Types;

namespace Template.Data
{
    public static class CommandsStore
    {
        public static List<BotCommand> AdminCommandsList = new()
        {
            new BotCommand() { Command = "/stats", Description = "Статистика пользователей в боте" },
            new BotCommand() { Command = "/download_users", Description = "Скачать базу пользователей" },
            new BotCommand() { Command = "/mailing", Description = "Рассылка" }
        };


        public static List<BotCommand> UserCommandsList = new()
        {
            new BotCommand() { Command = "/start", Description = "Перезапуск бота" },
        };


        public static List<string> CommandList = new();


        public static List<string> MetaCommandList = new()
        {

        };
        

        public static void InitCommandList()
        {
            List<string> adminCommands = AdminCommandsList.Select(a => a.Command).ToList();
            List<string> usersCommands = UserCommandsList.Select(a => a.Command).ToList();

            CommandList.AddRange(adminCommands);
            CommandList.AddRange(usersCommands);
            CommandList = CommandList.Distinct().ToList();
        }
    }
}
