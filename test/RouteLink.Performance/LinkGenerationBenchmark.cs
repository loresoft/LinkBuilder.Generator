using System.Buffers;
using System.Text;

using BenchmarkDotNet.Attributes;

namespace RouteLink.Performance;

[MemoryDiagnoser]
[Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.FastestToSlowest)]
public class LinkGenerationBenchmark
{
    private int clientId = 1001;
    private int? facilityId = 2356;

    [Benchmark(Baseline = true)]
    public string LinkStringInterpolation()
    {
        return $"/clients/{clientId}/facility/{facilityId}";
    }

    [Benchmark]
    public string LinkListStringJoin()
    {
        var list = new List<string>(4);
        list.Add("clients");
        list.Add($"{clientId}");
        list.Add("facility");

        if (facilityId.HasValue)
            list.Add($"{facilityId}");

        return string.Join('/', list);
    }

    [Benchmark]
    public string LinkStringConcatenationEach()
    {
        var link = "/clients";
        link += $"/{clientId}";
        link += "/facility";

        if (facilityId.HasValue)
            link += $"/{facilityId}";

        return link;
    }

    [Benchmark]
    public string LinkStringConcatenationReduced()
    {
        var link = $"/clients/{clientId}/facility";
        if (facilityId.HasValue)
            link += $"/{facilityId}";

        return link;
    }

    [Benchmark]
    public string LinkStringBuilder()
    {
        var builder = new StringBuilder();
        builder.Append("/clients");
        builder.Append('/').Append(clientId);
        builder.Append("/facility");

        if (facilityId.HasValue)
            builder.Append('/').Append(facilityId);

        return builder.ToString();
    }

    [Benchmark]
    public string LinkStringBuilderCache()
    {
        var builder = StringBuilderCache.Acquire();
        builder.Append("/clients");
        builder.Append('/').Append(clientId);
        builder.Append("/facility");

        if (facilityId.HasValue)
            builder.Append('/').Append(facilityId);

        return StringBuilderCache.ToString(builder);
    }

    [Benchmark]
    public string LinkValueStringBuilder()
    {
        var builder = new ValueStringBuilder();
        builder.Append("/clients");
        builder.Append('/');
        builder.Append(clientId.ToString());
        builder.Append("/facility");

        if (facilityId.HasValue)
        {
            builder.Append('/');
            builder.Append(facilityId.ToString());
        }

        return builder.ToString();
    }

    [Benchmark]
    public string LinkStringCreate()
    {
        string?[] segments = ["clients", clientId.ToString(), "facility", facilityId?.ToString()];
        var length = ComputeLength(segments);
        return string.Create(length, segments, CreateLink);
    }


    public const char PathSeparator = '/';

    private static int ComputeLength(string?[] segments)
    {
        var length = 0;
        for (int i = 0; i < segments.Length; i++)
        {
            var segment = segments[i];
            length += segment != null ? segment.Length + 1 : 0;
        }

        return length;
    }

    private static void CreateLink(Span<char> buffer, string?[] segments)
    {
        var position = 0;

        for (var i = 0; i < segments.Length; i++)
        {
            if (segments[i] == null)
                continue;

            buffer[position++] = PathSeparator;

            var span = segments[i].AsSpan();
            span.CopyTo(buffer[position..]);

            position += span.Length;
        }
    }
}
