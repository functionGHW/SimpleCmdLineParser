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
            args = new[] {"--path", @"C:\\Windows", "--enable", "-b"};

            var result = SimpleCmdLineParser.Parse<TestClass>(args);

            Console.WriteLine($"{result.Path}, {result.Enable}");

            Console.ReadLine();
        }

        public class TestClass
        {
            [Argument("--path")]
            public string Path { get; set; }

            [Argument()]
            public bool? Enable { get; set; }
        }
    }
}
