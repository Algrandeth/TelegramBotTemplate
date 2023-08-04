namespace Template.Additional
{
    /// <summary>
    /// Класс для выполнения вставок в pg sql
    /// </summary>
    public static class SqlHelper
    {
        /// <summary>
        /// Формирует SQL запрос для записи указанного значения с экранированием спец. символов и преобразованием данных под SQL формат
        /// </summary>
        /// <param name="dataType">Тип данных значения</param>
        /// <param name="value">Записываемое значение</param>
        /// <param name="isNullable">Указывает на то, что значение поддерживает NULL значения. Используется только для <see cref="SqlType.String"/>. Если isNullable=true, то будет возвращено NULL, иначе '' (Empty), но только когда value=IsNullOrEmpty</param>
        public static string GetInsertValue(SqlType dataType, object value, bool isNullable = true)
        {
            var _value = value?.ToString().Replace("'", "''");

            if (string.IsNullOrEmpty(_value) && (dataType != SqlType.String || isNullable))
                return "null";

            return dataType switch
            {
                SqlType.String => $"'{_value}'",
                SqlType.Int => _value ?? string.Empty,
                SqlType.Double => _value.Replace(",", "."),
                SqlType.DateTime => $"'{(DateTime?)value:yyyy-MM-dd HH:mm:ss}'",
                SqlType.Boolean => _value ?? string.Empty,
                _ => "",
            };
        }


        /// <summary>
        /// Формирует SQL запрос для обновления указанного значения с экранированием спец. символов и преобразованием данных под SQL формат
        /// </summary>
        /// <param name="dataType">Тип данных значения</param>
        /// <param name="fieldName">Название столбца в формате: TABLE_NAME.COLUMN_NAME</param>
        /// <param name="value">Обновляемое значение</param>
        /// <param name="updateIfNull">Произвести обновление данных для NULL записи (Затирание данных)</param>
        /// <param name="isNullable">Указывает на то, что значение поддерживает NULL значения. Используется только для <see cref="SqlType.String"/>. Если isNullable=true, то будет возвращено NULL, иначе '' (Empty), но только когда value=IsNullOrEmpty</param>
        /// <param name="addTextToTheEnd">Текст, который будет вставлен в конец строки</param>
        public static string GetUpdateValue(SqlType dataType, string fieldName, object value, string addTextToTheEnd = ",", bool updateIfNull = false, bool isNullable = true)
        {
            if (string.IsNullOrEmpty(value?.ToString()) && !updateIfNull)
                return "";

            return fieldName + "=" + GetInsertValue(dataType, value, isNullable) + addTextToTheEnd;
        }


        /// <summary>
        /// Получить тип <see cref="SqlType"> для объекта 
        /// </summary>
        /// <param name="value">Объект тип данных которого нужно определить</param>
        /// <returns></returns>
        public static SqlType GetSqlType(object value) =>
            GetSqlType(value.GetType());


        /// <summary>
        /// Получить тип <see cref="SqlType"> для указанного типа <see cref="Type">
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static SqlType GetSqlType(Type type)
        {
            if (type == typeof(double?) || type == typeof(double) || type == typeof(decimal?) || type == typeof(decimal))
                return SqlType.Double;

            if (type == typeof(DateTime?) || type == typeof(DateTime))
                return SqlType.DateTime;

            if (type == typeof(long?) || type == typeof(long) || type == typeof(int?) || type == typeof(int) || type == typeof(short?) || type == typeof(short))
                return SqlType.Int;

            if (type == typeof(bool?) || type == typeof(bool))
                return SqlType.Boolean;

            if (type == typeof(byte?) || type == typeof(byte))
                return SqlType.Boolean;


            return SqlType.String;
        }


        /// <summary>
        /// Поддерживаемые типы данных
        /// </summary>
        public enum SqlType
        {
            /// <summary>
            /// Текстовое значение
            /// </summary>
            String,
            /// <summary>
            /// Целочисленное значение
            /// </summary>
            Int,
            /// <summary>
            /// Дробное значение
            /// </summary>
            Double,
            /// <summary>
            /// Значение даты
            /// </summary>
            DateTime,
            /// <summary>
            /// Логическое значение
            /// </summary>
            Boolean
        }
    }
}
