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
/// Only uninherited (red) timing points are imported.
/// </summary>
public static class OsuImporter
{
    // Public entry points 
    /// <summary>
    /// Import timing from a .osu file on disk.
    /// </summary>
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

    /// <summary>
    /// Import timing from a .osz archive (picks the first .osu file it finds inside).
    /// </summary>
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
        var parsed = ParseTimingPoints(text, out error);
        if (parsed == null)
            return false;

        if (parsed.Count == 0)
        {
            error = "No uninherited timing points found in the file.";
            return false;
        }

        ApplyToTiming(parsed);
        return true;
    }

    private record OsuTimingPoint(float OffsetSec, float MsPerBeat, int BeatsInMeasure);

    private static List<OsuTimingPoint>? ParseTimingPoints(string text, out string error)
    {
        error = "";
        var result = new List<OsuTimingPoint>();

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
                // Empty line or new section ends the timing block
                if (line.StartsWith('[') || line == "")
                    break;

                if (line.StartsWith("//"))
                    continue;

                string[] parts = line.Split(',');
                if (parts.Length < 8)
                    continue;

                // Column 7 (0-indexed): 1 = uninherited (red line), 0 = inherited (green)
                if (!int.TryParse(parts[6].Trim(), out int uninherited) || uninherited != 1)
                    continue;

                if (!float.TryParse(parts[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float offsetMs))
                    continue;

                if (!float.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float msPerBeat))
                    continue;

                if (!int.TryParse(parts[2].Trim(), out int beatsInMeasure))
                    beatsInMeasure = 4;

                // Invert the export offset so the timing snaps back to the true audio position
                float correctedOffsetMs = offsetMs - Settings.Instance.ExportOffsetMs;
                float offsetSec = correctedOffsetMs / 1000f;

                result.Add(new OsuTimingPoint(offsetSec, msPerBeat, beatsInMeasure));
            }
        }

        return result;
    }

    // Apply to Timing
    private static void ApplyToTiming(List<OsuTimingPoint> points)
    {
        var timing = Timing.Instance;

        // Suppress per-point events during batch import
        timing.IsInstantiating = true;
        timing.DeleteAllTimingPoints();

        for (int i = 0; i < points.Count; i++)
        {
            var p = points[i];
            float bpm = 60000f / p.MsPerBeat;

            // MeasuresPerSecond = bpm / (60 * beatsPerMeasure)
            // beatsPerMeasure is the numerator of the time signature in 4/4-equivalent beats
            float beatsPerMeasure = p.BeatsInMeasure; // osu stores the numerator directly
            float measuresPerSecond = bpm / (60f * beatsPerMeasure);

            // Measure position: the first point always sits on measure 0.
            // Each subsequent point's position is derived from where the previous
            // timing puts it relative to the audio.
            float measurePosition;
            if (i == 0)
            {
                measurePosition = 0f;
            }
            else
            {
                // Compute measure position from offset using all previously loaded points
                // (timing.IsInstantiating = true so OffsetToMeasurePosition uses the
                // points already in the list)
                measurePosition = timing.OffsetToMeasurePosition(p.OffsetSec);
            }

            int[] timeSig = [p.BeatsInMeasure, 4];
            timing.AddTimingPoint(measurePosition, p.OffsetSec, measuresPerSecond);

            // Set time signature separately after the point is added
            var added = timing.TimingPoints[timing.TimingPoints.Count - 1];
            added.TimeSignature = timeSig;
        }

        timing.IsInstantiating = false;

        GlobalEvents.Instance.InvokeEvent(nameof(GlobalEvents.TimingPointCountChanged));
        GlobalEvents.Instance.InvokeEvent(nameof(GlobalEvents.TimingChanged));
        MementoHandler.Instance.AddTimingMemento();
    }
}
