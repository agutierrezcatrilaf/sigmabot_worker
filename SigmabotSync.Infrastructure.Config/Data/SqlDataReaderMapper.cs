using System;
using System.Data;
using System.Globalization;
using Microsoft.Data.SqlClient;
using System.Reflection;

namespace SigmabotSync.Infrastructure.Data
{
    /// <summary>
    /// Mapea un SqlDataReader o DataRow a un objeto por nombre de propiedad (columnas y propiedades con el mismo nombre, insensible a mayúsculas).
    /// </summary>
    public static class SqlDataReaderMapper
    {
        /// <summary>
        /// Mapea una fila de un DataTable al tipo T. Las columnas se asocian a propiedades por nombre (case-insensitive).
        /// </summary>
        public static T MapTo<T>(this DataRow row) where T : new()
        {
            if (row == null)
                return default(T);
            var obj = new T();
            var type = typeof(T);
            var table = row.Table;

            foreach (DataColumn col in table.Columns)
            {
                string columnName = col.ColumnName;
                var prop = GetProperty(type, columnName);
                if (prop == null || !prop.CanWrite)
                    continue;

                object value = row[col];
                try
                {
                    prop.SetValue(obj, ConvertValue(value, prop.PropertyType));
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        $"Error al mapear columna '{columnName}' a {type.Name}.{prop.Name}: {ex.Message}", ex);
                }
            }

            return obj;
        }

        /// <summary>
        /// Mapea la fila actual del reader al tipo T. Las columnas se asocian a propiedades por nombre (case-insensitive).
        /// </summary>
        public static T MapTo<T>(this SqlDataReader reader) where T : new()
        {
            var obj = new T();
            var type = typeof(T);

            for (int i = 0; i < reader.FieldCount; i++)
            {
                string columnName = reader.GetName(i);
                var prop = GetProperty(type, columnName);
                if (prop == null || !prop.CanWrite)
                    continue;

                object value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                try
                {
                    prop.SetValue(obj, ConvertValue(value, prop.PropertyType));
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        $"Error al mapear columna '{columnName}' a {type.Name}.{prop.Name}: {ex.Message}", ex);
                }
            }

            return obj;
        }

        private static PropertyInfo GetProperty(Type type, string columnName)
        {
            foreach (var p in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (string.Equals(p.Name, columnName, StringComparison.OrdinalIgnoreCase))
                    return p;
            }
            return null;
        }

        private static object ConvertValue(object value, Type targetType)
        {
            if (value == null || value == DBNull.Value)
            {
                if (targetType.IsValueType && Nullable.GetUnderlyingType(targetType) == null)
                    return Activator.CreateInstance(targetType); // 0, false, etc.
                return null;
            }

            if (targetType.IsAssignableFrom(value.GetType()))
                return value;

            if (targetType == typeof(string))
                return Convert.ToString(value, CultureInfo.InvariantCulture)?.Trim();

            Type nullableUnderlying = Nullable.GetUnderlyingType(targetType);
            Type convertType = nullableUnderlying ?? targetType;
            var inv = CultureInfo.InvariantCulture;

            try
            {
                if (convertType == typeof(int))
                    return Convert.ToInt32(value, inv);
                if (convertType == typeof(long))
                    return Convert.ToInt64(value, inv);
                if (convertType == typeof(short))
                    return Convert.ToInt16(value, inv);
                if (convertType == typeof(byte))
                    return Convert.ToByte(value, inv);
                if (convertType == typeof(double))
                    return Convert.ToDouble(value, inv);
                if (convertType == typeof(float))
                    return Convert.ToSingle(value, inv);
                if (convertType == typeof(decimal))
                    return Convert.ToDecimal(value, inv);
                if (convertType == typeof(bool))
                    return Convert.ToBoolean(value, inv);
                if (convertType == typeof(DateTime))
                    return Convert.ToDateTime(value, inv);
            }
            catch
            {
                // cae al ChangeType genérico
            }

            return Convert.ChangeType(value, convertType, inv);
        }
    }
}
