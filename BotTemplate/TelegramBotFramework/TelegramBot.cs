﻿using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Template.Additional;
using Template.Config;
using Template.Data;
using Template.Monitoring;

namespace TelegramBotFramework
{
    public abstract class TelegramBot
    {
        public readonly TelegramBotClient BotClient;
        public User Me { get; private set; }

        private readonly Dictionary<long, TaskInfo> _tasks = new();

        private int _lastUpdateId = -1;

        public abstract Task OnPrivateChat(Chat chat, User user, UpdateInfo update);
        public abstract Task OnChannel(Chat chat, User user, UpdateInfo update);

        public abstract Task HandleMessage(ITelegramBotClient botClient, UpdateInfo update);

        private readonly CancellationTokenSource cts = new();

        public TelegramBot(string botToken)
        {
            BotClient = new(botToken);
            Me = Task.Run(() => BotClient.GetMeAsync()).Result;
        }

        public async Task RunAsync()
        {
            await Logger.StartMessage('@' + Me.Username);

            await Task.Run(() =>
            {
                BotClient.StartReceiving(UpdateHandler, ErrorHandler, cancellationToken: cts.Token, receiverOptions: new Telegram.Bot.Polling.ReceiverOptions() { ThrowPendingUpdates = true });
            });

            await BotClient.SetMyCommandsAsync(CommandsStore.UserCommandsList, BotCommandScope.AllPrivateChats());

            foreach (var admin in Config.Admins)
                try
                {
                    await BotClient.SetMyCommandsAsync(CommandsStore.AdminCommandsList, BotCommandScope.Chat(admin));
                }
                catch (Exception) { }

            while (true)
            {
                await Task.Delay(TimeSpan.FromHours(3));
                _tasks.Clear();
            }
        }


        /// <summary> Error handler </summary>
        public static async Task ErrorHandler(ITelegramBotClient botClient, Exception exception, CancellationToken cts)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}] {apiRequestException.Message}",
                _ => exception.Message.ToString()
            };

            await Logger.LogCritical(ErrorMessage);
        }


        /// <summary>chat update handler</summary>
        public async Task UpdateHandler(ITelegramBotClient botClient, Update update, CancellationToken cts)
        {
            try
            {
                if (update.Id <= _lastUpdateId) return;
                _lastUpdateId = update.Id;

                if (update.Message != null)
                    await Tools.AddUserToDB(update.Message.From!);


                switch (update.Type)
                {
                    case UpdateType.Message: await HandleUpdate(update, UpdateKind.NewMessage, update.Message); break;
                    case UpdateType.EditedMessage: await HandleUpdate(update, UpdateKind.EditedMessage, update.EditedMessage); break;
                    case UpdateType.ChannelPost: await HandleUpdate(update, UpdateKind.NewMessage, update.ChannelPost); break;
                    case UpdateType.EditedChannelPost: await HandleUpdate(update, UpdateKind.EditedMessage, update.EditedChannelPost); break;
                    case UpdateType.CallbackQuery: await HandleUpdate(update, UpdateKind.CallbackQuery, update.CallbackQuery!.Message); break;
                    case UpdateType.MyChatMember: await HandleUpdate(update, UpdateKind.OtherUpdate, chat: update.MyChatMember!.Chat); break;
                    case UpdateType.ChatMember: await HandleUpdate(update, UpdateKind.OtherUpdate, chat: update.ChatMember!.Chat); break;
                    case UpdateType.ChatJoinRequest: await HandleUpdate(update, UpdateKind.OtherUpdate, chat: update.ChatJoinRequest!.Chat); break;
                    default: await HandleUpdate(update, UpdateKind.OtherUpdate); break;
                }
            }
            catch (Exception ex)
            {
                await Logger.LogCritical(ex.Message);
            }
        }


        private async Task HandleUpdate(Update update, UpdateKind updateKind, Message? message = null, Chat? chat = null)
        {
            try
            {
                TaskInfo taskInfo;
                chat ??= message?.Chat;
                long chatId = chat?.Id ?? 0;

                TaskInfo? existingTask;
                lock (_tasks)
                    _tasks.TryGetValue(chatId, out existingTask);

                taskInfo = existingTask ?? new TaskInfo();
                if (existingTask == null)
                {
                    lock (_tasks)
                        _tasks[chatId] = taskInfo;
                }

                var updateInfo = new UpdateInfo(taskInfo) { UpdateKind = updateKind, Update = update, Message = message };
                if (update.Type is UpdateType.CallbackQuery)
                    updateInfo.CallbackQuery = update.CallbackQuery;

                if (taskInfo.Task != null)
                {
                    lock (taskInfo)
                    {
                        taskInfo.Updates.Enqueue(updateInfo);
                        taskInfo.Semaphore.Release();
                    }
                    return;
                }

                await RunTask(taskInfo, updateInfo, chat!);
            }
            catch (Exception ex)
            {
                await Logger.LogCritical($"Ошибка в обработке апдейта: {ex}");
            }
        }


        private async Task RunTask(TaskInfo taskInfo, UpdateInfo updateInfo, Chat chat)
        {
            Func<Task> taskStarter;

            if (chat?.Type == ChatType.Private) taskStarter = () => OnPrivateChat(chat, updateInfo.Message?.From!, updateInfo);
            else if (chat?.Type == ChatType.Channel) taskStarter = () => OnChannel(chat, updateInfo.Message?.From!, updateInfo);
            else return;

            taskInfo.Task = Task.Run(taskStarter).ContinueWith(async t =>
            {
                if (taskInfo.Semaphore.CurrentCount == 0)
                {
                    taskInfo.Task = null!;
                    return;
                }

                await taskInfo.Semaphore.WaitAsync();
                try
                {
                    var newUpdate = await ((IGetNext)updateInfo).NextUpdate(cts.Token);
                    await RunTask(taskInfo, newUpdate, chat);
                }
                catch (Exception ex)
                {
                    await Logger.LogCritical(ex);
                }
                finally
                {
                    taskInfo.Semaphore.Release();
                }
            });
        }



        /// <summary> Detects chat update kind </summary>
        public async Task<UpdateKind> NextEvent(UpdateInfo update, CancellationToken ct = default)
        {
            using var bothCT = CancellationTokenSource.CreateLinkedTokenSource(ct, cts.Token);
            var newUpdate = await ((IGetNext)update).NextUpdate(bothCT.Token);

            update.Message = newUpdate.Message;
            if (newUpdate.CallbackQuery != null) update.CallbackData = newUpdate.CallbackQuery.Data;
            update.Update = newUpdate.Update;

            return update.UpdateKind = newUpdate.UpdateKind;
        }



        public async Task<CallbackQuery?> NewButtonClick(UpdateInfo update, Message? msg = null, CancellationToken ct = default)
        {
            while (true)
            {
                switch (await NextEvent(update, ct))
                {
                    case UpdateKind.NewMessage:
                        if (CommandsStore.CommandList.Any(a => a == update.Message.Text) || (update.Message.Text != null && CommandsStore.MetaCommandList.Any(a => update.Message.Text.Contains(a))))
                        {
                            await HandleMessage(BotClient, update);
                            return null;
                        }
                        break;

                    case UpdateKind.CallbackQuery:
                        if (msg != null && update.Message.MessageId != msg.MessageId)
                            _ = BotClient.AnswerCallbackQueryAsync(update.Update.CallbackQuery.Id, null, cancellationToken: ct);
                        else
                            return update.Update.CallbackQuery;
                        continue;

                    case UpdateKind.OtherUpdate
                        when update.Update.MyChatMember is ChatMemberUpdated
                        { NewChatMember: { Status: ChatMemberStatus.Left or ChatMemberStatus.Kicked } }:
                        {
                            return null;
                        }
                }
            }
        }


        /// <summary> Awaiting user next message </summary>
        public async Task<MsgCategory> NewMessage(UpdateInfo update, CancellationToken ct = default)
        {
            while (true)
            {
                switch (await NextEvent(update, ct))
                {
                    case UpdateKind.NewMessage
                        when update.MsgCategory is MsgCategory.Text or MsgCategory.MediaOrDoc or MsgCategory.StickerOrDice:
                        return update.MsgCategory;

                    case UpdateKind.CallbackQuery:
                        BotClient.AnswerCallbackQueryAsync(update.Update.CallbackQuery!.Id, null, cancellationToken: ct);
                        //if (update.MsgCategory == MsgCategory.Other)
                        return update.MsgCategory;

                    case UpdateKind.OtherUpdate
                        when update.Update.MyChatMember is ChatMemberUpdated
                        {
                            NewChatMember:
                            { Status: ChatMemberStatus.Left or ChatMemberStatus.Kicked }
                        }:
                        {
                            throw new LeftTheChatException();
                        }
                }
            }
        }


        public async Task<string?> NewPictureMessage(UpdateInfo update, CancellationToken ct = default)
        {
            bool awaited = true;
            while (awaited)
            {
                var newMessage = await NewMessage(update, ct);
                if (newMessage == MsgCategory.MediaOrDoc && update.Message.Photo != null) awaited = false;
                if (update.CallbackData == "Назад") return update.CallbackData;
            }

            if (CommandsStore.CommandList.Any(a => a == update.Message.Text) || (update.Message.Text != null && CommandsStore.MetaCommandList.Any(a => update.Message.Text.Contains(a))))
            {
                await HandleMessage(BotClient, update);
                return null;
            }

            if (update.Message.Photo == null)
                return "error";

            var image = update.Message.Photo.First();
            return image.FileId;
        }


        /// <summary> Awaiting user next text message </summary>
        public async Task<string?> NewTextMessage(UpdateInfo update, CancellationToken ct = default)
        {
            bool awaited = true;
            while (awaited)
            {
                var newMessage = await NewMessage(update, ct);
                if (newMessage == MsgCategory.Text) awaited = false;
                if (update.CallbackData == "Назад") return update.CallbackData;
            }

            if (CommandsStore.CommandList.Any(a => a == update.Message.Text) || (update.Message.Text != null && CommandsStore.MetaCommandList.Any(a => update.Message.Text.Contains(a))))
            {
                await HandleMessage(BotClient, update);
                return null;
            }

            return update.Message.Text!.Replace(";", "").Replace("'", "");
        }


        public async Task<Message?> NewFullMessage(UpdateInfo update, CancellationToken ct = default)
        {
            bool awaited = true;
            while (awaited)
            {
                var newMessage = await NewMessage(update, ct);
                if (newMessage == MsgCategory.Text || newMessage == MsgCategory.MediaOrDoc) awaited = false;
                if (update.CallbackData == "Назад") return new Message { Text = "Назад" };
            }


            if (CommandsStore.CommandList.Any(a => a == update.Message.Text) || (update.Message.Text != null && CommandsStore.MetaCommandList.Any(a => update.Message.Text.Contains(a))))
            {
                await HandleMessage(BotClient, update);
                return null;
            }

            return update.Message;
        }
    }

    public class LeftTheChatException : Exception { public LeftTheChatException() : base("The chat was left") { } }
}
