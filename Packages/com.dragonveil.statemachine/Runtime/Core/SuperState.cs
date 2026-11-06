using Dragonveil.StateMachine.Transitions;

namespace Dragonveil.StateMachine
{
    public abstract class SuperState : State
    {
        private TransitionHandler transitionHandler;

        #region Properties

        public TransitionHandler TransitionHandler
        {
            get => transitionHandler;
            set
            {
                transitionHandler = value;
            }
        }

        #endregion

        public SuperState(StateController stateController)
        {
            transitionHandler = new TransitionHandler(stateController);
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
            if (!TransitionHandler.CheckAndTransition())
            {
                TransitionHandler.CurrentState.Enter();
            }
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
