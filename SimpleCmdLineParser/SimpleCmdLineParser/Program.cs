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
            args = new[] {
                "--help",
                "--path",
                @"C:\Windows",
                "--test",
                "--enable",
                "--nouse",
                "should not be read",
                "-i",
                "123",
            };
            try
            {
                var result = SimpleCmdLineParser.Parse<TestClass>(args);
                Console.WriteLine($"{result.Path}, {result.Enable}, {result.NoUse}");

                if (result.ShowHelp)
                    Console.WriteLine(SimpleCmdLineParser.GetHelpText(typeof(TestClass)));
            }
            catch (ParserException ex)
            {
                Console.Error.WriteLine($"操作失败: {ex.Message}");
            }
            Console.ReadLine();
        }

        [ArgumentType("This program is used to do something with computer.\n" +
            "Here is the description of the arguments")]
        public class TestClass
        {
            [ShowHelp()]
            public bool ShowHelp { get; set; }

            [Argument("--path|-p", HelpText = "Input directory path.")]
            public string Path { get; set; }

            [Argument(HelpText = "Enable some feature.\nIf enable then some thing will happen.\nOtherwise, no change\n")]
            public bool? Enable { get; set; }

            [Argument("-i|--index", HelpText = "The index number.")]
            public int Index { get; set; }

            // 没有ArgumentAttribute的属性不予解析
            public string NoUse { get; set; }
        }
    }
}
