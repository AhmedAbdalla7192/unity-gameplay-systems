// Copyright (c) 2024-2026 Ahmed Abdalla. All rights reserved. See LICENSE in the repository root.

using System;
using System.Collections.Generic;

namespace Dragonveil.StateMachine.Tests
{
    /// <summary>
    /// A leaf state that records each lifecycle call as "Name.Method" in a shared log.
    /// </summary>
    internal class RecordingState : State
    {
        private readonly string name;
        private readonly List<string> log;

        public RecordingState(string name, List<string> log)
        {
            this.name = name;
            this.log = log;
        }

        public override void Enter() => log.Add($"{name}.Enter");
        public override void Do() => log.Add($"{name}.Do");
        public override void FixedDo() => log.Add($"{name}.FixedDo");
        public override void Exit() => log.Add($"{name}.Exit");
        public override string ToString() => name;
    }

    /// <summary>Marker used to test <see cref="StateExtensions.IsInCategory{TMarker}"/>.</summary>
    internal interface IGroundedState { }

    /// <summary>
    /// A super state that records its own lifecycle and starts in the sub-state it is given.
    /// </summary>
    internal class RecordingSuperState : SuperState, IGroundedState
    {
        private readonly string name;
        private readonly List<string> log;

        public State InitialSubState { get; set; }

        public RecordingSuperState(StateController controller, string name, List<string> log) : base(controller)
        {
            this.name = name;
            this.log = log;
        }

        protected override void SuperStateEnter() => log.Add($"{name}.Enter");
        protected override void SuperStateDo() { }
        protected override void SuperStateFixedDo() { }
        protected override void SuperStateExit() => log.Add($"{name}.Exit");
        protected override State AwakeSubState() => InitialSubState;
        public override string ToString() => name;
    }

    /// <summary>
    /// A controller whose states and transitions are set up by each test.
    /// Add it to an inactive GameObject, configure it, then activate the GameObject.
    /// </summary>
    internal class TestStateController : StateController
    {
        public State InitialState { get; set; }
        public Action<TestStateController> BuildTransitions { get; set; }
        public bool HasAuthority { get; set; } = true;

        protected override bool CanTickStateMachine => HasAuthority;

        public override void InitialiseStates() { }
        public override void CreateTransitions() => BuildTransitions?.Invoke(this);
        public override State AwakeState() => InitialState;
    }
}

namespace Dragonveil.StateMachine.Tests
{
    internal static class LogExtensions
    {
        /// <summary>
        /// The log without physics-step entries, whose timing between frames depends on the frame rate.
        /// </summary>
        public static string[] WithoutPhysicsSteps(this System.Collections.Generic.List<string> log)
            => log.FindAll(entry => !entry.EndsWith(".FixedDo")).ToArray();
    }
}
