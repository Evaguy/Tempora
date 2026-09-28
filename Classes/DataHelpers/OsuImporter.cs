// Copyright 2024 https://github.com/kongehund
// 
// This file is licensed under the Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International (CC BY-NC-ND 4.0).

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using Godot;
using Tempora.Classes.TimingClasses;
using Tempora.Classes.Utility;

namespace Tempora.Classes.DataHelpers;

/// <summary>
/// Parses timing data from an existing .osu or .osz file and loads it into <see cref="Timing"/>.
/// Only uninherited (red) timing points are imported into the timeline.
/// Inherited (green) lines are preserved verbatim in <see cref="Project.ImportedGreenLines"/>
/// so they are round-tripped back into the export without loss.
/// </summary>
public static class OsuImporter
{
    //Public entry points
    /// <summary>Import timing from a .osu file on disk.</summary>
    public static bool TryImportFromOsuFile(string path, out string error)
    {
        error = "";
        try
        {
            string text = File.ReadAllText(path);
            return TryImportFromOsuText(text, out error);
        }
        catch (Exception ex)
        {
            error = $"Could not read .osu file: {ex.Message}";
            return false;
        }
    }

    /// <summary>Import timing from a .osz archive (first .osu entry wins).</summary>
    public static bool TryImportFromOszFile(string path, out string error)
    {
        error = "";
        try
        {
            using var archive = ZipFile.OpenRead(path);
            foreach (var entry in archive.Entries)
            {
                if (!entry.Name.EndsWith(".osu", StringComparison.OrdinalIgnoreCase))
                    continue;

                using var stream = entry.Open();
                using var reader = new StreamReader(stream);
                string text = reader.ReadToEnd();
                return TryImportFromOsuText(text, out error);
            }
            error = "No .osu file found inside the .osz archive.";
            return false;
        }
        catch (Exception ex)
        {
            error = $"Could not read .osz file: {ex.Message}";
            return false;
        }
    }

    // Parsing 
    private static bool TryImportFromOsuText(string text, out string error)
    {
        error = "";
        ParseTimingSection(text, out var redLines, out var greenLines, out error);

        if (redLines.Count == 0)
        {
            error = "No uninherited timing points found in the file.";
            return false;
        }

        // Store green lines on the project for round-trip export
        Project.Instance.ImportedGreenLines = greenLines;

        ApplyToTiming(redLines);
        return true;
    }

    private record OsuRedLine(float OffsetSec, float MsPerBeat, int BeatsInMeasure);

    private static void ParseTimingSection(
        string text,
        out List<OsuRedLine> redLines,
        out List<string> greenLines,
        out string error)
    {
        error = "";
        redLines = [];
        greenLines = [];

        bool inTimingSection = false;

        foreach (string rawLine in text.Split('\n'))
        {
            string line = rawLine.Trim();

            if (line == "[TimingPoints]")
            {
                inTimingSection = true;
                continue;
            }

            if (inTimingSection)
            {
                if (line.StartsWith('[') || line == "")
                    break;
                if (line.StartsWith("//"))
                    continue;

                string[] parts = line.Split(',');
                if (parts.Length < 8)
                    continue;

                // Column 6: 1 = red (uninherited), 0 = green (inherited)
                if (!int.TryParse(parts[6].Trim(), out int uninherited))
                    continue;

                if (uninherited == 0)
                {
                    // Keep the raw line verbatim — we'll re-emit it in the export unchanged
                    greenLines.Add(line);
                    continue;
                }

                // Red line
                if (!float.TryParse(parts[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float offsetMs))
                    continue;
                if (!float.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float msPerBeat))
                    continue;
                if (!int.TryParse(parts[2].Trim(), out int beatsInMeasure))
                    beatsInMeasure = 4;

                // Invert the export offset so timing sits at the true audio position
                float correctedOffsetMs = offsetMs - Settings.Instance.ExportOffsetMs;
                float offsetSec = correctedOffsetMs / 1000f;

                redLines.Add(new OsuRedLine(offsetSec, msPerBeat, beatsInMeasure));
            }
        }
    }

    // Apply to Timing 
    private static void ApplyToTiming(List<OsuRedLine> points)
    {
        var timing = Timing.Instance;

        timing.IsInstantiating = true;
        timing.DeleteAllTimingPoints();

        for (int i = 0; i < points.Count; i++)
        {
            var p = points[i];
            float bpm = 60000f / p.MsPerBeat;
            float measuresPerSecond = bpm / (60f * p.BeatsInMeasure);

            float measurePosition = i == 0
                ? 0f
                : timing.OffsetToMeasurePosition(p.OffsetSec);

            int[] timeSig = [p.BeatsInMeasure, 4];
            timing.AddTimingPoint(measurePosition, p.OffsetSec, measuresPerSecond);

            var added = timing.TimingPoints[timing.TimingPoints.Count - 1];
            added.TimeSignature = timeSig;
        }

        timing.IsInstantiating = false;

        GlobalEvents.Instance.InvokeEvent(nameof(GlobalEvents.TimingPointCountChanged));
        GlobalEvents.Instance.InvokeEvent(nameof(GlobalEvents.TimingChanged));
        MementoHandler.Instance.AddTimingMemento();
    }
}
