﻿using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Kensai.AutonomousMovement {
    public class Cohesion2D : SteeringBehaviour2D {
        Vector2 centerOfMass = Vector2.zero;
        Vector2 steeringForce = Vector2.zero;

        void Reset() {
            if (World2D.Instance != null) {
                Weight = World2D.Instance.DefaultSettings.CohesionWeight;
                Probability = World2D.Instance.DefaultSettings.CohesionProb;
            }
        }

        public override Vector2 GetVelocity() {
            //return GetVelocity(agent, agent.Neighbors);

            Profiler.BeginSample("Cohesion2D");
            centerOfMass = Vector2.zero;
            steeringForce = Vector2.zero;
            foreach (var neighbor in agent.Neighbors) {
                if (neighbor == agent) continue;
                if (agent.TargetAgents.Contains(neighbor)) continue; //Ignore the target of evasion or pursue

                centerOfMass += neighbor.Rigidbody2D.position;
            }
            int neighborCount = agent.Neighbors.Count();
            if (neighborCount > 0) {
                centerOfMass /= (float)neighborCount;
                steeringForce = Seek2D.GetVelocity(agent, centerOfMass);
            }
            Profiler.EndSample();

            return steeringForce.normalized;
        }

        public static Vector2 GetVelocity(SteeringAgent2D agent, IEnumerable<SteeringAgent2D> neighbors) {
            Vector2 centerOfMass = Vector2.zero, steeringForce = Vector2.zero;
            foreach (var neighbor in neighbors) {
                if (neighbor == agent) continue;
                if (agent.TargetAgents.Contains(neighbor)) continue; //Ignore the target of evasion or pursue

                centerOfMass += neighbor.Rigidbody2D.position;
            }

            int neighborCount = neighbors.Count();
            if (neighborCount > 0) {
                centerOfMass /= (float)neighborCount;
                steeringForce = Seek2D.GetVelocity(agent, centerOfMass);
            }

            return steeringForce.normalized;
        }

        public override bool RequiresNeighborList {
            get {
                return true;
            }
        }

        public override int CalculationOrder {
            get { return 6; }
        }
    }
}
