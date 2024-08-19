using System.Diagnostics;
using System.Security.Principal;
using System.Text;
using Wist.Backend;
using Wist.Statistics.Logger;

namespace Wist.Preprocessor;

public class GccPreprocessor(ILogger logger) : IPreprocessor
{
    private const string SourceFile = "temp_source.c";
    private const string OutputFile = "temp_output.c";

    public string Preprocess(string input)
    {
        input = StdPreprocess(input);
        input = AddExtraSemicolonsToSaveStringsAfterMacroses(input);

        PrepareDataForGccPreprocessor(input);

        RunPreprocessor();

        var result = ReadPreprocessorResult();

        LogAndClear(result);

        return result;
    }

    private void LogAndClear(string result)
    {
        logger.Log(result);

        File.Delete(SourceFile);
        File.Delete(OutputFile);
    }

    private static string ReadPreprocessorResult()
    {
        var result = string.Join("\n", File.ReadAllLines(OutputFile).Where(x => x.Length == 0 || x[0] != '#'));
        return result;
    }

    private static void RunPreprocessor()
    {
        var process = MakeGccPreprocessorProcess();
        process.Start();
        process.WaitForExit();

        if (string.IsNullOrEmpty(File.ReadAllText(OutputFile)))
            throw new InvalidOperationException("GCC preprocessor failed");
    }

    private static Process MakeGccPreprocessorProcess()
    {
        var process = new Process();
        process.StartInfo.WorkingDirectory = Directory.GetCurrentDirectory();

        if (OS.IsWindows()) SetGccPreprocessorStartInfoForWindows(process);
        else if (OS.IsLinux()) SetGccPreprocessorStartInfoForLinux(process);
        else throw new InvalidOperationException("Unsupported OS for preprocessor");

        return process;
    }

    private static void SetGccPreprocessorStartInfoForLinux(Process process)
    {
        process.StartInfo.FileName = "/bin/bash";
        process.StartInfo.Arguments = $"-c \"gcc -nostdinc -x c -E {SourceFile} > {OutputFile}\"";
    }

    private static void SetGccPreprocessorStartInfoForWindows(Process process)
    {
#pragma warning disable CA1416 // Проверка совместимости платформы
        var userName = WindowsIdentity.GetCurrent().Name.Split(@"\")[1];
#pragma warning restore CA1416 // Проверка совместимости платформы

        var pathToGcc = $"""C:\Users\{userName}\gcc\bin\gcc.exe""";
        if (!File.Exists(pathToGcc))
            throw new InvalidOperationException(
                "Maybe gcc was not installed. You can follow this guide to download gcc the easiest way: https://programforyou.ru/poleznoe/kak-ustanovit-gcc-dlya-windows");


        process.StartInfo.FileName = "cmd.exe";
        process.StartInfo.Arguments = $"/c \"\"{pathToGcc}\" -nostdinc -x c -E {SourceFile} > {OutputFile}\"";
    }

    private static void PrepareDataForGccPreprocessor(string input)
    {
        File.WriteAllText(SourceFile, input);
        File.WriteAllText(OutputFile, "");
    }

    private static string AddExtraSemicolonsToSaveStringsAfterMacroses(string input)
    {
        var arr = input.Split('\n');
        for (var i = 0; i < arr.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(arr[i])) continue;
            if (arr[i][0] == '#') continue;
            if (arr[i].TrimEnd()[^1] == '\\') continue;

            arr[i] += ";";
        }

        input = string.Join("\n", arr);
        return input;
    }

    private static string StdPreprocess(string input)
    {
        input = new StdImportsPreprocessor().Preprocess(new StringBuilder(input), []);
        return input;
    }
}