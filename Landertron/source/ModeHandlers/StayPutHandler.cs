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
            RaycastHit surface;
            Physics.Raycast(vessel.CoM, down, out surface, float.PositiveInfinity, 1 << 15);
            return Vector3d.Dot(thrustDirection, surface.normal) >= 0;
        }
    }
}
