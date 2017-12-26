using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace SimpleCmdLineParser
{

    public class ArgumentAttribute : Attribute
    {
        public ArgumentAttribute()
        {
        }

        public ArgumentAttribute(string tagName) : this()
        {
            TagName = tagName;
        }

        public string TagName { get; set; }
    }

    public class ParserException : Exception
    {
        public ParserException(string message) : base(message)
        {
        }
    }

    public class SimpleCmdLineParser
    {
        public static T Parse<T>(string[] args)
        {
            var resultType = typeof(T);
            var mapper = new Dictionary<string, PropertyInfo>();
            foreach (var p in resultType.GetProperties())
            {
                var argAttr = p.GetCustomAttributes(typeof(ArgumentAttribute), true).FirstOrDefault() as ArgumentAttribute;
                if (argAttr == null)
                    continue;

                // 未指定TagName，默认使用"--{PropertyName}"作为TagName
                string tagName = string.IsNullOrEmpty(argAttr.TagName) ? "--" + p.Name : argAttr.TagName;
                mapper.Add(tagName.ToLowerInvariant(), p);
            }
            var result = Activator.CreateInstance<T>();
            int index = 0;
            while (index < args.Length)
            {
                string tmp = args[index];
                if (mapper.TryGetValue(tmp.Trim().ToLowerInvariant(), out var prop))
                {
                    // 布尔参数不需要读取值
                    if (prop.PropertyType == typeof(bool)
                        || prop.PropertyType == typeof(bool?))
                    {
                        prop.SetValue(result, true, null);
                    }
                    else
                    {
                        if (++index >= args.Length)
                            throw new ParserException($"缺少参数值: 参数名={tmp}");
                        try
                        {
                            object value = ConvertData(args[index], prop.PropertyType);
                            prop.SetValue(result, value, null);
                        }
                        catch (Exception)
                        {
                            throw new ParserException($"参数值转换错误: 参数名={tmp}");
                        }
                    }
                }
                index++;
            }
            return result;
        }

        private static object ConvertData(object value, Type targetType)
        {
            var underType = Nullable.GetUnderlyingType(targetType);
            if (underType != null && value == null)
            {
                return null;
            }
            var typeCode = Type.GetTypeCode(underType ?? targetType);

            switch (typeCode)
            {
                case TypeCode.Int16:
                    return Convert.ToInt16(value);
                case TypeCode.Int32:
                    return Convert.ToInt32(value);
                case TypeCode.Int64:
                    return Convert.ToInt64(value);
                case TypeCode.Byte:
                    return Convert.ToByte(value);
                case TypeCode.UInt16:
                    return Convert.ToUInt16(value);
                case TypeCode.UInt32:
                    return Convert.ToUInt32(value);
                case TypeCode.UInt64:
                    return Convert.ToUInt64(value);
                case TypeCode.SByte:
                    return Convert.ToSByte(value);
                case TypeCode.Single:
                    return Convert.ToSingle(value);
                case TypeCode.Double:
                    return Convert.ToDouble(value);
                case TypeCode.Decimal:
                    return Convert.ToDecimal(value);
                case TypeCode.Boolean:
                    return Convert.ToBoolean(value);
                case TypeCode.DateTime:
                    return Convert.ToDateTime(value);
                case TypeCode.String:
                    return Convert.ToString(value);
                case TypeCode.Char:
                    return Convert.ToChar(value);
            }
            return value;
        }
    }

}
