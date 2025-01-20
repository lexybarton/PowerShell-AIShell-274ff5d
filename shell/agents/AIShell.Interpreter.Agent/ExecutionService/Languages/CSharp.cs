internal class CSharp : AIShell.Interpreter.Agent.SubprocessLanguage
{
    internal CSharp() : base()
    {
        StartCmd = new[] { "dotnet", "run", "--project", "YourCSharpProject.csproj" };
        VersionCmd = new[] { "dotnet", "--version" };
    }

    protected override string PreprocessCode(string code)
    {
        return $@"
using System;
using System.Threading.Tasks;

public class Program
{{
    public static async Task Main()
    {{
        try
        {{
            {code}
        }}
        catch (Exception ex)
        {{
            Console.Error.WriteLine(ex.ToString());
        }}
        finally
        {{
            Console.WriteLine(""##end_of_execution##"");
        }}
    }}
}}";
    }
}