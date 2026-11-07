// Copyright (c) 2024-2026 Ahmed Abdalla. All rights reserved. See LICENSE in the repository root.

using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Dragonveil.StateMachine.Tests
{
    /// <summary>
    /// Ground (super state: Idle → Run) and Air, the classic character-controller shape.
    /// </summary>
    public class SuperStateTests
    {
        private GameObject gameObject;
        private TestStateController controller;
        private List<string> log;
        private RecordingSuperState ground;
        private RecordingState idle, run, air;
        private bool moving, airborne;

        [SetUp]
        public void SetUp()
        {
            log = new List<string>();
            moving = false;
            airborne = false;

            gameObject = new GameObject("SuperStateTest");
            gameObject.SetActive(false);
            controller = gameObject.AddComponent<TestStateController>();

            idle = new RecordingState("Idle", log);
            run = new RecordingState("Run", log);
            air = new RecordingState("Air", log);
            ground = new RecordingSuperState(controller, "Ground", log) { InitialSubState = idle };
            ground.TransitionHandler.CreateTransition(idle, run, () => moving);

            controller.InitialState = ground;
            controller.BuildTransitions = c => c.CreateTransition(ground, air, () => airborne);
        }

        [TearDown]
        public void TearDown() => Object.Destroy(gameObject);

        [UnityTest]
        public IEnumerator Entering_EntersSuperStateThenItsInitialSubState()
        {
            gameObject.SetActive(true);
            yield return null;

            Assert.AreEqual(new[] { "Ground.Enter", "Idle.Enter" }, log.GetRange(0, 2).ToArray());
            Assert.AreSame(idle, controller.CurrentState.GetActiveLeaf());
        }

        [UnityTest]
        public IEnumerator SubStates_TransitionInsideTheSuperState()
        {
            gameObject.SetActive(true);
            yield return null;
            log.Clear();

            moving = true;
            yield return null;

            Assert.AreSame(ground, controller.CurrentState, "The root state must not change for a sub-state transition.");
            Assert.AreSame(run, controller.CurrentState.GetActiveLeaf());
            Assert.AreEqual(new[] { "Idle.Exit", "Run.Enter", "Run.Do" }, log.WithoutPhysicsSteps());
        }

        [UnityTest]
        public IEnumerator LeavingTheSuperState_ExitsTheSubStateFirst()
        {
            gameObject.SetActive(true);
            moving = true;
            yield return null;
            yield return null;
            log.Clear();

            airborne = true;
            yield return null;

            Assert.AreSame(air, controller.CurrentState);
            Assert.AreEqual(new[] { "Run.Exit", "Ground.Exit", "Air.Enter", "Air.Do" }, log.WithoutPhysicsSteps());
        }

        [UnityTest]
        public IEnumerator IsInCategory_LooksThroughTheActiveHierarchy()
        {
            gameObject.SetActive(true);
            yield return null;
            Assert.IsTrue(controller.CurrentState.IsInCategory<IGroundedState>());

            airborne = true;
            yield return null;
            Assert.IsFalse(controller.CurrentState.IsInCategory<IGroundedState>());
        }

        // Regression: a sub-state transition ready on entry used to exit Idle without ever entering it.
        [UnityTest]
        public IEnumerator EnteringWithASubTransitionReady_EntersTheInitialSubStateFirst()
        {
            moving = true;

            gameObject.SetActive(true);
            yield return null;

            Assert.AreEqual(new[] { "Ground.Enter", "Idle.Enter", "Idle.Exit", "Run.Enter", "Run.Do" }, log.WithoutPhysicsSteps());
        }

        // Regression: sub-state changes used to fire the controller's root StateChanged event.
        [UnityTest]
        public IEnumerator SubStateChanges_AreReportedByTheSuperStateNotTheController()
        {
            var rootChanges = new List<string>();
            var subChanges = new List<string>();
            controller.StateChanged += (from, to) => rootChanges.Add($"{from}->{to}");
            ground.TransitionHandler.StateChanged += (from, to) => subChanges.Add($"{from}->{to}");

            gameObject.SetActive(true);
            yield return null;
            moving = true;
            yield return null;
            airborne = true;
            yield return null;

            Assert.AreEqual(new[] { "Idle->Run" }, subChanges.ToArray());
            Assert.AreEqual(new[] { "Ground->Air" }, rootChanges.ToArray());
        }

        // Regression: disabling during a delayed sub-state transition used to leave the super state stuck.
        [UnityTest]
        public IEnumerator DisablingDuringASubStateTransition_DoesNotLeaveItStuck()
        {
            var crouch = new RecordingState("Crouch", log);
            bool crouchPressed = true;
            ground.TransitionHandler.CreateTransition(idle, crouch, () => crouchPressed, () => { }, transitionTimeInSeconds: 0.2f, priority: 1);

            gameObject.SetActive(true);
            yield return null;
            Assert.IsTrue(ground.TransitionHandler.IsTransitioning);

            gameObject.SetActive(false);
            crouchPressed = false;
            gameObject.SetActive(true);
            yield return new WaitForSeconds(0.4f);

            Assert.IsFalse(ground.TransitionHandler.IsTransitioning);
            Assert.AreSame(idle, controller.CurrentState.GetActiveLeaf());
            CollectionAssert.DoesNotContain(log, "Crouch.Enter");
            Assert.AreEqual(1, log.FindAll(entry => entry == "Idle.Exit").Count, "Idle is not exited twice.");
            CollectionAssert.Contains(log, "Idle.Do", "Idle ticks again after re-enabling.");
        }
    }
}
