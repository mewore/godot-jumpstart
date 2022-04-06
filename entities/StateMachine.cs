using Godot;
using System;
using System.Collections.Generic;

public class StateMachine : Node
{
    private readonly Dictionary<string, State> stateByName = new Dictionary<string, State>();
    private State state;

    public override void _Ready()
    {
        var stateNodes = GetChildren();
        var initialStates = new List<State>(stateNodes.Count);
        for (int nodeIndex = 0; nodeIndex < stateNodes.Count; nodeIndex++)
        {
            if (stateNodes[nodeIndex] is State)
            {
                var state = (State)stateNodes[nodeIndex];
                if (this.state == null)
                {
                    this.state = state;
                }
                stateByName.Add(state.Name, state);
            }
            else
            {
                GD.PushWarning("State machine " + GetPath() + " contains a non-state node: " + ((Node)stateNodes[nodeIndex]).Name);
            }
        }
        if (state == null)
        {
            GD.PushWarning("State machine " + GetPath() + " contains no state nodes");
            QueueFree();
        }
    }

    public override void _Process(float delta) => performStateUpdate(() => state.Process(delta));

    public override void _PhysicsProcess(float delta) => performStateUpdate(() => state.PhysicsProcess(delta));

    public override void _UnhandledInput(InputEvent @event) => performStateUpdate(() => state.UnhandledInput(@event));

    private void performStateUpdate(Action stateUpdate)
    {
        stateUpdate.Invoke();
        string targetState = state.TargetState;
        if (targetState != null)
        {
            if (state.Name.Equals(targetState))
            {
                GD.PushWarning("Cannot switch to state '" + targetState + "' from the same state.");
                return;
            }
            if (!stateByName.ContainsKey(targetState))
            {
                GD.PushWarning("Cannot switch to state '" + targetState + "' because it does not exist.");
                return;
            }
            state.Exit();
            state.ClearTargetState();
            state = stateByName[targetState];
            state.ClearTargetState();
            state.Enter();
        }
    }
}
