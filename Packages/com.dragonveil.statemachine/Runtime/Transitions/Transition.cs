using System;
using System.Collections;

namespace Dragonveil.StateMachine.Transitions
{
    /// <summary>
    /// Transition Conditions. Conditions are bool methods.
    /// </summary>
    public delegate bool Condition();

    public class Transition
    {
        #region Fields
        private Condition _condition;
        private State _fromState;
        private State _toState;
        private int _priority;
        private readonly IEnumerator transitionRoutine;
        private readonly Action transitionMethod;
        private readonly float transitionTimeInSeconds;
        #endregion

        #region Properties
        /// <summary>
        /// The Condition needed to be true in order for a transition to happen between <see cref="Transition.FromState"/> <see cref="Transition.ToState"/>
        /// </summary>
        public Condition Condition
        {
            get => _condition;
            set => _condition = value;
        }

        /// <summary>
        /// The state that you can transition from.
        /// </summary>
        public State FromState
        {
            get => _fromState;
            set => _fromState = value;
        }

        /// <summary>
        /// The state you will transition to.
        /// </summary>
        public State ToState
        {
            get => _toState;
            set => _toState = value;
        }

        /// <summary>
        /// Higher values win when multiple transitions are valid on the same tick.
        /// Equal priority keeps creation-order behavior.
        /// </summary>
        public int Priority
        {
            get => _priority;
            set => _priority = value;
        }
        public IEnumerator TransitionRoutine => transitionRoutine;
        public Action TransitionMethod => transitionMethod;
        public float TransitionTimeInSeconds => transitionTimeInSeconds;
        #endregion

        #region Constructors
        /// <summary>
        /// Create a transition between <paramref name="fromState"/> to <paramref name="toState"/> with a trigger <paramref name="condition"/>.
        /// </summary>
        public Transition(State fromState, State toState, Condition condition, int priority = 0)
        {
            FromState = fromState;
            ToState = toState;
            Condition = condition;
            Priority = priority;
            transitionRoutine = null;
            transitionMethod = null;
            transitionTimeInSeconds = 0f;
        }

        /// <summary>
        /// Create a transition between <paramref name="fromState"/> to <paramref name="toState"/> with a trigger <paramref name="condition"/>.
        /// Add a coroutine that occurs between transitions.
        /// </summary>
        public Transition(State fromState, State toState, Condition condition, IEnumerator ienumTransitionMethod, float transitionTimeInSeconds, int priority = 0)
        {
            FromState = fromState;
            ToState = toState;
            Condition = condition;
            Priority = priority;
            transitionMethod = null;
            transitionRoutine = ienumTransitionMethod;
            this.transitionTimeInSeconds = transitionTimeInSeconds;
        }

        /// <summary>
        /// Create a transition between <paramref name="fromState"/> to <paramref name="toState"/> with a trigger <paramref name="condition"/>.
        /// Add a function that occurs between transitions.
        /// </summary>
        public Transition(State fromState, State toState, Condition condition, Action transitionMethod, float transitionTimeInSeconds, int priority = 0)
        {
            FromState = fromState;
            ToState = toState;
            Condition = condition;
            Priority = priority;
            this.transitionMethod = transitionMethod;
            transitionRoutine = null;
            this.transitionTimeInSeconds = transitionTimeInSeconds;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Are all conditions met to allow for the transition?
        /// </summary>
        public bool IsConditionsMet()
        {
            return Condition?.Invoke() == true;
        }
        #endregion
    }
}
