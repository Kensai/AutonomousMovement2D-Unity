﻿using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Kensai.Util.Extensions;

namespace Kensai.AutonomousMovement {
    [RequireComponent(typeof(SteeringAgent2D))]
    public abstract class SteeringBehaviour2D : MonoBehaviour {
        public float Weight = 1;
        public float Probability = 1;
        protected SteeringAgent2D agent;

        public abstract int CalculationOrder {
            get;
        }

        public abstract Vector2 GetVelocity();

        public virtual bool RequiresNeighborList {
            get { return false; }
        }

        void Awake() {
            agent = GetComponent<SteeringAgent2D>();
        }

        void Start() {
            agent.RegisterSteeringBehaviour(this);
        }

        void OnDestroy() {
            if (agent != null) { 
                agent.DeregisterSteeringBehaviour(this);
            }
        }
    }

    public static class SteeringBehaviourExtensions {
        public static Vector2 CalculateCompound(
            this IEnumerable<SteeringBehaviour2D> steeringBehaviours, 
            float MaxForce,
            SteeringCombinationType type = SteeringCombinationType.WeightedSum) {

            if (steeringBehaviours == null || steeringBehaviours.Count() == 0) return Vector2.zero;

            switch (type) {
                case SteeringCombinationType.WeightedSum:
                    return WeightedTruncatedSum(steeringBehaviours, MaxForce);
                case SteeringCombinationType.PrioritizedWeightedSum:
                    return PrioritizedWeightedTruncatedSum(steeringBehaviours, MaxForce);
                case SteeringCombinationType.PrioritizedDithering:
                    return PrioritizedDithering(steeringBehaviours, MaxForce);
                //TODO-> Time slicing
                //case SteeringCombinationType.TimeSlicing:
                //    steeringForce = TimeSlicing(steeringBehaviours, MaxForce);
                //    break;
                default:
                    return Vector2.zero;
            }
        }

        private static Vector2 WeightedTruncatedSum(IEnumerable<SteeringBehaviour2D> steeringBehaviours, float MaxForce) {
            var steeringForce = Vector2.zero;
            foreach (var behaviour in steeringBehaviours) {
                if (!behaviour.enabled) continue;
                var steeringForceTweaker = World2D.Instance.DefaultSettings.SteeringForceTweaker;
                steeringForce += behaviour.GetVelocity() * behaviour.Weight * steeringForceTweaker;
            }
            steeringForce = steeringForce.Truncate(MaxForce);
            return steeringForce;
        }

        private static Vector2 PrioritizedWeightedTruncatedSum(IEnumerable<SteeringBehaviour2D> steeringBehaviours, float MaxForce) {
            Profiler.BeginSample("Preparation.");
            var steeringForce = Vector2.zero;
            float steeringForceMagnitude = 0;

            steeringBehaviours = steeringBehaviours.OrderBy(sb => sb.CalculationOrder);
            Profiler.EndSample();

            Profiler.BeginSample("Main steering sum calculation.");
            foreach (var behaviour in steeringBehaviours) {
                if (!behaviour.enabled) continue;
                var steeringForceTweaker = World2D.Instance.DefaultSettings.SteeringForceTweaker;

                Profiler.BeginSample("Behavior calculation.");
                var behaviorForce = behaviour.GetVelocity() * behaviour.Weight * steeringForceTweaker;
                Profiler.EndSample();
                var behaviorForceMagnitude = behaviorForce.magnitude;
                if (steeringForceMagnitude + behaviorForceMagnitude < MaxForce) {
                    steeringForce += behaviorForce;
                } else {
                    steeringForce += behaviorForce.Truncate(MaxForce - steeringForceMagnitude);
                    break;
                }
                steeringForceMagnitude = steeringForce.magnitude;
            }
            Profiler.EndSample();
            return steeringForce;
        }

        //TODO-> Revise and see why the behaviour is so erratic
        private static Vector2 PrioritizedDithering(IEnumerable<SteeringBehaviour2D> steeringBehaviours, float MaxForce) {
            Vector2 steeringForce = Vector2.zero;
            var randomizer = new System.Random();

            steeringBehaviours = steeringBehaviours.OrderBy(sb => sb.CalculationOrder);
            foreach (var behaviour in steeringBehaviours) {
                if (!behaviour.enabled) continue;
                var randomNumber = (float)randomizer.NextDouble();
                if (randomNumber < behaviour.Probability) {
                    var steeringForceTweaker = World2D.Instance.DefaultSettings.SteeringForceTweaker;
                    var behaviorForce = behaviour.GetVelocity() * behaviour.Weight * steeringForceTweaker / behaviour.Probability;
                    if (behaviorForce.magnitude != 0) {
                        steeringForce = behaviorForce;
                        break;
                    }
                }
            }
            return steeringForce.Truncate(MaxForce);
        }

        public enum SteeringCombinationType {
            WeightedSum,
            PrioritizedWeightedSum,
            PrioritizedDithering,
            TimeSlicing
        }
    }
}
