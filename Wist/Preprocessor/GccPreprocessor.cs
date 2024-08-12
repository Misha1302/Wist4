using System.Diagnostics;
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
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.Arguments = $"/c \"gcc -nostdinc -x c -E {sourceFile} > {outputFile}\"";
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