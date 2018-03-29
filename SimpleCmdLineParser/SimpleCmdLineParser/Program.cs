using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimpleCmdLineParser
{
    class Program
    {
        static void Main(string[] args)
        {
            args = new[] {"--path", @"C:\Windows", "--test", "--enable", "--nouse", "should not be read"};
            try
            {
                var result = SimpleCmdLineParser.Parse<TestClass>(args);
                Console.WriteLine($"{result.Path}, {result.Enable}, {result.NoUse}");
            }
            catch (ParserException ex)
            {
                Console.Error.WriteLine($"操作失败: {ex.Message}");
            }
            Console.ReadLine();
        }

        public class TestClass
        {
            [Argument("--path")]
            public string Path { get; set; }

            [Argument()]
            public bool? Enable { get; set; }

            // 没有ArgumentAttribute的属性不予解析
            public string NoUse { get; set; }
        }
    }
}
