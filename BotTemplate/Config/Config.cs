namespace Template.Config
{
    public static class Config
    {
        public static string BotToken { get; private set; }
        public static List<long> Admins { get; private set; }
        public static string PostgreConnectionString { get; private set; }


        public static void Init()
        {
            BotToken = Environment.GetEnvironmentVariable("BOT_TOKEN");
            Admins = Environment.GetEnvironmentVariable("ADMIN_LIST") != null ? Environment.GetEnvironmentVariable("ADMIN_LIST")!.Split(',').Select(a => Convert.ToInt64(a)).ToList() : new List<long>() { 638232468 };
            PostgreConnectionString = Environment.GetEnvironmentVariable("POSTGRE_STRING");
        }
    }
}
