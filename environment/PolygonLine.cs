using Godot;

[Tool]
public class PolygonLine : Line2D
{
    public override async void _Ready()
    {
        var polygon = GetParent<Polygon2D>();
        await ToSignal(polygon, "ready");
        var points = new Vector2[polygon.Polygon.Length + 1];
        polygon.Polygon.CopyTo(points, 0);
        points[points.Length - 1] = points[0];
        Points = points;
    }

    public override void _Process(float delta)
    {
        if (Engine.EditorHint)
        {
            var polygon = GetParent<Polygon2D>();
            var points = new Vector2[polygon.Polygon.Length + 1];
            polygon.Polygon.CopyTo(points, 0);
            points[points.Length - 1] = points[0];
            Points = points;
        }
    }
}
