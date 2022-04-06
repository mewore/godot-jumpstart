using Godot;

[Tool]
public class AutoPolygon : Polygon2D
{
    public override async void _Ready()
    {
        Node parent = GetParent<Node>();
        await ToSignal(parent, "ready");
        if (parent is CollisionPolygon2D)
        {
            Polygon = ((CollisionPolygon2D)parent).Polygon;
        }
        else if (parent is CollisionShape2D)
        {
            var shape = ((CollisionShape2D)parent).Shape;
        }
    }

    public override void _Process(float delta)
    {
        if (Engine.EditorHint)
        {
            Node parent = GetParent<Node>();
            if (parent is CollisionPolygon2D)
            {
                Polygon = ((CollisionPolygon2D)parent).Polygon;
            }
            else if (parent is CollisionShape2D)
            {
                var shape = ((CollisionShape2D)parent).Shape;
            }
        }
    }
}
