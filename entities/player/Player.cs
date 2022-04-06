using Godot;

public class Player : KinematicBody2D
{
    private const float ACCELERATION = 1600f;
    private const float AIR_ACCELERATION = 400f;
    private const float INACTIVE_ACCELERATION = 100f;
    private const float MAX_SPEED = 120f;

    private const float MAX_FALL_SPEED = 300f;
    private const float GRAVITY = 600f;

    private float now = 0f;

    private float jumpSpeed;
    private const float JUMP_SPEED_RETENTION = .5f;
    private const float JUMP_GRACE_PERIOD = .2f;
    private float lastWantedToJumpAt = -JUMP_GRACE_PERIOD * 2f;
    private float lastOnFloorAt = 0f;
    private bool isJumping = false;

    private Vector2 motion = new Vector2();
    public Vector2 Motion { get => motion; }

    private Sprite sprite;
    private Sprite outlineSprite;
    private AnimationPlayer animationPlayer;
    private AnimationPlayer tipAnimationPlayer;

    private AudioStreamPlayer jumpSound;
    private AudioStreamPlayer landSound;

    [Export]
    private string inputSuffix = "";

    [Export]
    private string walkLeftInput = "move_left";

    [Export]
    private string walkRightInput = "move_right";

    [Export]
    private string jumpInput = "jump";


    public override void _Ready()
    {
        float jumpHeight = -GetNode<Node2D>("MaxJumpHeight").Position.y;
        jumpSpeed = Mathf.Sqrt(GRAVITY * jumpHeight * 2f);
        sprite = GetNode<Sprite>("Sprite");
        outlineSprite = GetNode<Sprite>("Sprite/Outline");
        animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
        tipAnimationPlayer = GetNode<AnimationPlayer>("Tip/AnimationPlayer");
        jumpSound = GetNode<AudioStreamPlayer>("JumpSound");
        landSound = GetNode<AudioStreamPlayer>("LandSound");
        if (!inputSuffix.Equals(""))
        {
            walkLeftInput += "_" + inputSuffix;
            walkRightInput += "_" + inputSuffix;
            jumpInput += "_" + inputSuffix;
        }
    }

    public override void _PhysicsProcess(float delta)
    {
        now += delta;
    }

    public override void _Process(float delta)
    {
        outlineSprite.Frame = sprite.Frame;
    }

    public void Move(float delta, bool canControl)
    {
        bool isOnFloor = IsOnFloor();
        float maxAcceleration = (canControl ? (isOnFloor ? ACCELERATION : AIR_ACCELERATION) : INACTIVE_ACCELERATION) * delta;

        float targetMotionX = canControl
            ? (Input.GetActionStrength(walkRightInput) - Input.GetActionStrength(walkLeftInput)) * MAX_SPEED
            : 0;
        motion.x = Mathf.Abs(targetMotionX - motion.x) <= maxAcceleration
            ? targetMotionX
            : motion.x + (targetMotionX > motion.x ? maxAcceleration : -maxAcceleration);

        float lastMotionY = motion.y;
        motion.y = Mathf.Min(isOnFloor ? motion.y : (motion.y + GRAVITY * delta), MAX_FALL_SPEED);

        if (targetMotionX != 0f && tipAnimationPlayer != null && !tipAnimationPlayer.IsPlaying())
        {
            tipAnimationPlayer.Play("fade_out");
            tipAnimationPlayer = null;
        }

        if (Input.IsActionJustPressed(jumpInput))
        {
            lastWantedToJumpAt = now;
        }
        if (isOnFloor)
        {
            lastOnFloorAt = now;
        }

        if (canControl)
        {
            if (!isJumping && Mathf.Max(now - lastWantedToJumpAt, now - lastOnFloorAt) <= JUMP_GRACE_PERIOD)
            {
                lastOnFloorAt = lastWantedToJumpAt = now - JUMP_GRACE_PERIOD;
                motion.y = -jumpSpeed;
                isJumping = true;
                jumpSound.PitchScale = 1f + (GD.Randf() - .5f) * .5f;
                jumpSound.Play();
            }
            else if (isJumping && Input.IsActionJustReleased(jumpInput))
            {
                motion = new Vector2(motion.x, motion.y * JUMP_SPEED_RETENTION);
                isJumping = false;
            }
        }
        isJumping = isJumping && motion.y < 0f;

        motion = MoveAndSlide(motion, Vector2.Up);

        if (canControl && !isOnFloor && IsOnFloor() && lastMotionY > jumpSpeed * .5f)
        {
            landSound.Play();
        }

        if (canControl && sprite.Visible)
        {
            string targetAnimation = (Mathf.Abs(motion.x) < MAX_SPEED * .2f) ? "idle" : "walk";
            if (now - lastOnFloorAt > JUMP_GRACE_PERIOD)
            {
                targetAnimation = ((motion.y < 0f) ? "jump" : "fall") + (targetAnimation.Equals("idle") ? "" : "_side");
            }
            int motionScale = Mathf.Sign(motion.x);
            if (motionScale != 0 && Mathf.Sign(sprite.Scale.x) != motionScale)
            {
                sprite.Scale = new Vector2(sprite.Scale.x * -1f, sprite.Scale.y);
            }
            if (!targetAnimation.Equals(animationPlayer.CurrentAnimation))
            {
                animationPlayer.Play(targetAnimation);
            }
        }
    }
}
