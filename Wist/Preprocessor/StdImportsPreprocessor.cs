using System.Text;

namespace Wist.Preprocessor;

public class StdImportsPreprocessor
{
    public string Preprocess(StringBuilder input, HashSet<string> imported)
    {
        for (var i = 0; i < input.Length; i++)
        {
            var s = input.ToString();
            if (!s[i..].StartsWith("import")) continue;

            var isImportMany = s[i..].StartsWith("import many");

            var importStartIndex = i;

            while (input[i] != '"')
                i++;

            var startIndex = i + 1;

            do i++;
            while (input[i] != '"');

            var endIndex = i;

            var defines = new List<(string key, string value)>();
            while (input[i] is not '\n' and not 'w')
                i++;
            if (input[i] == 'w')
                i += 4; // [w](ith ) - 4
            while (input[i] is not '\n')
            {
                var startDefineIndex = i;
                while (input[i] is not '=') i++;
                var endDefineIndex = i;
                i++;
                var defineStr = input.ToString()[startDefineIndex..endDefineIndex];

                var startValueIndex = i;
                while (input[i] is not ' ' and not '\n') i++;
                var endValueIndex = i;
                var valueStr = input.ToString()[startValueIndex..endValueIndex];

                defines.Add((defineStr, valueStr));
            }

            var path = s[startIndex..endIndex];
            if (path.EndsWith(".dll")) continue;
            if (!isImportMany && !imported.Add(path)) continue;

            input = input.Remove(importStartIndex, i - importStartIndex + 1);
            i = importStartIndex;

            if (File.Exists(path))
                input.Insert(i, AppendFile(imported, path, defines));
            else if (Directory.Exists(path))
                foreach (var file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
                    input.Insert(i, AppendFile(imported, file, defines));
            else throw new InvalidOperationException();
        }

        return input.ToString();
    }

    private string AppendFile(HashSet<string> imported, string path, List<(string key, string value)> defines)
    {
        imported.Add(path);
        var definesStr = string.Join("\n", defines.Select(x => $"#define {x.key} {x.value}")) + "\n";
        var undefinesStr = "\n" + string.Join("\n", defines.Select(x => $"#undef {x.key}"));
        return Preprocess(new StringBuilder(definesStr + File.ReadAllText(path) + undefinesStr), imported) + "\n";
    }
}