using Godot;

public class LineBar : Line2D
{
    private Line2D inner;

    private float initialLength;

    public float Value
    {
        get => (inner.Points[1].x - inner.Points[0].x) / initialLength;
        set => inner.Points = new Vector2[] {
            inner.Points[0],
            new Vector2(inner.Points[0].x + Mathf.Clamp(value, 0f, 1f) * initialLength, inner.Points[1].y)
        };
    }

    public override void _Ready()
    {
        inner = GetNode<Line2D>("Inner");
        initialLength = inner.Points[1].x - inner.Points[0].x;
    }
}
