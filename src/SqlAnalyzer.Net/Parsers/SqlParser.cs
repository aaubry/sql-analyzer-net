using SqlAnalyzer.Net.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SqlAnalyzer.Net.Parsers
{
    internal enum VariableReferenceKind
    {
        Declaration,
        Access,
        Literal
    }

    internal static class SqlParser
    {
        private static readonly string[] StatementEndingTokens = [
            "declare",
            "select",
            "update",
            "delete",
            "insert",
            "merge",
            "if",
            "begin",
            "set",
            "with",
            "execute",
            ";",
        ];

        private static readonly Dictionary<string, string> Aliases = new(StringComparer.OrdinalIgnoreCase)
        {
            { "exec", "execute" },
        };

        private static IEnumerable<(string name, VariableReferenceKind kind)> Parse(IEnumerable<string> tokens)
        {
            using var en = tokens.GetEnumerator();
            while (en.MoveNext())
            {
                var current = en.Current;
                if (current.Equals("declare", StringComparison.OrdinalIgnoreCase))
                {
                    var endOfDeclarationFound = false;
                    do
                    {
                        if (!en.MoveNext())
                        {
                            yield break;
                        }

                        current = en.Current;

                        if (!current.StartsWith("@"))
                        {
                            break;
                        }

                        yield return (current.Substring(1), VariableReferenceKind.Declaration);

                        do
                        {
                            if (!en.MoveNext())
                            {
                                yield break;
                            }

                            current = en.Current;

                            if (StatementEndingTokens.Contains(current, StringComparer.OrdinalIgnoreCase))
                            {
                                endOfDeclarationFound = true;
                                break;
                            }

                            if (current == "=")
                            {
                                var parenthesisDepth = 0;
                                do
                                {
                                    if (!en.MoveNext())
                                    {
                                        yield break;
                                    }

                                    current = en.Current;

                                    if (parenthesisDepth == 0 && StatementEndingTokens.Contains(current, StringComparer.OrdinalIgnoreCase))
                                    {
                                        endOfDeclarationFound = true;
                                        break;
                                    }

                                    if (current == "(")
                                    {
                                        ++parenthesisDepth;
                                    }
                                    else if (current == ")" && parenthesisDepth > 0)
                                    {
                                        --parenthesisDepth;
                                    }
                                    else if (current.StartsWith("@") && !current.StartsWith("@@"))
                                    {
                                        yield return (current.Substring(1), VariableReferenceKind.Access);
                                    }

                                } while (parenthesisDepth > 0 || current != ",");
                            }

                        } while (current != "," && !endOfDeclarationFound);
                    } while (!endOfDeclarationFound);
                }
                else if (current.Equals("execute", StringComparison.OrdinalIgnoreCase))
                {
                    do
                    {
                        if (!en.MoveNext())
                        {
                            yield break;
                        }
                        current = en.Current;

                    process_current:
                        if (current.StartsWith("@") && !current.StartsWith("@@"))
                        {
                            var variable = current;
                            if (en.MoveNext())
                            {
                                current = en.Current;
                                if (current == "=") // Ignore named parameters in execute statements as they are not variables
                                {
                                    continue;
                                }
                                else
                                {
                                    yield return (variable.Substring(1), VariableReferenceKind.Access);
                                    goto process_current;
                                }
                            }

                            yield return (current.Substring(1), VariableReferenceKind.Access);
                        }
                        else if (StatementEndingTokens.Contains(current, StringComparer.OrdinalIgnoreCase))
                        {
                            break;
                        }
                    } while (true);
                }
                else if (current.StartsWith("@") && !current.StartsWith("@@"))
                {
                    yield return (current.Substring(1), VariableReferenceKind.Access);
                }
                else if (current.StartsWith("{=") && current.EndsWith("}"))
                {
                    yield return (current.Substring(2, current.Length - 3), VariableReferenceKind.Literal);
                }
            }
        }

        private static IEnumerable<string> Dealias(IEnumerable<string> tokens)
        {
            foreach (var token in tokens)
            {
                yield return Aliases.TryGetValue(token, out var alias) ? alias : token;
            }
        }

        private static IEnumerable<string> Tokenize(IEnumerable<char> code)
        {
            var token = new StringBuilder();
            using var en = code.GetEnumerator();
            while (en.MoveNext())
            {
                var current = en.Current;
                if (char.IsLetterOrDigit(current) || current == '@' || current == '_')
                {
                    token.Append(current);
                }
                else if (current == '\'')
                {
                    token.Append(current);
                    do
                    {
                        if (!en.MoveNext())
                        {
                            if (token.Length > 0)
                            {
                                yield return token.ToString();
                            }
                            yield break;
                        }
                        current = en.Current;
                        token.Append(current);
                    } while (current != '\'');
                    yield return token.ToString();
                    token.Length = 0;
                }
                else
                {
                    if (token.Length > 0)
                    {
                        yield return token.ToString();
                        token.Length = 0;
                    }

                    if (!char.IsWhiteSpace(current))
                    {
                        if (current == '{')
                        {
                            if (!en.MoveNext())
                            {
                                yield return "{";
                                yield break;
                            }

                            current = en.Current;
                            if (current != '=')
                            {
                                yield return "{";
                            }

                            token.Append("{=");
                            do
                            {
                                if (!en.MoveNext())
                                {
                                    yield return token.ToString();
                                    yield break;
                                }

                                current = en.Current;
                                if (!char.IsWhiteSpace(current))
                                {
                                    token.Append(current);
                                }
                            } while (current != '}');
                            yield return token.ToString();
                            token.Length = 0;
                        }
                        else
                        {
                            yield return current.ToString();
                        }
                    }
                }
            }

            if (token.Length > 0)
            {
                yield return token.ToString();
            }
        }

        private static IEnumerable<char> Preprocess(IEnumerable<char> code)
        {
            var previous = '\0';
            using var en = code.GetEnumerator();
            while (en.MoveNext())
            {
                var current = en.Current;
                if (current == '-' && previous == '-')
                {
                    do
                    {
                        if (!en.MoveNext())
                        {
                            if (previous != '\0')
                            {
                                yield return previous;
                            }
                            yield break;
                        }
                        current = en.Current;
                    } while (current != '\r' && current != '\n');
                    previous = current;
                    continue;
                }
                if (current == '*' && previous == '/')
                {
                    do
                    {
                        if (!en.MoveNext())
                        {
                            yield break;
                        }
                        previous = current;
                        current = en.Current;
                    } while (current != '/' || previous != '*');
                    if (!en.MoveNext())
                    {
                        yield break;
                    }
                    previous = en.Current;
                    continue;
                }

                if (previous != '\0')
                {
                    yield return previous;
                }
                previous = current;
            }

            if (previous != '\0')
            {
                yield return previous;
            }
        }


        public static ICollection<string> FindParameters(string sql, Orm orm = Orm.AdoNet)
        {
            var sqlVariables = new HashSet<string>();
            var declaredVariables = new HashSet<string>();

            var parsed = Parse(Dealias(Tokenize(Preprocess(sql))));
            foreach (var (name, kind) in parsed)
            {
                switch (kind)
                {
                    case VariableReferenceKind.Declaration:
                        declaredVariables.Add(name);
                        break;

                    case VariableReferenceKind.Access:
                        sqlVariables.Add(name);
                        break;

                    case VariableReferenceKind.Literal when orm == Orm.Dapper:
                        sqlVariables.Add(name);
                        break;
                }
            }

            sqlVariables.ExceptWith(declaredVariables);

            return sqlVariables;
        }
    }
}
