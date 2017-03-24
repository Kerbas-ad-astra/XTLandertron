/* Copyright 2015 charfa.
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 * 
 * */

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
			Vector3d down = (vessel.mainBody.position - vessel.CoM).normalized;

			double projectedSpeed = Vector3d.Dot(vessel.srf_velocity, thrustDirection);
            if (projectedSpeed >= 0) // already stopped
                return false;

            double distanceToGround = calculateDistanceToGround(vessel, -thrustDirection);
			double distanceToGroundVert = calculateDistanceToGround(vessel, down);
            // Landertrons will start slowing the vessel down only on the next FixedUpdate,
            // so we check how far off the ground it will be then.
            distanceToGround += projectedSpeed * TimeWarp.fixedDeltaTime;
            // Actually, let's look two frames ahead, if it will be too late (vessel stops under ground)
            // we trigger Landertrons to start slowing us down one frame ahead.
            double nextFrameDistanceToGround = distanceToGround + projectedSpeed * TimeWarp.fixedDeltaTime;
			log.debug("Distance to ground: " + distanceToGround + ", next frame: " + nextFrameDistanceToGround + ", vertically: " + distanceToGroundVert);
            if (distanceToGround <= 0) // already on the ground
                return false;
            
            double gravity = FlightGlobals.currentMainBody.gravParameter / Math.Pow(FlightGlobals.currentMainBody.Radius, 2.0);
			double finalAcc = Vector3d.Dot(down, thrustDirection)*gravity + combinedThrust.magnitude / vessel.GetTotalMass();
			double predictedSpeed = Math.Sqrt(Math.Pow(projectedSpeed, 2.0) + 2 * distanceToGroundVert * gravity);
			log.debug("Projspeed: " + projectedSpeed + " predspeed: " + predictedSpeed);
			double timeToStop = predictedSpeed / finalAcc;
            double burnTime = getMinBurnTime(armedLandertrons);

			log.debug("Final acc: " + finalAcc + " time to stop: " + timeToStop + " burn time: " + burnTime);

            if (timeToStop < 0 || timeToStop > burnTime) // will never stop
                timeToStop = burnTime;

            double distanceToStop = Math.Abs(projectedSpeed * timeToStop + finalAcc * timeToStop * timeToStop / 2);
            log.debug("Distance to stop: " + distanceToStop);

            return distanceToStop > nextFrameDistanceToGround;
        }

        protected override bool shouldShutdownFiringLandertrons()
        {
            Vector3d thrustDirection = calculateCombinedThrust(firingLandertrons).normalized;
            // just for debugging
            double distanceToGround = calculateDistanceToGround(vessel, -thrustDirection);
            log.debug("Distance to ground: " + distanceToGround);
            // Similar to above, Landertrons will stop accellerating the vessel on next FixedUpdate,
            // so we check if the speed on next frame will be positive.
            // Next step in precision would be to check if speed on next frame is negative, but closer
            // to 0 than two frames ahead, but it doesn't matter as much here as it does when igniting.
            Vector3d predictedVelocity = vessel.srf_velocity + vessel.acceleration * TimeWarp.fixedDeltaTime;
            double predictedSpeed = Vector3d.Dot(predictedVelocity, thrustDirection);
            log.debug("Predicted speed = " + predictedSpeed);
            return predictedSpeed >= 0;
        }

        private double getMinBurnTime(List<Landertron> armedLandertrons)
        {
            double minBurnTime = double.PositiveInfinity;
            foreach (var landertron in armedLandertrons)
                minBurnTime = Math.Min(minBurnTime, landertron.engineBurnTime);
            return minBurnTime;
        }

        public double calculateDistanceToGround(Vessel vessel, Vector3d direction)
        {
			Vector3d position = vessel.CoM;
            RaycastHit hit;
            if (!Physics.Raycast(position, direction, out hit, float.PositiveInfinity, 1 << 15))
                return double.PositiveInfinity;

            double distanceToTerrain = hit.distance;

            double distanceToWater = double.PositiveInfinity;

            if (vessel.mainBody.ocean) {  // Do a little trig to see if/where our current trajectory will intersect the sea-level sphere (which the raycast doesn't detect).
                Vector3d down = (vessel.mainBody.position - vessel.CoM).normalized;
                double cosTheta = Vector3d.Dot (down, direction);
                double r = vessel.mainBody.Radius;
                double a = r + vessel.altitude;
                double discriminant = Math.Pow (a, 2) * Math.Pow (cosTheta, 2) - (Math.Pow (a, 2) - Math.Pow (r, 2)); //It's divided by a factor of four from the usual discriminant, because the discriminant will just get sqrted and divided by 2 anyway.
                if (discriminant >= 0) {
                    distanceToWater = a * cosTheta - Math.Sqrt (discriminant);
                }
            }

            double distanceToImpact = Math.Min (distanceToTerrain, distanceToWater);

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

            return distanceToImpact - maxExtent;
        }
    }
}