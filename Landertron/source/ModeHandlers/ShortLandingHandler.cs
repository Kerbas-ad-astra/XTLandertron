using System;
using System.Collections.Generic;

namespace Landertron
{
    class ShortLandingHandler: ModeHandlerBase
    {
        public ShortLandingHandler(Vessel vessel)
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
            Vector3d predictedVelocity = vessel.srf_velocity + vessel.acceleration * TimeWarp.fixedDeltaTime;
            double predictedSpeed = Vector3d.Dot(predictedVelocity, thrustDirection);
            log.debug("Predicted speed = " + predictedSpeed);
            if (predictedSpeed >= 0)
                return true;
            else
                return false;
        }
    }
}
