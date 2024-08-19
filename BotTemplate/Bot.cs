﻿using System.Globalization;
using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramBotFramework;
using Template.Additional;
using Template.Data;
using Template.Entities;
using Template.Monitoring;

namespace Template
{
    public class Bot : TelegramBot
    {
        public static string DatabaseConnectionString;
        private static CommandHandler CommandsHandler;

        public Bot(string botToken) : base(botToken) { }

        private static async Task Main()
        {
            Config.Config.Init();
            CommandsStore.InitCommandList();

            DatabaseConnectionString = Config.Config.PostgreConnectionString;
            CultureInfo culture = new CultureInfo("ru-RU");
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            Bot bot = new(Config.Config.BotToken);

            CommandsHandler = new(bot);
            await bot.RunAsync();
        }


        /// <summary> Private chat update handler. </summary>
        public override async Task OnPrivateChat(Chat chat, User user, UpdateInfo update)
        {
            try
            {
                switch (update.UpdateKind)
                {
                    case UpdateKind.NewMessage: await HandleMessage(BotClient, update); return;
                    case UpdateKind.CallbackQuery: await HandleCallbackQuery(update); return;
                    case UpdateKind.OtherUpdate:
                        {
                            if (update.Update.MyChatMember != null)
                            {
                                if (update.Update.MyChatMember.NewChatMember is ChatMemberBanned)
                                {
                                    await Tools.DeleteUserFromDB(update.Update.MyChatMember.From);
                                }


                                if (update.Update.MyChatMember.NewChatMember is ChatMemberMember)
                                {
                                    await Tools.AddUserToDB(update.Update.MyChatMember.From);
                                }
                            }
                        }
                        return;
                }
            }
            catch (Exception ex)
            {
                await Logger.LogCritical(ex.Message + " " + ex.StackTrace);
                await BotClient.SendTextMessageAsync(638232468, $"{(ex.InnerException != null ? ex.InnerException.Message + ex.StackTrace : ex.Message, ex.StackTrace)}");
            }
        }


        /// <summary> Channel update handler </summary>
        public override async Task OnChannel(Chat chat, User user, UpdateInfo update)
        {
            try
            {
                switch (update.UpdateKind)
                {
                    case UpdateKind.NewMessage: await HandleMessage(BotClient, update); break;
                    case UpdateKind.CallbackQuery: await HandleCallbackQuery(update); break;
                    case UpdateKind.OtherUpdate: break;
                }
            }
            catch (Exception ex)
            {
                await Logger.LogCritical(ex.Message + " " + ex.StackTrace);
                await BotClient.SendTextMessageAsync(638232468, $"{(ex.InnerException != null ? ex.InnerException.Message + ex.StackTrace : ex.Message, ex.StackTrace)}");
            }
        }


        /// <summary> Update message handler </summary>
        public override async Task HandleMessage(ITelegramBotClient botClient, UpdateInfo update)
        {
            if (Config.Config.Admins.Any(a => a == update.Message.Chat.Id))
                switch (update.Message.Text)
                {
                    case "/stats": await CommandsHandler.UserStatistics(update); return;
                    case "/download_users": await CommandsHandler.DownloadUsers(update); return;
                    case "/mailing": await CommandsHandler.Mailing(update); return;
                }

            switch (update.Message.Text)
            {
                case "/start": await CommandsHandler.Start(update); return;
            }
        }


        /// <summary> Update callback handler </summary>
        public async Task HandleCallbackQuery(UpdateInfo update)
        {
            await Logger.LogMessage("got callback");
            await BotClient.AnswerCallbackQueryAsync(update.CallbackQuery.Id);
        }
    }
}
