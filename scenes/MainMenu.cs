using Godot;

public class MainMenu : Node2D
{
    private Button playButton;
    private Button continueButton;
    private Overlay overlay;
    private SoundControl soundControl;

    public override void _Ready()
    {
        var container = GetNode<Node>("Container/VBoxContainer");
        container.GetNode<Label>("Title").Text = (string)ProjectSettings.GetSetting("application/config/name");
        playButton = container.GetNode<Button>("PlayButton");
        continueButton = container.GetNode<Button>("ContinueButton");
        overlay = GetNode<Overlay>("Overlay");
        if (Global.ReturningToMenu)
        {
            overlay.FadeInReverse();
            Global.ReturningToMenu = false;
        }
        else
        {
            overlay.FadeIn();
        }
        if (!Global.LoadBestLevel())
        {
            continueButton.Disabled = true;
        }
        else
        {
            continueButton.Text += " from level " + Global.BestLevel;
        }
        container.GetNode<CanvasItem>("WinLabel").Visible = Global.HasBeatenAllLevels;
        soundControl = container.GetNode<SoundControl>("SoundControl");
        GlobalSound.GetInstance(this).MusicForeground = false;
    }

    public void _on_PlayButton_pressed()
    {
        Global.SetLevelToFirst();
        FadeOut();
    }

    public void _on_ContinueButton_pressed()
    {
        Global.SetLevelToBest();
        FadeOut();
    }

    private void FadeOut()
    {
        playButton.Disabled = true;
        continueButton.Disabled = true;
        soundControl.Editable = false;
        overlay.FadeOut();
    }

    public void _on_Overlay_FadeOutDone()
    {
        GetTree().ChangeScene(Global.CurrentLevelPath);
    }
}
