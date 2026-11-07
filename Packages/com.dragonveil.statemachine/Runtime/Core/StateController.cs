using Dragonveil.StateMachine.Transitions;
using System;
using UnityEngine;

namespace Dragonveil.StateMachine
{
    public abstract class StateController : MonoBehaviour
    {
        private TransitionHandler transitionHandler;
        private State lastCurrentState;
        private bool initialized;
        private bool currentStateEntered;
        private bool hadTickAuthority;

        public State CurrentState => transitionHandler?.CurrentState;
        public State LastCurrentState => lastCurrentState;
        public bool IsTransitioning => transitionHandler?.IsTransitioning ?? false;

        public event Action<State, State> StateChanged;

        internal bool CanCurrentlyTick => isActiveAndEnabled && CanTickStateMachine;

        /// <summary>
        /// Override in an integration adapter to gate ticking. The reusable core is local by default.
        /// </summary>
        protected virtual bool CanTickStateMachine => true;

        protected virtual void Awake()
        {
            EnsureInitialized();
            RefreshTickAuthority(force: true);
        }

        protected virtual void OnEnable()
        {
            EnsureInitialized();
            RefreshTickAuthority(force: true);
        }

        protected virtual void Update()
        {
            if (!RefreshTickAuthority())
                return;

            transitionHandler.CheckAndTransition();
            if (!transitionHandler.IsTransitioning)
                transitionHandler.CurrentState?.Do();

            if (CurrentState != null)
                lastCurrentState = CurrentState;
        }

        protected virtual void FixedUpdate()
        {
            if (!RefreshTickAuthority() || transitionHandler.IsTransitioning)
                return;

            transitionHandler.CurrentState?.FixedDo();
        }

        protected virtual void OnDisable()
        {
            // Unity stops this object's coroutines on disable, so cancel a waiting transition rather than leave it hanging.
            transitionHandler?.CancelTransition();
            ExitCurrentState();
            hadTickAuthority = false;
        }

        public void CreateTransition(State fromState, State toState, Condition condition, int priority = 0)
            => transitionHandler.CreateTransition(fromState, toState, condition, priority);

        public void CreateTransition(State fromState, State toState, Condition condition, Func<System.Collections.IEnumerator> transitionRoutine, float transitionTimeInSeconds = 0f, int priority = 0)
            => transitionHandler.CreateTransition(fromState, toState, condition, transitionRoutine, transitionTimeInSeconds, priority);

        public void CreateTransition(State fromState, State toState, Condition condition, Action transitionMethod, float transitionTimeInSeconds = 0f, int priority = 0)
            => transitionHandler.CreateTransition(fromState, toState, condition, transitionMethod, transitionTimeInSeconds, priority);

        public void CreateTwoWayTransition(State fromState, State toState, Condition fromCondition, Condition toCondition, int priority = 0)
            => transitionHandler.CreateTwoWayTransition(fromState, toState, fromCondition, toCondition, priority);

        public void AddTransition(Transition transition) => transitionHandler.AddTransition(transition);
        public void DeleteTransition(Transition transition) => transitionHandler.DeleteTransition(transition);
        public void ReplaceTransition(Transition oldTransition, Transition newTransition) => transitionHandler.ReplaceTransition(oldTransition, newTransition);

        public abstract void InitialiseStates();
        public abstract void CreateTransitions();
        public abstract State AwakeState();

        internal void NotifyTransitionStarted()
        {
            currentStateEntered = false;
        }

        internal void NotifyStateChanged(State previous, State current, bool entered)
        {
            lastCurrentState = previous;
            currentStateEntered = entered;
            StateChanged?.Invoke(previous, current);
        }

        private void EnsureInitialized()
        {
            if (initialized)
                return;

            transitionHandler = new TransitionHandler(this, isRoot: true);
            InitialiseStates();
            transitionHandler.SetInitialState(AwakeState());
            CreateTransitions();
            initialized = true;
        }

        private bool RefreshTickAuthority(bool force = false)
        {
            bool canTick = isActiveAndEnabled && CanTickStateMachine;
            if (!force && canTick == hadTickAuthority)
                return canTick;

            if (canTick && !currentStateEntered)
            {
                transitionHandler.CurrentState?.Enter();
                currentStateEntered = transitionHandler.CurrentState != null;
            }
            else if (!canTick)
            {
                ExitCurrentState();
            }

            hadTickAuthority = canTick;
            return canTick;
        }

        private void ExitCurrentState()
        {
            if (!initialized || !currentStateEntered)
                return;

            try
            {
                transitionHandler.CurrentState?.Exit();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
            finally
            {
                currentStateEntered = false;
            }
        }
    }
}
