using System;
using System.Collections.Generic;
using UnityEngine;

namespace Landertron
{
    class SoftLandingHandler: ModeHandlerBase
    {
        public SoftLandingHandler(Vessel vessel)
            : base(vessel)
        {
        }

        protected override bool shouldFireArmedLandertrons()
        {
            if (vessel.LandedOrSplashed)
                return false;

            Vector3d combinedThrust = calculateCombinedThrust(armedLandertrons);
            Vector3d thrustDirection = combinedThrust.normalized;

            double projectedSpeed = Vector3d.Dot(vessel.srf_velocity, thrustDirection);
            if (projectedSpeed >= 0) // already stopped
                return false;

            double distanceToGround = calculateDistanceToGround(vessel, -thrustDirection);
            distanceToGround += projectedSpeed * TimeWarp.fixedDeltaTime;
            log.debug("Predicted distance to ground: " + distanceToGround);
            if (distanceToGround < 0) // already on the ground
                return false;

            double finalAcc = Vector3d.Dot(vessel.acceleration, thrustDirection) + combinedThrust.magnitude / vessel.GetTotalMass();
            double timeToStop = -projectedSpeed / finalAcc;
            double burnTime = getMinBurnTime(armedLandertrons);
            if (timeToStop < 0 || timeToStop > burnTime) // will never stop
                timeToStop = burnTime;

            double distanceToStop = Math.Abs(projectedSpeed * timeToStop + finalAcc * timeToStop * timeToStop / 2);
            log.debug("Distance to stop: " + distanceToStop);

            return distanceToStop > distanceToGround;
        }

        protected override bool shouldShutdownFiringLandertrons()
        {
            Vector3d thrustDirection = calculateCombinedThrust(firingLandertrons).normalized;
            Vector3d predictedVelocity = vessel.srf_velocity + vessel.acceleration * TimeWarp.fixedDeltaTime;
            double predictedSpeed = Vector3d.Dot(predictedVelocity, thrustDirection);
            log.debug("Predicted speed = " + predictedSpeed);
            if (predictedSpeed >= 0)
                return true;
            else
                return false;
        }

        private Vector3d calculateCombinedThrust(List<Landertron> landertrons)
        {
            Vector3d result = Vector3d.zero;

            foreach (var landertron in landertrons)
            {
                result += landertron.engineThrust;
            }

            return result;
        }

        private double getMinBurnTime(List<Landertron> armedLandertrons)
        {
            double minBurnTime = double.PositiveInfinity;
            foreach (var landertron in armedLandertrons)
                minBurnTime = Math.Min(minBurnTime, landertron.engineBurnTime);
            return minBurnTime;
        }

        private double calculateDistanceToGround(Vessel vessel, Vector3d direction)
        {
            Vector3d position = vessel.findWorldCenterOfMass();
            RaycastHit hit;
            if (!Physics.Raycast(position, direction, out hit, float.PositiveInfinity, 1 << 15))
                return double.PositiveInfinity;

            Vector3d fakeInfinity = position + 1000 * direction;
            double maxExtent = 0;
            foreach (var part in vessel.parts)
            {
                if (part.collider != null)
                {
                    double extent = Vector3d.Dot(part.collider.ClosestPointOnBounds(fakeInfinity) - position, direction);
                    maxExtent = Math.Max(maxExtent, extent);
                }
            }

            return hit.distance - maxExtent;
        }
    }
}
