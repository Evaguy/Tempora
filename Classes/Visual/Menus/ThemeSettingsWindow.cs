// Copyright 2024 https://github.com/kongehund
// 
// This file is licensed under the Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International (CC BY-NC-ND 4.0).

using Godot;
using Tempora.Classes.Utility;

namespace Tempora.Classes.Visual;

public partial class ThemeSettingsWindow : Window
{
    [Export] ColorPickerButton backgroundPicker = null!;
    [Export] ColorPickerButton timingPointPicker = null!;
    [Export] ColorPickerButton selectionPicker = null!;
    [Export] ColorPickerButton downbeatPicker = null!;
    [Export] ColorPickerButton grid16Picker = null!;
    [Export] ColorPickerButton grid12Picker = null!;
    [Export] ColorPickerButton gridOtherPicker = null!;

    [Export] Button presetDefaultButton = null!;
    [Export] Button presetDarkButton = null!;
    [Export] Button presetCyberpunkButton = null!;
    [Export] Button presetSoftButton = null!;

    [Export] Button okButton = null!;
    [Export] Button applyButton = null!;
    [Export] Button cancelButton = null!;

    // snapshot taken when the window opens, so Cancel can restore it
    private ThemeSettings? snapshot;

    public override void _Ready()
    {
        CloseRequested += OnCancel;
        AboutToPopup += OnAboutToPopup;

        presetDefaultButton.Pressed += () => ApplyPreset(ThemeSettings.PresetDefault());
        presetDarkButton.Pressed += () => ApplyPreset(ThemeSettings.PresetDark());
        presetCyberpunkButton.Pressed += () => ApplyPreset(ThemeSettings.PresetCyberpunk());
        presetSoftButton.Pressed += () => ApplyPreset(ThemeSettings.PresetSoft());

        // Live preview: any picker change immediately updates the scene
        backgroundPicker.ColorChanged += c => { Settings.Instance.Theme.Background = c; FireThemeChanged(); };
        timingPointPicker.ColorChanged += c => { Settings.Instance.Theme.TimingPoint = c; FireThemeChanged(); };
        selectionPicker.ColorChanged += c => { Settings.Instance.Theme.TimingPointSelection = c; FireThemeChanged(); };
        downbeatPicker.ColorChanged += c => { Settings.Instance.Theme.GridDownbeat = c; FireThemeChanged(); };
        grid16Picker.ColorChanged += c => { Settings.Instance.Theme.Grid16th = c; FireThemeChanged(); };
        grid12Picker.ColorChanged += c => { Settings.Instance.Theme.Grid12th = c; FireThemeChanged(); };
        gridOtherPicker.ColorChanged += c => { Settings.Instance.Theme.GridOther = c; FireThemeChanged(); };

        okButton.Pressed += OnOk;
        applyButton.Pressed += OnApply;
        cancelButton.Pressed += OnCancel;
    }

    private void OnAboutToPopup()
    {
        snapshot = Settings.Instance.Theme.Clone();
        LoadPickersFromSettings();
    }

    private void LoadPickersFromSettings()
    {
        var t = Settings.Instance.Theme;
        backgroundPicker.Color = t.Background;
        timingPointPicker.Color = t.TimingPoint;
        selectionPicker.Color = t.TimingPointSelection;
        downbeatPicker.Color = t.GridDownbeat;
        grid16Picker.Color = t.Grid16th;
        grid12Picker.Color = t.Grid12th;
        gridOtherPicker.Color = t.GridOther;
    }

    private void ApplyPreset(ThemeSettings preset)
    {
        Settings.Instance.Theme = preset;
        LoadPickersFromSettings();
        FireThemeChanged();
    }

    private void OnOk()
    {
        Settings.Instance.SaveSettings();
        Hide();
    }

    private void OnApply()
    {
        Settings.Instance.SaveSettings();
    }

    private void OnCancel()
    {
        if (snapshot != null)
        {
            Settings.Instance.Theme = snapshot;
            FireThemeChanged();
        }
        Hide();
    }

    private static void FireThemeChanged() =>
        GlobalEvents.Instance.InvokeEvent(nameof(GlobalEvents.ThemeChanged));
}
