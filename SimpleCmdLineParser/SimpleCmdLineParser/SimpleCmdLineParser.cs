using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace SimpleCmdLineParser
{
    /// <summary>
    /// 参数设置特性
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class ArgumentAttribute : Attribute
    {
        public ArgumentAttribute()
        {
        }

        public ArgumentAttribute(string tagName) : this()
        {
            TagName = tagName;
        }

        /// <summary>
        /// 参数标签名
        /// </summary>
        public string TagName { get; set; }

        /// <summary>
        /// 表示该参数是可选的
        /// </summary>
        public bool Optional { get; set; }

        /// <summary>
        /// 参数帮助说明文本
        /// </summary>
        public string HelpText { get; set; }
    }

    /// <summary>
    /// 参数类型标识
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ArgumentTypeAttribute : Attribute
    {
        public ArgumentTypeAttribute(string helpText)
        {
            HelpText = helpText;
        }

        /// <summary>
        /// 帮助文本
        /// </summary>
        public string HelpText { get; set; }
    }

    /// <summary>
    /// 参数解析异常类，包含出错信息
    /// </summary>
    public class ParserException : Exception
    {
        public ParserException(string message) : base(message)
        {
        }
    }


    /// <summary>
    /// 参数解析类
    /// </summary>
    public class SimpleCmdLineParser
    {
        /// <summary>
        /// 版本号标识
        /// </summary>
        public const string Version = "1.0.0.0";

        /// <summary>
        /// 默认的参数帮助文本
        /// </summary>
        public static string DefaultHelpText { get; set; } = "No description.";

        public static string OptionalHelpText { get; set; } = "Optional";

        private class ArgumentInfo
        {
            public string TagName { get; set; }

            public bool Optional { get; set; }

            public PropertyInfo Property { get; set; }

            public string HelpText { get; set; }

            // 参数是否被设置，用于检查必要参数是否缺失
            public bool IsSet { get; set; }
        }

        /// <summary>
        /// 获取格式化的帮助信息文本，包含参数的解释说明
        /// </summary>
        /// <param name="argumentType"></param>
        /// <returns></returns>
        public static string GetHelpText(Type argumentType)
        {
            var builder = new StringBuilder();
            var typeAttr = argumentType.GetCustomAttributes(typeof(ArgumentTypeAttribute), true)
                .FirstOrDefault() as ArgumentTypeAttribute;
            string helpText = typeAttr?.HelpText ?? DefaultHelpText;
            builder.AppendLine(helpText);

            var mapper = BuildArgumentInfoMapper(argumentType);
            var argumentInfos = mapper.Values.ToArray();
            if (argumentInfos.Length == 0)
                return builder.ToString();

            const int tabSize = 2;
            const char spaceChar = ' ';
            int maxLength = tabSize + argumentInfos.Select(x => x.TagName.Length).Max();
            string multipleLineTextPrefix = new string(spaceChar, tabSize + maxLength);

            builder.AppendLine("\nUsage:");
            foreach (var arg in argumentInfos)
            {
                string argHelpText = BuildHelpText(multipleLineTextPrefix, arg);
                builder.Append(spaceChar, tabSize);
                builder.AppendLine($"{arg.TagName.PadRight(maxLength)}{argHelpText}");
            }
            return builder.ToString();
        }

        private static string BuildHelpText(string multipleLineTextPrefix, ArgumentInfo arg)
        {
            string helpText = arg.Optional ? $"({OptionalHelpText}){arg.HelpText}" : arg.HelpText;
            var textLines = helpText.Split('\n');
            for (var i = 1; i < textLines.Length; i++)
            {
                textLines[i] = multipleLineTextPrefix + textLines[i];
            }
            helpText = string.Join("\n", textLines);
            return helpText;
        }

        /// <summary>
        /// 解析命令行参数
        /// </summary>
        /// <typeparam name="T">参数定义类型</typeparam>
        /// <param name="args"></param>
        /// <returns></returns>
        /// <exception cref="ParserException">解析参数过程出错，Message包含错误说明</exception>
        public static T Parse<T>(string[] args)
        {
            if (args == null)
                throw new ArgumentNullException(nameof(args));

            var result = Activator.CreateInstance<T>();
            return (T)Parse(args, result, typeof(T));
        }

        /// <summary>
        /// 解析命令行参数到指定的model对象中
        /// </summary>
        /// <typeparam name="T">参数定义类型</typeparam>
        /// <param name="args"></param>
        /// <param name="result"></param>
        /// <param name="resultType"></param>
        /// <returns></returns>
        /// <exception cref="ParserException">解析参数过程出错，Message包含错误说明</exception>
        public static object ParseToObject(string[] args, object result, Type resultType = null)
        {
            if (args == null)
                throw new ArgumentNullException(nameof(args));
            if (result == null)
                throw new ArgumentNullException(nameof(result));

            return Parse(args, result, resultType ?? result.GetType());
        }

        private static object Parse(string[] args, object result, Type resultType)
        {
            var mapper = BuildArgumentInfoMapper(resultType);
            int index = 0;
            while (index < args.Length)
            {
                string tmp = args[index];
                if (mapper.TryGetValue(tmp.Trim().ToLowerInvariant(), out var argInfo))
                {
                    if (argInfo.IsSet)
                        throw new ParserException($"指定了重复参数：参数名={tmp}");

                    var prop = argInfo.Property;
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
                    argInfo.IsSet = true;
                }
                index++;
            }
            foreach (var item in mapper.Values)
            {
                if (!item.Optional && !item.IsSet)
                    throw new ParserException($"缺少必须的参数：参数名={item.TagName}");
            }

            return result;
        }

        private static Dictionary<string, ArgumentInfo> BuildArgumentInfoMapper(Type resultType)
        {
            var mapper = new Dictionary<string, ArgumentInfo>();
            foreach (var p in resultType.GetProperties())
            {
                var argAttr = p.GetCustomAttributes(typeof(ArgumentAttribute), true)
                    .FirstOrDefault() as ArgumentAttribute;
                if (argAttr == null)
                    continue;

                // 未指定TagName，默认使用"--{PropertyName}"作为TagName
                string tagName = string.IsNullOrEmpty(argAttr.TagName) ? "--" + p.Name : argAttr.TagName;
                tagName = tagName.ToLowerInvariant();
                var argInfo = new ArgumentInfo()
                {
                    TagName = tagName,
                    Optional = argAttr.Optional,
                    Property = p,
                    HelpText = argAttr.HelpText ?? DefaultHelpText,
                };
                mapper.Add(tagName, argInfo);
            }

            return mapper;
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
