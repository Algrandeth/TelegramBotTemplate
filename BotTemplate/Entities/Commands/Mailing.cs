using System.Data;
using System.Text.RegularExpressions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBotFramework;
using Template.Monitoring;

namespace Template.Entities
{
    public partial class CommandHandler
    {
        /// <summary> Рассылка по выбранным тематикам </summary>
        public async Task Mailing(UpdateInfo update, CallbackQuery? callback = null)
        {
            var userMessagePhoto = default(string?);
            var userMessageText = default(string?);
            var userMessageEntities = default(MessageEntity[]);
            var userMessageInlineKeyboard = default(InlineKeyboardButton[]?);

            var newOrRetryMsg = "<b>Рассылка по пользователям бота.\n\n" +
                                $"Пришли картинку и текст.</b>";

            if (callback == null)
                await bot.BotClient.SendTextMessageAsync(update.Message.Chat.Id, newOrRetryMsg, parseMode: ParseMode.Html, disableWebPagePreview: true, replyMarkup: new InlineKeyboardMarkup(new List<InlineKeyboardButton[]>()
                {
                    //new InlineKeyboardButton[] { "Назад" }
                }));
            else
            {
                await bot.BotClient.DeleteMessageAsync(update.Message.Chat.Id, callback.Message.MessageId);
                await bot.BotClient.SendTextMessageAsync(update.Message.Chat.Id, newOrRetryMsg, parseMode: ParseMode.Html, disableWebPagePreview: true, replyMarkup: new InlineKeyboardMarkup(new List<InlineKeyboardButton[]>()
                {
                    //new InlineKeyboardButton[] { "Назад" }
                }));
            }

            var continueButtonPressed = false;
            do
            {
                var nextMessage = await bot.NewFullMessage(update);
                if (nextMessage == null) return;
                //if (nextMessage.Text == "Назад")
                //{
                //    await bot.BotClient.DeleteMessageAsync(update.Message.Chat.Id, update.Message.MessageId);
                //    await AdminPanel(update);
                //    return;
                //}

                try
                {
                    continueButtonPressed = true;

                    userMessageText = nextMessage.Text ?? nextMessage.Caption;

                    if (nextMessage.Photo != null)
                        userMessagePhoto = nextMessage.Photo.First().FileId;

                    userMessageEntities = nextMessage?.Entities ?? nextMessage?.CaptionEntities;

                    var keyboard = new InlineKeyboardMarkup(new[] { new InlineKeyboardButton[] { "Продолжить", "Изменить" } });

                    if (userMessagePhoto != null) await bot.BotClient.SendPhotoAsync(update.Message.Chat.Id, InputFile.FromFileId(userMessagePhoto), caption: userMessageText, replyMarkup: keyboard, captionEntities: userMessageEntities);
                    else await bot.BotClient.SendTextMessageAsync(update.Message.Chat.Id, userMessageText, replyMarkup: keyboard, disableWebPagePreview: true, entities: userMessageEntities);

                    var nextCallbackQuery = await bot.NewButtonClick(update);
                    if (nextCallbackQuery == null) return;


                    if (nextCallbackQuery.Data == "Продолжить")
                    {
                        await bot.BotClient.DeleteMessageAsync(update.Message.Chat.Id, nextCallbackQuery.Message.MessageId);
                        await bot.BotClient.SendTextMessageAsync(update.Message.Chat.Id, "<b>Прикрепить кнопку?</b>", parseMode: ParseMode.Html, replyMarkup: new InlineKeyboardMarkup(new[]
                        {
                            new InlineKeyboardButton[] { "Да", "Нет" }
                        }));

                        nextCallbackQuery = await bot.NewButtonClick(update);
                        if (nextCallbackQuery == null) return;
                        else if (nextCallbackQuery.Data == "Да")
                        {
                            continueButtonPressed = false;
                            while (continueButtonPressed == false)
                            {
                                var text = $"Пришлите *текст* и *ссылку* для кнопки в таком формате:\n\n" +
                                           $"текст - ссылка\n\n" +
                                           $"*Например:* \n\nУчаствовать - https://t.me/test";

                                await bot.BotClient.DeleteMessageAsync(nextCallbackQuery.From.Id, nextCallbackQuery.Message.MessageId);
                                await bot.BotClient.SendTextMessageAsync(update.Message.Chat.Id, text, parseMode: ParseMode.Markdown);

                                var newTextMessage = await bot.NewTextMessage(update);

                                var buttonRegex = new Regex(@"(.*) - (.*)");

                                var buttonName = buttonRegex.Match(newTextMessage).Groups[1].Value;
                                var buttonUrl = buttonRegex.Match(newTextMessage).Groups[2].Value;

                                var userKeyboardButton = new InlineKeyboardButton[] { new InlineKeyboardButton(buttonName) { Url = buttonUrl } };

                                await bot.BotClient.SendTextMessageAsync(update.Message.Chat.Id, "Так будет выглядеть ваша <b>кнопка</b> под сообщением.\n\n<b>Хотите продолжить?</b>", parseMode: ParseMode.Html, replyMarkup: new InlineKeyboardMarkup(new[]
                                {
                                    userKeyboardButton,
                                    new InlineKeyboardButton[] { "Продолжить", "Изменить" }
                                }));

                                nextCallbackQuery = await bot.NewButtonClick(update);
                                if (nextCallbackQuery == null) return;
                                else if (nextCallbackQuery.Data == "Изменить")
                                    continue;
                                else if (nextCallbackQuery.Data == "Продолжить")
                                {

                                    userMessageInlineKeyboard = userKeyboardButton;
                                    continueButtonPressed = true;
                                }
                            }
                        }

                        await bot.BotClient.DeleteMessageAsync(nextCallbackQuery.From.Id, nextCallbackQuery.Message.MessageId);

                        continueButtonPressed = true;
                    }

                    else if (nextCallbackQuery.Data == "Изменить")
                    {
                        continueButtonPressed = false;

                        await Mailing(update, nextCallbackQuery);
                    }
                }
                catch (Exception ex)
                {
                    var replyMsg = $@"Возникло необработанное исключение. Попробуйте другой пост.";
                    await bot.BotClient.SendTextMessageAsync(update.Message.Chat.Id, replyMsg);
                }
            }
            while (continueButtonPressed == false);


            if (userMessagePhoto != null)
                await bot.BotClient.SendPhotoAsync(update.Message.Chat.Id, InputFile.FromFileId(userMessagePhoto), caption: userMessageText, replyMarkup: userMessageInlineKeyboard != null ? new InlineKeyboardMarkup(userMessageInlineKeyboard) : null, captionEntities: userMessageEntities);
            else
                await bot.BotClient.SendTextMessageAsync(update.Message.Chat.Id, userMessageText, replyMarkup: userMessageInlineKeyboard != null ? new InlineKeyboardMarkup(userMessageInlineKeyboard) : null, entities: userMessageEntities);

            await bot.BotClient.SendTextMessageAsync(update.Message.Chat.Id, "<b>Так выглядит ваше сообщение для рассылки.</b>", parseMode: ParseMode.Html, replyMarkup: new InlineKeyboardMarkup(new[]
            {
                new InlineKeyboardButton[] { "Разослать", "Отменить" }
            }));

            var nextCallback = await bot.NewButtonClick(update);
            if (nextCallback == null) return;
            else if (nextCallback.Data == "Разослать")
            {
                sqlQuery = $@"select user_id
                                  from users
                                  where user_id not in ({update.Message.Chat.Id})";
                var usersToSend = pg.ExecuteSqlQueryAsEnumerable(sqlQuery).Select(a => new
                {
                    UserID = a.Field<long>("user_id")
                }).ToList();


                await bot.BotClient.EditMessageTextAsync(update.Message.Chat.Id, nextCallback.Message.MessageId, $"*Рассылка запущена.* Получателей: *{usersToSend.Count}* \n\n*По готовности вам придет уведомление.*", parseMode: ParseMode.Markdown);

                _ = Task.Run(async () =>
                {
                    var usersGetMessageCount = 0;
                    foreach (var user in usersToSend)
                    {
                        try
                        {
                            if (userMessagePhoto != null) await bot.BotClient.SendPhotoAsync(user.UserID, InputFile.FromFileId(userMessagePhoto), caption: userMessageText, replyMarkup: userMessageInlineKeyboard != null ? new InlineKeyboardMarkup(userMessageInlineKeyboard) : null, captionEntities: userMessageEntities);
                            else await bot.BotClient.SendTextMessageAsync(user.UserID, userMessageText, disableWebPagePreview: true, replyMarkup: userMessageInlineKeyboard != null ? new InlineKeyboardMarkup(userMessageInlineKeyboard) : null, entities: userMessageEntities);

                            usersGetMessageCount++;
                        }
                        catch (Exception ex)
                        {
                            Thread.Sleep(3000);

                            await Logger.LogError("Ошибка при рассылке: " + ex.Message);
                            continue;
                        }
                    }

                    await bot.BotClient.SendTextMessageAsync(update.Message.Chat.Id, $"*Рассылка успешно завершена*. \n\nПользователей получило сообщение: *{usersGetMessageCount}*.", parseMode: ParseMode.Markdown);
                });
            }
            else if (nextCallback.Data == "Отменить")
            {
                await bot.BotClient.SendTextMessageAsync(update.Message.Chat.Id, "<b>Рассылка отменена.</b>", parseMode: ParseMode.Html);
            }//await AdminPanel(update, nextCallback);
        }
    }
}
