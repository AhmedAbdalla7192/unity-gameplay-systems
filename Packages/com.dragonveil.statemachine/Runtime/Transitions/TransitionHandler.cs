using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dragonveil.StateMachine.Transitions
{
    /// <summary>
    /// Holds one level's transitions and its current state, and runs a transition when its condition is met.
    /// A <see cref="StateController"/> owns the root handler; each <see cref="SuperState"/> owns one for its sub-states.
    /// </summary>
    public sealed class TransitionHandler
    {
        private readonly List<Transition> transitions = new();
        private readonly StateController stateController;
        private readonly bool isRoot;
        private Coroutine runningTransition;

        public State CurrentState { get; private set; }
        public bool IsTransitioning { get; private set; }
        public IReadOnlyList<Transition> Transitions => transitions;

        /// <summary>
        /// Raised after this handler finishes a transition, with the previous and new state.
        /// For a super state's handler this reports sub-state changes; the controller's own event reports root changes only.
        /// </summary>
        public event Action<State, State> StateChanged;

        internal TransitionHandler(StateController attachedController, bool isRoot)
        {
            stateController = attachedController ?? throw new ArgumentNullException(nameof(attachedController));
            this.isRoot = isRoot;
        }

        internal void SetInitialState(State state)
        {
            CurrentState = state ?? throw new InvalidOperationException("The initial state cannot be null.");
        }

        public void CreateTransition(State fromState, State toState, Condition condition, int priority = 0)
            => AddTransition(new Transition(fromState, toState, condition, priority));

        public void CreateTransition(State fromState, State toState, Condition condition, Func<IEnumerator> transitionRoutine, float transitionTimeInSeconds = 0f, int priority = 0)
            => AddTransition(new Transition(fromState, toState, condition, transitionRoutine, transitionTimeInSeconds, priority));

        public void CreateTransition(State fromState, State toState, Condition condition, Action transitionMethod, float transitionTimeInSeconds = 0f, int priority = 0)
            => AddTransition(new Transition(fromState, toState, condition, transitionMethod, transitionTimeInSeconds, priority));

        public void CreateTwoWayTransition(State fromState, State toState, Condition fromCondition, Condition toCondition, int priority = 0)
        {
            CreateTransition(fromState, toState, fromCondition, priority);
            CreateTransition(toState, fromState, toCondition, priority);
        }

        public void AddTransition(Transition transition)
        {
            if (transition != null && !transitions.Contains(transition))
                transitions.Add(transition);
        }

        public void DeleteTransition(Transition transition)
        {
            if (!transitions.Remove(transition))
                Debug.LogWarning($"Transition '{transition}' was not registered.");
        }

        public void ReplaceTransition(Transition oldTransition, Transition newTransition)
        {
            DeleteTransition(oldTransition);
            AddTransition(newTransition);
        }

        public bool CheckAndTransition()
        {
            if (IsTransitioning)
                return false;

            Transition transition = SelectBestTransitionFrom(CurrentState);
            if (transition == null)
                return false;

            State previous = CurrentState;
            previous?.Exit();
            if (isRoot)
                stateController.NotifyTransitionStarted();
            IsTransitioning = true;

            bool hasSteps = transition.TransitionRoutine != null ||
                            transition.TransitionMethod != null ||
                            transition.TransitionTimeInSeconds > 0f;
            if (hasSteps)
            {
                Coroutine started = stateController.StartCoroutine(RunTransition(previous, transition));
                // Steps with nothing to wait on finish inside StartCoroutine, so only keep a handle that is still running.
                if (IsTransitioning)
                    runningTransition = started;
            }
            else
            {
                CompleteTransition(previous, transition.ToState);
            }

            return true;
        }

        /// <summary>
        /// Stops a transition that is waiting on its steps. The state being left stays current and has already exited,
        /// so it is entered again the next time this level is entered. Its destination is never entered.
        /// </summary>
        internal void CancelTransition()
        {
            if (!IsTransitioning)
                return;

            if (runningTransition != null)
                stateController.StopCoroutine(runningTransition);
            runningTransition = null;
            IsTransitioning = false;
        }

        internal Transition SelectBestTransitionFrom(State fromState)
        {
            Transition best = null;
            int bestPriority = int.MinValue;
            for (int i = 0; i < transitions.Count; i++)
            {
                Transition candidate = transitions[i];
                if (candidate.FromState != fromState || !candidate.IsConditionsMet())
                    continue;

                if (best == null || candidate.Priority > bestPriority)
                {
                    best = candidate;
                    bestPriority = candidate.Priority;
                }
            }

            return best;
        }

        private IEnumerator RunTransition(State previous, Transition transition)
        {
            transition.TransitionMethod?.Invoke();

            if (transition.TransitionRoutine != null)
                yield return transition.TransitionRoutine();

            if (transition.TransitionTimeInSeconds > 0f)
                yield return new WaitForSeconds(transition.TransitionTimeInSeconds);

            CompleteTransition(previous, transition.ToState);
        }

        private void CompleteTransition(State previous, State next)
        {
            runningTransition = null;
            CurrentState = next ?? throw new InvalidOperationException("A transition target cannot be null.");
            bool entered = stateController.CanCurrentlyTick;
            if (entered)
                CurrentState.Enter();
            IsTransitioning = false;

            if (isRoot)
                stateController.NotifyStateChanged(previous, CurrentState, entered);
            StateChanged?.Invoke(previous, CurrentState);
        }
    }
}
