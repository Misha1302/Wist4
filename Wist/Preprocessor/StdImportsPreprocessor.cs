using System.Text;

namespace Wist.Preprocessor;

public class StdImportsPreprocessor
{
    public string Preprocess(string input, HashSet<string> imported)
    {
        var output = new StringBuilder();

        for (var i = 0; i < input.Length; i++)
            if (input[i..].StartsWith("import"))
            {
                var importStartIndex = i;

                while (input[i] != '"')
                    i++;

                var startIndex = i + 1;

                do
                {
                    i++;
                } while (input[i] != '"');

                var endIndex = i;

                var path = input[startIndex..endIndex];
                if (path.EndsWith(".dll")) continue;
                if (!imported.Add(path)) continue;

                input = input.Remove(importStartIndex, endIndex - importStartIndex + 1);
                i = importStartIndex;

                //if (Directory.Exists(path))
                if (File.Exists(path))
                    AppendFile(imported, output, path);
                else if (Directory.Exists(path))
                    foreach (var file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
                        AppendFile(imported, output, file);
                else throw new InvalidOperationException();
            }

        output.AppendLine(input);

        return output.ToString();
    }

    private void AppendFile(HashSet<string> imported, StringBuilder output, string path)
    {
        imported.Add(path);
        output.AppendLine(Preprocess(File.ReadAllText(path), imported));
    }
}