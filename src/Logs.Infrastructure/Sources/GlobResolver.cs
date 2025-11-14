using Logs.Core.Application.Abstractions.Sources;
using System.Text;
using System.Text.RegularExpressions;

namespace Logs.Infrastructure.Sources;

/// <summary>
/// Реализация разрешения glob-паттернов в список файлов
/// </summary>
public sealed class GlobResolver : IGlobResolver
{
    /// <summary>
    /// Разрешает glob-паттерн в список путей к файлам, поддерживает * и ** для рекурсивного поиска
    /// </summary>
    public IEnumerable<string> Resolve(string pattern)
    {
        if (IsUrl(pattern))
        {
            return [pattern];
        }

        var normalized = pattern.Replace('\\', '/');
        var rootDir = FindRootDirectory(normalized);
        var regex = GlobToRegex(normalized);
        var currentDirectory = Directory.GetCurrentDirectory();
        var rootFullPath = Path.GetFullPath(rootDir);

        return !Directory.Exists(rootDir)
            ? throw new FileNotFoundException($"Директория не найдена: {rootDir}")
            : Directory.EnumerateFiles(rootDir, "*", SearchOption.AllDirectories)
            .Where(p =>
            {
                var normalizedPath = p.Replace('\\', '/');

                if (Regex.IsMatch(normalizedPath, regex, RegexOptions.IgnoreCase))
                {
                    return true;
                }

                var relativeToCurrent = Path.GetRelativePath(currentDirectory, p).Replace('\\', '/');

                if (Regex.IsMatch(relativeToCurrent, regex, RegexOptions.IgnoreCase))
                {
                    return true;
                }

                var relativeToRoot = Path.GetRelativePath(rootFullPath, p).Replace('\\', '/');
                var combinedWithRoot = CombineNormalized(rootDir, relativeToRoot);

                if (Regex.IsMatch(combinedWithRoot, regex, RegexOptions.IgnoreCase))
                {
                    return true;
                }

                var relativeWithDot = $"./{relativeToCurrent}";
                return Regex.IsMatch(relativeWithDot, regex, RegexOptions.IgnoreCase);
            });
    }

    private static bool IsUrl(string s) => s.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
        || s.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

    private static string FindRootDirectory(string pattern)
    {
        int idx = pattern.IndexOfAny(['*', '?']);
        var root = idx >= 0
            ? Path.GetDirectoryName(pattern[..idx]) ?? "."
            : Path.GetDirectoryName(pattern) ?? ".";

        if (string.IsNullOrEmpty(root))
        {
            root = ".";
        }

        return root;
    }

    private static string CombineNormalized(string rootDir, string relativePath)
    {
        var normalizedRoot = rootDir.Replace('\\', '/');

        if (string.IsNullOrEmpty(relativePath) || relativePath == ".")
        {
            return normalizedRoot;
        }

        if (!normalizedRoot.EndsWith('/'))
        {
            normalizedRoot += '/';
        }

        return normalizedRoot + relativePath.Replace('\\', '/');
    }

    private static string GlobToRegex(string glob)
    {
        var sb = new StringBuilder();
        sb.Append('^');

        for (int i = 0; i < glob.Length; i++)
        {
            char c = glob[i];
            char next = i + 1 < glob.Length ? glob[i + 1] : '\0';

            if (c == '*' && next == '*')
            {
                sb.Append(".*");
                i++;
                continue;
            }

            string part = c switch
            {
                '*' => "[^/]*",
                '?' => ".",
                '.' => "\\.",
                '/' => "/",
                _ => "+()^$|{}[]".Contains(c) ? ("\\" + c) : c.ToString()
            };

            sb.Append(part);
        }

        sb.Append('$');
        return sb.ToString();
    }
}
