using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dragonveil.StateMachine.Transitions
{
    public sealed class TransitionHandler
    {
        private readonly List<Transition> transitions = new();
        private readonly StateController stateController;

        public State CurrentState { get; private set; }
        public bool IsTransitioning { get; private set; }
        public IReadOnlyList<Transition> Transitions => transitions;

        public TransitionHandler(StateController attachedController)
        {
            stateController = attachedController ?? throw new ArgumentNullException(nameof(attachedController));
        }

        internal void SetInitialState(State state)
        {
            CurrentState = state ?? throw new InvalidOperationException("AwakeState returned null.");
        }

        public void CreateTransition(State fromState, State toState, Condition condition, int priority = 0)
            => AddTransition(new Transition(fromState, toState, condition, priority));

        public void CreateTransition(State fromState, State toState, Condition condition, IEnumerator transitionRoutine, float transitionTimeInSeconds = 0f, int priority = 0)
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
            stateController.NotifyTransitionStarted();
            IsTransitioning = true;

            bool hasPipeline = transition.TransitionRoutine != null ||
                               transition.TransitionMethod != null ||
                               transition.TransitionTimeInSeconds > 0f;
            if (hasPipeline)
                stateController.StartCoroutine(RunTransition(previous, transition));
            else
                CompleteTransition(previous, transition.ToState);

            return true;
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
            if (transition.TransitionMethod != null)
                transition.TransitionMethod.Invoke();

            if (transition.TransitionRoutine != null)
                yield return transition.TransitionRoutine;

            if (transition.TransitionTimeInSeconds > 0f)
                yield return new WaitForSeconds(transition.TransitionTimeInSeconds);

            CompleteTransition(previous, transition.ToState);
        }

        private void CompleteTransition(State previous, State next)
        {
            CurrentState = next ?? throw new InvalidOperationException("A transition target cannot be null.");
            bool entered = stateController.CanCurrentlyTick;
            if (entered)
                CurrentState.Enter();
            IsTransitioning = false;
            stateController.NotifyStateChanged(previous, CurrentState, entered);
        }
    }
}
