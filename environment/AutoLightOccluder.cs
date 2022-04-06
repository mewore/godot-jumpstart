using Godot;

[Tool]
public class AutoLightOccluder : LightOccluder2D
{
    public override async void _Ready()
    {
        var polygon = GetParent<Polygon2D>();
        await ToSignal(polygon, "ready");
        OccluderPolygon2D newOccluder = new OccluderPolygon2D();
        newOccluder.Closed = Occluder.Closed;
        newOccluder.CullMode = Occluder.CullMode;
        newOccluder.Polygon = polygon.Polygon;
        Occluder = newOccluder;
    }

    public override void _Process(float delta)
    {
        if (Engine.EditorHint)
        {
            OccluderPolygon2D newOccluder = new OccluderPolygon2D();
            newOccluder.Closed = Occluder.Closed;
            newOccluder.CullMode = Occluder.CullMode;
            newOccluder.Polygon = GetParent<Polygon2D>().Polygon;
            Occluder = newOccluder;
        }
    }
}
