using Godot;

public class Overlay : CanvasLayer
{
    [Signal]
    delegate void FadeInDone();

    [Signal]
    delegate void FadeOutDone();

    private AnimationPlayer animationPlayer;

    private ScreenState state = ScreenState.HIDDEN;

    public bool Transitioning { get => animationPlayer.IsPlaying() || animationPlayer.GetQueue().Length > 0; }

    public override void _Ready()
    {
        animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
        GetNode<CanvasItem>("Polygon2D").Visible = true;
    }

    public void FadeIn()
    {
        DoAnimation("fade_in", ScreenState.VISIBLE);
    }

    public void FadeInReverse()
    {
        DoAnimation("fade_in_reverse", ScreenState.VISIBLE);
    }

    public void FadeOut()
    {
        DoAnimation("fade_out", ScreenState.HIDDEN);
    }

    public void FadeOutReverse()
    {
        DoAnimation("fade_out_reverse", ScreenState.HIDDEN);
    }

    private void DoAnimation(string animationName, ScreenState targetState)
    {
        if (state != targetState)
        {
            state = targetState;
            animationPlayer.Queue(animationName);
        }
    }

    public void _on_AnimationPlayer_animation_finished(string animationName)
    {
        if (animationName.StartsWith("fade_in"))
        {
            EmitSignal(nameof(FadeInDone));
        }
        else if (animationName.StartsWith("fade_out"))
        {
            EmitSignal(nameof(FadeOutDone));
        }
    }
}

enum ScreenState
{
    VISIBLE,
    HIDDEN
}
