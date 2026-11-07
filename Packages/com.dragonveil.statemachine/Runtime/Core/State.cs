// Copyright (c) 2024-2026 Ahmed Abdalla. All rights reserved. See LICENSE in the repository root.

namespace Dragonveil.StateMachine
{
    /// <summary>
    /// One behaviour a <see cref="StateController"/> can be in, such as Idle, Run or Jump.
    /// Derive from this for leaf states; derive from <see cref="SuperState"/> for states with sub-states.
    /// </summary>
    public abstract class State
    {
        /// <summary>
        /// Called once when the state becomes active.
        /// </summary>
        public abstract void Enter();

        /// <summary>
        /// Called every frame from the owning <see cref="StateController"/>'s Update.
        /// </summary>
        public abstract void Do();

        /// <summary>
        /// Called every physics step from the owning <see cref="StateController"/>'s FixedUpdate.
        /// </summary>
        public abstract void FixedDo();

        /// <summary>
        /// Called once when the state stops being active.
        /// </summary>
        public abstract void Exit();
    }
}
