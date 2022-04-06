public class PlayerState : State
{
    protected const string ACTIVE = "Active";

    protected Player player;

    public override void _Ready()
    {
        player = GetOwner<Player>();
    }
}
