using System.Diagnostics;

namespace RouteLink.Generators.Parsing;

public static class TemplatePatternParser
{
    private const char Separator = '/';
    private const char OpenBrace = '{';
    private const char CloseBrace = '}';
    private const char QuestionMark = '?';
    private const string PeriodString = ".";

    public static TemplatePattern Parse(string pattern)
    {
        var trimmedPattern = TrimPrefix(pattern);

        var segments = new List<TemplatePart>();
        var errors = new List<string>();

        if (string.IsNullOrEmpty(trimmedPattern))
        {
            segments.Add(new TemplatePart("/"));
            return new TemplatePattern("/", segments, errors);
        }

        var context = new Context(trimmedPattern);
        while (context.MoveNext())
        {
            var i = context.Index;

            if (context.Current == Separator)
            {
                // If we get here is means that there's a consecutive '/' character.
                // Templates don't start with a '/' and parsing a segment consumes the separator.
                errors.Add("The route template separator character '/' cannot appear consecutively.");
                break;
            }

            if (!ParseSegment(context, segments))
            {
                var error = context.Error;
                if (!string.IsNullOrEmpty(error))
                    errors.Add(error!);

                break;
            }

            // A successful parse should always result in us being at the end or at a separator.
            Debug.Assert(context.AtEnd() || context.Current == Separator);

            if (context.Index <= i)
            {
                // This shouldn't happen, but we want to crash if it does.
                errors.Add("Infinite loop detected in the parser.");
                break;
            }
        }

        return new TemplatePattern(pattern, segments, errors);
    }

    private static bool ParseSegment(Context context, List<TemplatePart> segments)
    {
        if (context == null || segments == null)
            return false;

        while (true)
        {
            var i = context.Index;

            if (context.Current == OpenBrace)
            {
                if (!context.MoveNext())
                {
                    // This is a dangling open-brace, which is not allowed
                    context.Error = "There is an incomplete parameter in the route template. Check that each '{' character has a matching '}' character.";
                    return false;
                }

                if (context.Current == OpenBrace)
                {
                    // This is an 'escaped' brace in a literal, like "{{foo"
                    context.Back();
                    if (!ParseLiteral(context, segments))
                    {
                        return false;
                    }
                }
                else
                {
                    // This is a parameter
                    context.Back();
                    if (!ParseParameter(context, segments))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (!ParseLiteral(context, segments))
                {
                    return false;
                }
            }

            if (context.Current == Separator || context.AtEnd())
            {
                // We've reached the end of the segment
                break;
            }

            if (context.Index <= i)
            {
                // This shouldn't happen, but we want to crash if it does.
                context.Error = "Infinite loop detected in the parser. Please open an issue.";
                return false;
            }
        }

        return true;

    }

    private static bool ParseParameter(Context context, List<TemplatePart> parts)
    {
        Debug.Assert(context.Current == OpenBrace);
        context.Mark();

        context.MoveNext();

        while (true)
        {
            if (context.Current == OpenBrace)
            {
                // This is an open brace inside of a parameter, it has to be escaped
                if (context.MoveNext())
                {
                    if (context.Current != OpenBrace)
                    {
                        // If we see something like "{p1:regex(^\d{3", we will come here.
                        context.Error = "In a route parameter, '{' and '}' must be escaped with '{{' and '}}'.";
                        return false;
                    }
                }
                else
                {
                    // This is a dangling open-brace, which is not allowed
                    // Example: "{p1:regex(^\d{"
                    context.Error = "There is an incomplete parameter in the route template. Check that each '{' character has a matching '}' character.";
                    return false;
                }
            }
            else if (context.Current == CloseBrace)
            {
                // When we encounter Closed brace here, it either means end of the parameter or it is a closed
                // brace in the parameter, in that case it needs to be escaped.
                // Example: {p1:regex(([}}])\w+}. First pair is escaped one and last marks end of the parameter
                if (!context.MoveNext())
                {
                    // This is the end of the string -and we have a valid parameter
                    break;
                }

                if (context.Current == CloseBrace)
                {
                    // This is an 'escaped' brace in a parameter name
                }
                else
                {
                    // This is the end of the parameter
                    break;
                }
            }

            if (!context.MoveNext())
            {
                // This is a dangling open-brace, which is not allowed
                context.Error = "There is an incomplete parameter in the route template. Check that each '{' character has a matching '}' character.";
                return false;
            }
        }

        var text = context.Capture();
        if (text == null)
        {
            context.Error = "Invalid parameter text";
            return false;
        }

        if (text == "{}")
        {
            context.Error = "Route parameter names must be non-empty and cannot contain these characters: '{{', '}}', '/'. The '?' character marks a parameter as optional, and can occur only at the end of the parameter.";
            return false;
        }

        var inside = text.Substring(1, text.Length - 2);
        var decoded = inside.Replace("}}", "}").Replace("{{", "{");

        // At this point, we need to parse the raw name for inline constraint,
        // default values and optional parameters.
        var templatePart = TemplateParameterParser.ParseRouteParameter(decoded);

        // See #475 - this is here because InlineRouteParameterParser can't return errors
        if (decoded.StartsWith("*") && decoded.EndsWith("?"))
        {
            context.Error = "A catch-all parameter cannot be marked optional.";
            return false;
        }

        if (templatePart.IsOptional && templatePart.DefaultValue != null)
        {
            // Cannot be optional and have a default value.
            // The only way to declare an optional parameter is to have a ? at the end,
            // hence we cannot have both default value and optional parameter within the template.
            // A workaround is to add it as a separate entry in the defaults argument.
            context.Error = "An optional parameter cannot have default value.";
            return false;
        }

        parts.Add(templatePart);
        return true;
    }

    private static bool ParseLiteral(Context context, List<TemplatePart> parts)
    {
        context.Mark();

        while (true)
        {
            if (context.Current == Separator)
            {
                // End of the segment
                break;
            }
            else if (context.Current == OpenBrace)
            {
                if (!context.MoveNext())
                {
                    // This is a dangling open-brace, which is not allowed
                    context.Error = "There is an incomplete parameter in the route template. Check that each '{' character has a matching '}' character.";
                    return false;
                }

                if (context.Current == OpenBrace)
                {
                    // This is an 'escaped' brace in a literal, like "{{foo" - keep going.
                }
                else
                {
                    // We've just seen the start of a parameter, so back up.
                    context.Back();
                    break;
                }
            }
            else if (context.Current == CloseBrace)
            {
                if (!context.MoveNext())
                {
                    // This is a dangling close-brace, which is not allowed
                    context.Error = "There is an incomplete parameter in the route template. Check that each '{' character has a matching '}' character.";
                    return false;
                }

                if (context.Current == CloseBrace)
                {
                    // This is an 'escaped' brace in a literal, like "{{foo" - keep going.
                }
                else
                {
                    // This is an unbalanced close-brace, which is not allowed
                    context.Error = "There is an incomplete parameter in the route template. Check that each '{' character has a matching '}' character.";
                    return false;
                }
            }

            if (!context.MoveNext())
            {
                break;
            }
        }

        var encoded = context.Capture() ?? string.Empty;
        var decoded = encoded.Replace("}}", "}").Replace("{{", "{");

        var templatePart = new TemplatePart(decoded);
        parts.Add(templatePart);

        return true;
    }

    private static string TrimPrefix(string routePattern)
    {
        if (routePattern.StartsWith("~/", StringComparison.Ordinal))
        {
            return routePattern.Substring(2);
        }
        else if (routePattern.StartsWith("/", StringComparison.Ordinal))
        {
            return routePattern.Substring(1);
        }
        else if (routePattern.StartsWith("~", StringComparison.Ordinal))
        {
            return routePattern.Substring(1);
        }
        return routePattern;
    }

    private sealed class Context
    {
        private readonly string _template;
        private int _index;
        private int? _mark;

        public Context(string template)
        {
            _template = template;

            _index = -1;
        }

        public char Current
        {
            get { return (_index < _template.Length && _index >= 0) ? _template[_index] : (char)0; }
        }

        public int Index => _index;

        public string? Error { get; set; }

        public HashSet<string> ParameterNames { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public bool Back()
        {
            return --_index >= 0;
        }

        public bool AtEnd()
        {
            return _index >= _template.Length;
        }

        public bool MoveNext()
        {
            return ++_index < _template.Length;
        }

        public void Mark()
        {
            Debug.Assert(_index >= 0);

            // Index is always the index of the character *past* Current - we want to 'mark' Current.
            _mark = _index;
        }

        public string? Capture()
        {
            if (_mark.HasValue)
            {
                var value = _template.Substring(_mark.Value, _index - _mark.Value);
                _mark = null;
                return value;
            }
            else
            {
                return null;
            }
        }
    }
}
