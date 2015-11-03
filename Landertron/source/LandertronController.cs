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
            SoftLandingHandler softLandingHandler = new SoftLandingHandler(vessel);

            foreach (var landertron in vessel.FindPartModulesImplementing<Landertron>())
            {
                switch (landertron.mode)
                {
                    case Landertron.Mode.SoftLanding:
                        softLandingHandler.addLandertron(landertron);
                        break;
                }
            }

            softLandingHandler.execute();
        }
    }
}
