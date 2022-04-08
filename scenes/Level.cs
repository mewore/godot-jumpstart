using Godot;

public class Level : Node2D
{
    private const string MAIN_MENU_PATH = "res://scenes/MainMenu.tscn";

    private Overlay overlay;
    private int currentLevel;
    bool paused = false;
    string targetScene;

    private CanvasItem pauseMenu;

    public override void _Ready()
    {
        overlay = GetNode<Overlay>("Overlay");
        pauseMenu = GetNode<CanvasItem>("PauseUi/PauseMenu");
        overlay.FadeIn();
        currentLevel = Global.CurrentLevel;
        GlobalSound.GetInstance(this).MusicForeground = true;
    }

    public override void _Process(float delta)
    {
        GetTree().Paused = overlay.Transitioning || paused;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("debug_clear_level"))
        {
            if (OS.IsDebugBuild())
            {
                WinLevel();
            }
        }
        else if (@event.IsActionPressed("pause"))
        {
            if (!overlay.Transitioning)
            {
                pauseMenu.Visible = paused = !paused;
                GlobalSound.GetInstance(this).MusicForeground = !paused;
            }
        }
    }

    public void _on_Overlay_FadeOutDone()
    {
        pauseMenu.Visible = paused = false;
        if (targetScene != null)
        {
            GetTree().ChangeScene(targetScene);
        }
        else
        {
            GD.PushWarning("No target scene is set. Reloading the current scene as a fallback");
            GetTree().ReloadCurrentScene();
        }
    }

    private void LoseLevel()
    {
        GlobalSound.GetInstance(this).PlayLose();
        overlay.FadeOutReverse();
    }

    private void WinLevel()
    {
        GlobalSound.GetInstance(this).PlayClearLevel();
        targetScene = Global.WinLevel(currentLevel) ? Global.CurrentLevelPath : MAIN_MENU_PATH;
        overlay.FadeOut();
    }

    public void _on_Resume_pressed()
    {
        pauseMenu.Visible = paused = false;
        GlobalSound.GetInstance(this).MusicForeground = true;
    }

    public void _on_RestartLevel_pressed()
    {
        targetScene = Global.CurrentLevelPath;
        overlay.FadeOutReverse();
    }

    public void _on_ReturnToMenu_pressed()
    {
        Global.ReturningToMenu = true;
        targetScene = MAIN_MENU_PATH;
        overlay.FadeOutReverse();
    }
}
