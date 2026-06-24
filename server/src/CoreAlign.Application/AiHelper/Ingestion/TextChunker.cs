using System;
using System.Collections.Generic;
using System.Text;

namespace CoreAlign.Application.AiHelper.Ingestion;

public sealed record ContextualChunk(string HeadingPath, string Body);

public static class TextChunker
{
    private const string HeadingSeparator = " › ";

    public static IReadOnlyList<string> Chunk(string content, int maxChars = 1000, int overlapChars = 150)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return Array.Empty<string>();
        }

        var normalized = content.Replace("\r\n", "\n").Replace('\r', '\n');
        var paragraphs = normalized.Split("\n\n", StringSplitOptions.RemoveEmptyEntries);

        var pieces = new List<string>();
        foreach (var raw in paragraphs)
        {
            var paragraph = raw.Trim();
            if (paragraph.Length == 0)
            {
                continue;
            }

            if (paragraph.Length <= maxChars)
            {
                pieces.Add(paragraph);
                continue;
            }

            for (var i = 0; i < paragraph.Length; i += maxChars)
            {
                pieces.Add(paragraph.Substring(i, Math.Min(maxChars, paragraph.Length - i)));
            }
        }

        var chunks = new List<string>();
        var current = new StringBuilder();
        foreach (var piece in pieces)
        {
            if (current.Length > 0 && current.Length + piece.Length + 2 > maxChars)
            {
                chunks.Add(current.ToString().Trim());
                var tail = current.ToString();
                current.Clear();
                if (overlapChars > 0 && tail.Length > overlapChars)
                {
                    current.Append(tail[^overlapChars..]).Append("\n\n");
                }
            }

            if (current.Length > 0)
            {
                current.Append("\n\n");
            }

            current.Append(piece);
        }

        if (current.Length > 0)
        {
            var last = current.ToString().Trim();
            if (last.Length > 0)
            {
                chunks.Add(last);
            }
        }

        return chunks;
    }

    public static IReadOnlyList<ContextualChunk> ChunkWithContext(string content, int maxChars = 1000, int overlapChars = 150)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return Array.Empty<ContextualChunk>();
        }

        var normalized = content.Replace("\r\n", "\n").Replace('\r', '\n');
        var lines = normalized.Split('\n');

        var trailLevels = new List<int>();
        var trailTexts = new List<string>();
        var section = new StringBuilder();
        var result = new List<ContextualChunk>();

        void FlushSection()
        {
            var body = section.ToString().Trim();
            section.Clear();
            if (body.Length == 0)
            {
                return;
            }

            var headingPath = string.Join(HeadingSeparator, trailTexts);
            foreach (var piece in Chunk(body, maxChars, overlapChars))
            {
                result.Add(new ContextualChunk(headingPath, piece));
            }
        }

        foreach (var line in lines)
        {
            if (TryParseHeading(line, out var level, out var heading))
            {
                FlushSection();
                while (trailLevels.Count > 0 && trailLevels[^1] >= level)
                {
                    trailLevels.RemoveAt(trailLevels.Count - 1);
                    trailTexts.RemoveAt(trailTexts.Count - 1);
                }

                trailLevels.Add(level);
                trailTexts.Add(heading);
                continue;
            }

            section.Append(line).Append('\n');
        }

        FlushSection();
        return result;
    }

    private static bool TryParseHeading(string line, out int level, out string text)
    {
        level = 0;
        text = string.Empty;

        var trimmed = line.TrimStart();
        var hashes = 0;
        while (hashes < trimmed.Length && trimmed[hashes] == '#')
        {
            hashes++;
        }

        if (hashes == 0 || hashes > 6 || hashes >= trimmed.Length || trimmed[hashes] != ' ')
        {
            return false;
        }

        var heading = trimmed[(hashes + 1)..].Trim();
        if (heading.Length == 0)
        {
            return false;
        }

        level = hashes;
        text = heading;
        return true;
    }
}
