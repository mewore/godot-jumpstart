using Godot;

public class GlobalSound : Node
{
    private const float FOREGROUND_VOLUME_CHANGE_PER_SECOND = 2f;
    private const float MIN_FOREGROUND_VOLUME = -40f;
    private const float MAX_FOREGROUND_VOLUME = 0f;
    private const int FOREGROUND_MUSIC_BUS = 3;

    private AudioStreamPlayer clearLevel;
    private AudioStreamPlayer lose;

    private float targetForegroundVolume;
    public bool MusicForeground { set => targetForegroundVolume = value ? 1f : 0f; }

    private float ForegroundVolume
    {
        get => Mathf.InverseLerp(MIN_FOREGROUND_VOLUME, MAX_FOREGROUND_VOLUME, AudioServer.GetBusVolumeDb(FOREGROUND_MUSIC_BUS));
        set
        {
            AudioServer.SetBusVolumeDb(FOREGROUND_MUSIC_BUS, Mathf.Lerp(MIN_FOREGROUND_VOLUME, MAX_FOREGROUND_VOLUME, value));
            AudioServer.SetBusMute(FOREGROUND_MUSIC_BUS, value < .0001f);
        }
    }

    public override void _Ready()
    {
        clearLevel = GetNode<AudioStreamPlayer>("ClearLevel");
        lose = GetNode<AudioStreamPlayer>("Lose");
    }

    public override void _Process(float delta)
    {
        float volumeDifference = targetForegroundVolume - ForegroundVolume;
        float volumeDistance = Mathf.Abs(volumeDifference);
        if (volumeDistance > .0001f)
        {
            float maxVolumeChange = FOREGROUND_VOLUME_CHANGE_PER_SECOND * delta;
            ForegroundVolume = volumeDistance > maxVolumeChange
                ? ForegroundVolume + Mathf.Sign(volumeDifference) * maxVolumeChange
                : targetForegroundVolume;
        }

    }

    public void PlayClearLevel()
    {
        clearLevel.Play();
    }

    public void PlayLose()
    {
        lose.Play();
    }

    public static GlobalSound GetInstance(Node node)
    {
        return node.GetNode<GlobalSound>("/root/GlobalSound");
    }
}
