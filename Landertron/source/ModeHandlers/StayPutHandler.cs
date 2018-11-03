/* Copyright 2015-2017 charfa, Kerbas_ad_astra.
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
    class StayPutHandler: ModeHandlerBase
    {
        public StayPutHandler(Vessel vessel)
            : base(vessel)
        {
        }

        protected override bool shouldFireArmedLandertrons()
        {
            return vessel.Landed;
        }

        protected override bool shouldShutdownFiringLandertrons()
        {
            Vector3d thrustDirection = calculateCombinedThrust(firingLandertrons).normalized;
            Vector3d down = (vessel.mainBody.position - vessel.CoM).normalized;
            //RaycastHit surface;
            Physics.Raycast(vessel.CoM, down, out RaycastHit surface, float.PositiveInfinity, 1 << 15);
            return Vector3d.Dot(thrustDirection, surface.normal) >= 0;
        }
    }
}
