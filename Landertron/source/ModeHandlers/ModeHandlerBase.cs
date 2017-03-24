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
