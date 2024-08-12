using System.Diagnostics;
using System.Security.Principal;
using Wist.Backend;
using Wist.Statistics.Logger;

namespace Wist.Preprocessor;

public class GccPreprocessor(ILogger logger) : IPreprocessor
{
    public string Preprocess(string input)
    {
        const string sourceFile = "temp_source.c";
        const string outputFile = "temp_output.c";

        File.WriteAllText(sourceFile, input);
        File.WriteAllText(outputFile, "");


        var process = new Process();
        process.StartInfo.WorkingDirectory = Directory.GetCurrentDirectory();

        if (OS.IsWindows())
        {
#pragma warning disable CA1416 // Проверка совместимости платформы
            var userName = WindowsIdentity.GetCurrent().Name.Split(@"\")[1];
#pragma warning restore CA1416 // Проверка совместимости платформы

            var pathToGcc = $"""C:\Users\{userName}\gcc\bin\gcc.exe""";
            if (!File.Exists(pathToGcc))
            {
                throw new InvalidOperationException("Maybe gcc was not installed. You can follow this guide to download gcc the easiest way: https://programforyou.ru/poleznoe/kak-ustanovit-gcc-dlya-windows");
            }


            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.Arguments = $"/c \"\"{pathToGcc}\" -nostdinc -x c -E {sourceFile} > {outputFile}\"";
        }
        else if (OS.IsLinux())
        {
            process.StartInfo.FileName = "/bin/bash";
            process.StartInfo.Arguments = $"-c \"gcc -nostdinc -x c -E {sourceFile} > {outputFile}\"";
        }
        else
        {
            throw new InvalidOperationException("Unsupported OS for preprocessor");
        }


        process.Start();
        process.WaitForExit();

        var result = string.Join("\n", File.ReadAllLines(outputFile).Where(x => x.Length == 0 || x[0] != '#'));

        logger.Log(result);

        File.Delete(sourceFile);
        File.Delete(outputFile);

        return result;
    }
}