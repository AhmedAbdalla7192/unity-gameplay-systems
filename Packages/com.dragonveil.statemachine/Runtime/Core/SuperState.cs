// Copyright (c) 2024-2026 Ahmed Abdalla. All rights reserved. See LICENSE in the repository root.

using Dragonveil.StateMachine.Transitions;

namespace Dragonveil.StateMachine
{
    /// <summary>
    /// A state with its own sub-states and transitions, such as Ground containing Idle, Walk and Run.
    /// Leaving and re-entering it resumes the sub-state it was last in.
    /// </summary>
    public abstract class SuperState : State
    {
        #region Properties

        /// <summary>
        /// The sub-states' transitions. Add transitions here, and subscribe to its StateChanged event for sub-state changes.
        /// </summary>
        public TransitionHandler TransitionHandler { get; }

        #endregion

        protected SuperState(StateController stateController)
        {
            TransitionHandler = new TransitionHandler(stateController, isRoot: false);
        }

        #region Override Methods

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public sealed override void Enter()
        {
            if (TransitionHandler.CurrentState == null)
            {
                TransitionHandler.SetInitialState(AwakeSubState());
            }

            SuperStateEnter();
            // Enter the sub-state first; its transitions are checked on the next Do(), so nothing exits without entering.
            TransitionHandler.CurrentState.Enter();
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public sealed override void Do()
        {
            SuperStateDo();
            TransitionHandler.CheckAndTransition();
            if (!TransitionHandler.IsTransitioning)
                TransitionHandler.CurrentState?.Do();
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public sealed override void FixedDo()
        {
            if (!TransitionHandler.IsTransitioning)
                TransitionHandler.CurrentState?.FixedDo();
            SuperStateFixedDo();
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public override void Exit()
        {
            // A sub-state transition that is still running has already exited the old sub-state.
            bool subStateWasLeaving = TransitionHandler.IsTransitioning;
            TransitionHandler.CancelTransition();
            if (!subStateWasLeaving)
                TransitionHandler.CurrentState?.Exit();
            SuperStateExit();
        }

        #endregion

        #region Public Methods

        #endregion

        #region Abstract Methods
        /// <summary>
        /// The method that happens when the super state is first selected.
        /// </summary>
        protected abstract void SuperStateEnter();

        /// <summary>
        /// The method that happens in the Update function of the owning <see cref="StateController"/>.
        /// </summary>
        protected abstract void SuperStateDo();

        /// <summary>
        /// The method that happens in the FixedUpdate function of the owning <see cref="StateController"/>.
        /// </summary>
        protected abstract void SuperStateFixedDo();

        /// <summary>
        /// The method that happens when the state is about to exit.
        /// </summary>
        protected abstract void SuperStateExit();

        /// <summary>
        /// Sets the current substate to the returned state.
        /// </summary>
        protected abstract State AwakeSubState();

        #endregion
    }
}
