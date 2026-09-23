// Copyright 2024 https://github.com/kongehund
// 
// This file is licensed under the Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International (CC BY-NC-ND 4.0).

using Godot;

namespace Tempora.Classes.Visual;

/// <summary>
/// Holds all user-configurable theme colors and defines built-in presets.
/// </summary>
public class ThemeSettings
{
    public Color Background { get; set; } = new("002630");
    public Color TimingPoint { get; set; } = new("ff9900");
    public Color TimingPointSelection { get; set; } = new("ab0091");
    public Color GridDownbeat { get; set; } = new("ff3333");
    public Color Grid16th { get; set; } = new("b20000");
    public Color Grid12th { get; set; } = new("7572ff");
    public Color GridOther { get; set; } = new("0000b2");

    public ThemeSettings Clone() => new()
    {
        Background = Background,
        TimingPoint = TimingPoint,
        TimingPointSelection = TimingPointSelection,
        GridDownbeat = GridDownbeat,
        Grid16th = Grid16th,
        Grid12th = Grid12th,
        GridOther = GridOther,
    };

    // Presets 
    public static ThemeSettings PresetDefault() => new()
    {
        Background = new("002630"),
        TimingPoint = new("ff9900"),
        TimingPointSelection = new("ab0091"),
        GridDownbeat = new("ff3333"),
        Grid16th = new("b20000"),
        Grid12th = new("7572ff"),
        GridOther = new("0000b2"),
    };

    public static ThemeSettings PresetDark() => new()
    {
        Background           = new("111111"),
        TimingPoint          = new("ffffff"),
        TimingPointSelection = new("00d4ff"),
        GridDownbeat         = new("ff4444"),
        Grid16th             = new("cc2222"),
        Grid12th             = new("8888ff"),
        GridOther            = new("444488"),
    };

    public static ThemeSettings PresetCyberpunk() => new()
    {
        Background = new("0d0221"),
        TimingPoint = new("f72585"),
        TimingPointSelection = new("4cc9f0"),
        GridDownbeat = new("f72585"),
        Grid16th = new("b5179e"),
        Grid12th = new("7209b7"),
        GridOther = new("3a0ca3"),
    };

    public static ThemeSettings PresetSoft() => new()
    {
        Background = new("1e2030"),
        TimingPoint = new("c3e88d"),
        TimingPointSelection = new("82aaff"),
        GridDownbeat = new("ff9966"),
        Grid16th = new("cc6644"),
        Grid12th = new("c792ea"),
        GridOther = new("546e7a"),
    };
}
