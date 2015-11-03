using System;
using System.Collections.Generic;

namespace Landertron
{
    abstract class ModeHandlerBase
    {
        protected Logger log;
        protected Vessel vessel;
        protected List<Landertron> armedLandertrons = new List<Landertron>();
        protected List<Landertron> firingLandertrons = new List<Landertron>();

        protected ModeHandlerBase(Vessel vessel)
        {
            this.vessel = vessel;
            log = new Logger("[Landertron " + GetType().Name + ":" + vessel.id + "] ");
        }

        public void addLandertron(Landertron landertron)
        {
            if (landertron.isArmed)
                armedLandertrons.Add(landertron);
            else if (landertron.isFiring)
                firingLandertrons.Add(landertron);
        }

        public void removeLandertron(Landertron landertron)
        {
            armedLandertrons.Remove(landertron);
            firingLandertrons.Remove(landertron);
        }

        public void execute()
        {
            if (firingLandertrons.Count > 0 && shouldShutdownFiringLandertrons())
            {
                log.info("Shutting down Landertrons");
                foreach (var landertron in firingLandertrons)
                    landertron.shutdown();
            }
            else if (armedLandertrons.Count > 0 && shouldFireArmedLandertrons())
            {
                log.info("Firing Landertrons");
                foreach (var landertron in armedLandertrons)
                    landertron.fire();
            }
        }

        protected abstract bool shouldShutdownFiringLandertrons();

        protected abstract bool shouldFireArmedLandertrons();

        protected Vector3d calculateCombinedThrust(List<Landertron> landertrons)
        {
            Vector3d result = Vector3d.zero;

            foreach (var landertron in landertrons)
            {
                result += landertron.engineThrust;
            }

            return result;
        }
    }
}
