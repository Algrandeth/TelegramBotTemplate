using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace Template.Data
{
    public class Structures
    {
        public struct GreetingMessage
        {
            public string Text { get; set; }
            public string? PictureID { get; set; }
            public List<InlineKeyboardButton[]> Buttons { get; set; }
            public MessageEntity[]? Entities { get; set; }
        }


        public struct Topic
        {
            public long TopicID { get; set; }
            public string Name { get; set; }
        }
    }
}
