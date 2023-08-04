namespace Template.Additional
{
    public static class Extensions
    {
        /// <summary>
        /// Возвращает описание указанного значения перечисления
        /// </summary>
        /// <param name="value">Выбранное значение перечисления</param>
        /// <returns></returns>
        public static string Description(this Enum value)
        {
            var field = value?.GetType().GetField(value?.ToString());
            var attributes = field?.GetCustomAttributes(false);

            dynamic? displayAttribute = null;

            if (attributes?.Any() == true)
                displayAttribute = attributes.ElementAt(0);

            return displayAttribute?.Description ?? string.Empty;
        }


        /// <summary>
        /// Конвертация значения DateTime в TimeStamp
        /// </summary>
        /// <param name="date">Значение DateTime, которое будет конвертировано в TimeSpan</param>
        /// <returns></returns>
        public static long ToTimeStamp(this DateTime date) => (long)(date - new DateTime(1970, 1, 1, 0, 0, 0, 0)).Duration().TotalSeconds;


        /// <summary>
        /// Конвертирует значение TimeStamp в DateTime
        /// </summary>
        /// <param name="date">Значение TimeSpan, которое будет конвертировано в DateTime</param>
        /// <returns></returns>
        public static DateTime ToDateTime(this long date) => new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(date);


        public static string ToDateTimeString(this DateTime date) => $@"{date:HH:mm dd.MM.yyyy}";


        public static string ToDateTimeString(this long date) => $@"{new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(date):HH:mm dd.MM.yyyy}";
    }
}
