using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Dragonveil.StateMachine.Tests
{
    public class StateControllerTests
    {
        private GameObject gameObject;
        private TestStateController controller;
        private List<string> log;

        [SetUp]
        public void SetUp()
        {
            log = new List<string>();
            gameObject = new GameObject("StateControllerTest");
            gameObject.SetActive(false); // so Awake runs only after the test has configured the controller
            controller = gameObject.AddComponent<TestStateController>();
        }

        [TearDown]
        public void TearDown() => Object.Destroy(gameObject);

        [UnityTest]
        public IEnumerator EntersAwakeState_WhenEnabled()
        {
            var idle = new RecordingState("Idle", log);
            controller.InitialState = idle;

            gameObject.SetActive(true);
            yield return null;

            Assert.AreSame(idle, controller.CurrentState);
            Assert.AreEqual("Idle.Enter", log[0]);
            CollectionAssert.Contains(log, "Idle.Do");
        }

        [UnityTest]
        public IEnumerator Transition_ExitsOldStateBeforeEnteringNewOne()
        {
            var idle = new RecordingState("Idle", log);
            var run = new RecordingState("Run", log);
            bool moving = false;
            State changedFrom = null, changedTo = null;
            controller.InitialState = idle;
            controller.BuildTransitions = c => c.CreateTransition(idle, run, () => moving);
            controller.StateChanged += (from, to) => { changedFrom = from; changedTo = to; };

            gameObject.SetActive(true);
            yield return null;
            log.Clear();

            moving = true;
            yield return null;

            Assert.AreSame(run, controller.CurrentState);
            Assert.AreEqual(new[] { "Idle.Exit", "Run.Enter", "Run.Do" }, log.WithoutPhysicsSteps());
            Assert.AreSame(idle, changedFrom);
            Assert.AreSame(run, changedTo);
        }

        [UnityTest]
        public IEnumerator HigherPriorityTransition_Wins()
        {
            var idle = new RecordingState("Idle", log);
            var run = new RecordingState("Run", log);
            var jump = new RecordingState("Jump", log);
            controller.InitialState = idle;
            controller.BuildTransitions = c =>
            {
                c.CreateTransition(idle, run, () => true);
                c.CreateTransition(idle, jump, () => true, priority: 5);
            };

            gameObject.SetActive(true);
            yield return null;

            Assert.AreSame(jump, controller.CurrentState);
        }

        [UnityTest]
        public IEnumerator EqualPriorityTransitions_KeepRegistrationOrder()
        {
            var idle = new RecordingState("Idle", log);
            var run = new RecordingState("Run", log);
            var jump = new RecordingState("Jump", log);
            controller.InitialState = idle;
            controller.BuildTransitions = c =>
            {
                c.CreateTransition(idle, run, () => true);
                c.CreateTransition(idle, jump, () => true);
            };

            gameObject.SetActive(true);
            yield return null;

            Assert.AreSame(run, controller.CurrentState);
        }

        [UnityTest]
        public IEnumerator TwoWayTransition_GoesThereAndBack()
        {
            var ground = new RecordingState("Ground", log);
            var air = new RecordingState("Air", log);
            bool airborne = false;
            controller.InitialState = ground;
            controller.BuildTransitions = c => c.CreateTwoWayTransition(ground, air, () => airborne, () => !airborne);

            gameObject.SetActive(true);
            yield return null;

            airborne = true;
            yield return null;
            Assert.AreSame(air, controller.CurrentState);

            airborne = false;
            yield return null;
            Assert.AreSame(ground, controller.CurrentState);
        }

        [UnityTest]
        public IEnumerator DelayedTransition_RunsItsStepThenEntersAfterTheDelay()
        {
            var idle = new RecordingState("Idle", log);
            var dash = new RecordingState("Dash", log);
            int stepCalls = 0;
            controller.InitialState = idle;
            controller.BuildTransitions = c => c.CreateTransition(idle, dash, () => true, () => stepCalls++, transitionTimeInSeconds: 0.1f);

            gameObject.SetActive(true);
            yield return null;

            Assert.IsTrue(controller.IsTransitioning);
            Assert.AreEqual(1, stepCalls);
            CollectionAssert.DoesNotContain(log, "Dash.Enter");

            yield return new WaitForSeconds(0.25f);

            Assert.IsFalse(controller.IsTransitioning);
            Assert.AreSame(dash, controller.CurrentState);
            CollectionAssert.Contains(log, "Dash.Enter");
            CollectionAssert.DoesNotContain(log, "Idle.Do", "The old state must not tick while a transition is running.");
        }

        [UnityTest]
        public IEnumerator FixedDo_RunsOnThePhysicsStep()
        {
            controller.InitialState = new RecordingState("Idle", log);

            gameObject.SetActive(true);
            yield return new WaitForFixedUpdate();
            yield return null;

            CollectionAssert.Contains(log, "Idle.FixedDo");
        }

        [UnityTest]
        public IEnumerator Disabling_ExitsTheCurrentState()
        {
            controller.InitialState = new RecordingState("Idle", log);

            gameObject.SetActive(true);
            yield return null;
            gameObject.SetActive(false);

            Assert.AreEqual("Idle.Exit", log[log.Count - 1]);
        }

        [UnityTest]
        public IEnumerator WithoutTickAuthority_StateIsNotEnteredUntilAuthorityArrives()
        {
            controller.InitialState = new RecordingState("Idle", log);
            controller.HasAuthority = false;

            gameObject.SetActive(true);
            yield return null;
            CollectionAssert.IsEmpty(log, "A controller without authority must not enter or tick its state.");

            controller.HasAuthority = true;
            yield return null;
            Assert.AreEqual("Idle.Enter", log[0]);

            controller.HasAuthority = false;
            yield return null;
            Assert.AreEqual("Idle.Exit", log[log.Count - 1]);
        }
    }
}
