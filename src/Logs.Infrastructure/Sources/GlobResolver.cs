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

        if (pattern.IndexOfAny(['*', '?']) < 0)
        {
            return [pattern];
        }

        var normalized = pattern.Replace('\\', '/');
        var rootDir = FindRootDirectory(normalized);

        if (!Directory.Exists(rootDir))
        {
            throw new FileNotFoundException($"Директория не найдена: {rootDir}");
        }

        var regex = GlobToRegex(normalized);

        return Directory.EnumerateFiles(rootDir, "*", SearchOption.AllDirectories)
            .Select(p => p.Replace('\\', '/'))
            .Where(p => Regex.IsMatch(p, regex, RegexOptions.IgnoreCase));
    }

    private static bool IsUrl(string s) => s.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
        || s.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

    private static string FindRootDirectory(string pattern)
    {
        int idx = pattern.IndexOfAny(['*', '?']);
        var root = idx >= 0
            ? Path.GetDirectoryName(pattern[..idx]) ?? "/"
            : Path.GetDirectoryName(pattern) ?? ".";

        if (string.IsNullOrEmpty(root))
        {
            root = ".";
        }

        return root;
    }

    private static string GlobToRegex(string glob)
    {
        var sb = new StringBuilder();
        sb.Append('^');

        for (int i = 0; i < glob.Length; i++)
        {
            char c = glob[i];

            if (c == '*')
            {
                bool isDoubleStar = i + 1 < glob.Length && glob[i + 1] == '*';

                if (isDoubleStar && i + 2 < glob.Length && glob[i + 2] == '/')
                {
                    sb.Append("(?:[^/]*/)*");
                    i += 2;
                    continue;
                }

                if (isDoubleStar)
                {
                    sb.Append(".*");
                    i++;
                    continue;
                }

                sb.Append("[^/]*");
                continue;
            }

            string part = c switch
            {
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
