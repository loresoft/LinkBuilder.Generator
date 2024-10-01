using System.Buffers;

using BenchmarkDotNet.Running;

namespace RouteLink.Performance;

internal class Program
{
    static void Main(string[] args)
    {
        var summary = BenchmarkRunner.Run(typeof(Program).Assembly);
    }
}

public static partial class Routes
{
    public static partial class Client
    {
        public const string List = $"/clients";
        public const string Create = $"{List}/create";
        public const string Edit = $"{List}/{{id:int}}";
        public const string Facility = $"{Edit}/facility/{{facilityId:int}}";

        public static string EditLink(int id) => $"{List}/{id}";

        public static string FacilityLink(int clientId, int? facilityId)
        {
            string?[] segments = ["clients", clientId.ToString(), "facility", facilityId?.ToString()];
            var length = ComputeLength(segments);

            return string.Create(length, segments, CreateLink);
        }
    }
}

public static partial class Routes
{
    public const char PathSeparator = '/';

    private static int ComputeLength(string?[] segments)
    {
        var length = 0;
        foreach (var part in segments)
            length += part != null ? part.Length + 1 : 0;

        return length;
    }

    private static void CreateLink(Span<char> chars, string?[] segments)
    {
        var position = 0;

        for (var i = 0; i < segments.Length; i++)
        {
            if (segments[i] == null)
                continue;

            chars[position++] = PathSeparator;

            var span = segments[i].AsSpan();
            span.CopyTo(chars[position..]);

            position += span.Length;
        }
    }
}
