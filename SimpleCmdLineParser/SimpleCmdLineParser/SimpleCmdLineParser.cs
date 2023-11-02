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

    public class ShowHelpAttribute : ArgumentAttribute
    {
        public ShowHelpAttribute() : this("--help|-h")
        {
        }
        public ShowHelpAttribute(string tagName) : base(tagName)
        {
            Optional = true;
            HelpText = "Show help info.";
        }
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

        private class ArgumentInfo
        {
            //public string TagName { get; set; }

            public string ShortTagName { get; set; }
            public string FullTagName { get; set; }

            public bool Optional { get; set; }

            public PropertyInfo Property { get; set; }

            public string HelpText { get; set; }

            public bool IsShowHelp { get; set; }

            // 参数是否被设置，用于检查必要参数是否缺失以及重复传参
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

            var argumentInfos = BuildArgumentInfoList(argumentType);
            if (argumentInfos.Length == 0)
                return builder.ToString();

            const int tabSize = 2;
            const char spaceChar = ' ';
            int maxFullTagLength = argumentInfos.Select(x => x.FullTagName.Length).Max();
            int maxShortTagLength = argumentInfos.Select(x => x.ShortTagName.Length).Max();

            int helpTextPaddingSize = 3 * tabSize + maxFullTagLength + maxShortTagLength;
            string multipleLineTextPrefix = new string(spaceChar, helpTextPaddingSize);

            builder.AppendLine("\nUsage:");
            foreach (var arg in argumentInfos)
            {
                string argHelpText = BuildHelpText(multipleLineTextPrefix, arg);
                builder.Append(spaceChar, tabSize);
                builder.Append(arg.FullTagName.PadRight(maxFullTagLength));
                builder.Append(spaceChar, tabSize);
                builder.Append(arg.ShortTagName.PadRight(maxShortTagLength));
                builder.Append(spaceChar, tabSize);
                builder.AppendLine(argHelpText);
            }
            return builder.ToString();
        }

        private static string BuildHelpText(string multipleLineTextPrefix, ArgumentInfo arg)
        {
            string helpText = arg.HelpText;
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
            var argList = BuildArgumentInfoList(resultType);
            int index = 0;
            while (index < args.Length)
            {
                string tmp = args[index].Trim().ToLowerInvariant();
                var argInfo = argList.FirstOrDefault(x => x.FullTagName == tmp || x.ShortTagName == tmp);
                if (argInfo != null)
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
            bool isShowHelp = argList.Any(x => x.IsShowHelp && x.IsSet);
            if (isShowHelp)
                return result;

            foreach (var item in argList)
            {
                if (!item.Optional && !item.IsSet)
                {
                    string argName = string.IsNullOrEmpty(item.FullTagName) ? item.ShortTagName : item.FullTagName;
                    throw new ParserException($"缺少必须的参数：参数名={argName}");
                }
            }
            return result;
        }

        private static void ParseTagName(string tagName, out string shortTagName, out string fullTagName)
        {
            var tags = tagName.Split("|".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            if (tags.Length > 2)
                throw new ParserException($"参数定义错误：无法解析的格式\"{tagName}\"");

            shortTagName = "";
            fullTagName = "";

            for (int i = 0; i < tags.Length; i++)
            {
                string tag = tags[i].Trim();
                if (tag.StartsWith("--"))
                {
                    fullTagName = tag;
                }
                else if (tag.StartsWith("-"))
                {
                    shortTagName = tag;
                }
                else
                {
                    throw new ParserException($"参数定义错误：无法解析的格式\"{tagName}\"");
                }
            }

        }

        private static ArgumentInfo[] BuildArgumentInfoList(Type resultType)
        {
            var result = new List<ArgumentInfo>();
            foreach (var p in resultType.GetProperties())
            {
                var argAttr = p.GetCustomAttributes(typeof(ArgumentAttribute), true)
                    .FirstOrDefault() as ArgumentAttribute;
                if (argAttr == null)
                    continue;

                // 未指定TagName，默认使用"--{PropertyName}"作为TagName
                string tagName = string.IsNullOrEmpty(argAttr.TagName) ? $"--{p.Name}" : argAttr.TagName;
                tagName = tagName.ToLowerInvariant();
                ParseTagName(tagName, out string shortTag, out string fullTag);
                var argInfo = new ArgumentInfo()
                {
                    FullTagName = fullTag,
                    ShortTagName = shortTag,
                    Optional = argAttr.Optional,
                    Property = p,
                    IsShowHelp = argAttr is ShowHelpAttribute,
                    HelpText = argAttr.HelpText ?? DefaultHelpText,
                };
                result.Add(argInfo);
            }

            return result.ToArray();
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
