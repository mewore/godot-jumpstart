using System;
using Godot;

public class SoundControl : VBoxContainer
{
    private const int MIN_VOLUME_DB = -80;
    private const int MAX_VOLUME_DB = 5;

    private const int MASTER_CHANNEL_INDEX = 0;
    private const int SFX_CHANNEL_INDEX = 1;
    private const int MUSIC_CHANNEL_INDEX = 2;

    private CanvasItem sfxNode;
    private HSlider masterSlider;
    private HSlider sfxSlider;
    private CanvasItem musicNode;
    private HSlider musicSlider;

    private static float MasterVolume
    {
        get => NormalizeVolume(AudioServer.GetBusVolumeDb(MASTER_CHANNEL_INDEX));
        set => UpdateChannel(MASTER_CHANNEL_INDEX, value);
    }

    private static float SfxVolume
    {
        get => NormalizeVolume(AudioServer.GetBusVolumeDb(SFX_CHANNEL_INDEX));
        set => UpdateChannel(SFX_CHANNEL_INDEX, value);
    }

    private static float MusicVolume
    {
        get => NormalizeVolume(AudioServer.GetBusVolumeDb(MUSIC_CHANNEL_INDEX));
        set => UpdateChannel(MUSIC_CHANNEL_INDEX, value);
    }

    private bool editable = true;
    public bool Editable
    {
        get => editable;
        set
        {
            editable = value;
            if (masterSlider != null)
            {
                masterSlider.Editable = value;
            }
            SetNonMasterInputsEditable(masterSlider.Value);
        }
    }

    public override void _Ready()
    {
        masterSlider = GetNode<HSlider>("Master/MasterSlider");
        masterSlider.Editable = editable;

        sfxNode = GetNode<CanvasItem>("Sfx");
        sfxSlider = sfxNode.GetNode<HSlider>("SfxSlider");

        musicNode = GetNode<CanvasItem>("Music");
        musicSlider = musicNode.GetNode<HSlider>("MusicSlider");

        try
        {
            Global.LoadSettings();
            masterSlider.Value = Global.Settings.MasterVolume;
            sfxSlider.Value = Global.Settings.SfxVolume;
            musicSlider.Value = Global.Settings.MusicVolume;

            MasterVolume = Global.Settings.NormalizedMasterVolume;
            SfxVolume = Global.Settings.NormalizedSfxVolume;
            MusicVolume = Global.Settings.NormalizedMusicVolume;
        }
        catch (Exception)
        {
            GD.PrintErr("Exception encountered while loading and setting initial volume!");
        }

        SetNonMasterInputsEditable(masterSlider.Value);
    }

    public void _on_MasterSlider_value_changed(float newValue)
    {
        Global.Settings.MasterVolume = Mathf.RoundToInt(newValue);
        MasterVolume = Global.Settings.NormalizedMasterVolume;
        SetNonMasterInputsEditable(newValue);
        Global.SaveSettings();
    }

    public void _on_SfxSlider_value_changed(float newValue)
    {
        Global.Settings.SfxVolume = Mathf.RoundToInt(newValue);
        SfxVolume = Global.Settings.NormalizedSfxVolume;
        Global.SaveSettings();
    }

    public void _on_MusicSlider_value_changed(float newValue)
    {
        Global.Settings.MusicVolume = Mathf.RoundToInt(newValue);
        MusicVolume = Global.Settings.NormalizedMusicVolume;
        Global.SaveSettings();
    }

    private void SetNonMasterInputsEditable(double masterValue)
    {
        if (sfxSlider == null || musicSlider == null)
        {
            return;
        }
        sfxSlider.Editable = musicSlider.Editable = editable && masterValue > 0;
        float visibility = masterValue > 0 ? 1f : .666f;
        sfxNode.Modulate = musicNode.Modulate = new Color(visibility, visibility, visibility);
    }

    private static void UpdateChannel(int channelIndex, float value)
    {
        AudioServer.SetBusMute(channelIndex, value < .001f);
        AudioServer.SetBusVolumeDb(channelIndex, DenormalizeVolume(value));
    }

    private static float NormalizeVolume(float volume) => Mathf.InverseLerp(MIN_VOLUME_DB, MAX_VOLUME_DB, volume);
    private static float DenormalizeVolume(float ratio) => Mathf.Lerp(MIN_VOLUME_DB, MAX_VOLUME_DB, ratio);
}
