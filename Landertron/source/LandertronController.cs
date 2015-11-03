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
    public class LandertronController : VesselModule
    {
        Vessel vessel;
        Logger log;

        public void Start()
        {
            vessel = GetComponent<Vessel>();
            log = new Logger("[LandertronController:" + vessel.id + "] ");
        }

        public void FixedUpdate()
        {
            List<Landertron> armedLandertrons = new List<Landertron>();
            List<Landertron> firingLandertrons =  new List<Landertron>();
            foreach (var landertron in vessel.FindPartModulesImplementing<Landertron>())
            {
                if (landertron.isArmed)
                    armedLandertrons.Add(landertron);
                else if (landertron.isFiring)
                    firingLandertrons.Add(landertron);
            }

            if (firingLandertrons.Count > 0 && shouldShutdownFiringLandertrons(vessel, firingLandertrons))
            {
                log.info("Shutting down Landertrons");
                foreach (var landertron in firingLandertrons)
                    landertron.shutdown();
            }
            else if (armedLandertrons.Count > 0 && shouldFireArmedLandertrons(vessel, armedLandertrons))
            {
                log.info("Firing Landertrons");
                foreach (var landertron in armedLandertrons)
                    landertron.fire();
            }
        }

        private bool shouldFireArmedLandertrons(Vessel vessel, List<Landertron> armedLandertrons)
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

        private bool shouldShutdownFiringLandertrons(Vessel vessel, List<Landertron> firingLandertrons)
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
            if (!Physics.Raycast(position, direction, out hit, float.PositiveInfinity, 1<<15))
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
