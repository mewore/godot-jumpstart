public class PlayerActive : PlayerState
{
    public override void PhysicsProcess(float delta)
    {
        player.Move(delta, true);
    }
}
