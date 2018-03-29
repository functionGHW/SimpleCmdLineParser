# SimpleCmdLineParser

命令行参数解析到对象属性

+ 使用Attribute实现
+ .Net >= 3.5
+ 支持字符串到简单类型(如int，short)的自动转换(取决于目标属性的类型)

## 使用示例
复制SimpleCmdLineParser.cs文件，添加到你的项目里。

```csharp
using SimpleCmdLineParser;

public class MyArguments
{
    [Argument]
    public string FilePath { get; set; }
    
    [Argument("-s")]
    public bool Silent { get; set; }
    
    [Argument("--number", Optional = true)]
    public int? Number { get; set; }
    
    public string OtherProperty1 { get; set; }
    
    public string OtherProperty2 { get; set; }
}


//  C:\>myapp.exe --Number 255 --filepath "C:\Windows\explorer.exe" -s
static void Main(string[] args)
{
    try
    {
        var results = SimpleCmdLineParser.Parse<MyArguments>(args);
        Console.WriteLine(result.FilePath)
    }
    catch (ParseException ex)
    {
        Console.Error.WriteLine($"操作失败: {ex.Message}");
    }
}
```
