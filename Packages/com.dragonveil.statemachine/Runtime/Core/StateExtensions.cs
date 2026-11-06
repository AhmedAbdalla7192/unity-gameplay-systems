namespace Dragonveil.StateMachine
{
    public static class StateExtensions
    {
        /// <summary>
        /// Walks down the active state chain from a root state,
        /// checking each super/substate for the given marker/interface type.
        /// </summary>
        public static bool IsInCategory<TMarker>(this State state)
        {
            State current = state;
            while (current != null)
            {
                if (current is TMarker)
                    return true;

                if (current is SuperState super && super.TransitionHandler.CurrentState != null)
                    current = super.TransitionHandler.CurrentState;
                else
                    current = null;
            }
            return false;
        }

        /// <summary>
        /// Returns the active leaf state at the bottom of the hierarchy.
        /// </summary>
        public static State GetActiveLeaf(this State state)
        {
            State current = state;
            while (current is SuperState super && super.TransitionHandler.CurrentState != null)
                current = super.TransitionHandler.CurrentState;
            return current;
        }
    }
}
