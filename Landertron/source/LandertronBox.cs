using System;
using System.Collections.Generic;
using UnityEngine;

namespace Landertron
{
    public class LandertronBox : Landertron
    {
        [KSPField]
        new public bool refuelable = false;

        override public Vector3d engineThrust
        {
            get
            {
                Vector3d sumVector = Vector3d.zero;
                foreach (var engine in engines) {
                    Vector3d vector = Vector3d.zero;
					for (int i = 0; i < engine.thrustTransforms.Count; i++) {
						vector -= engine.thrustTransforms [i].forward * engine.thrustTransformMultipliers [i];
					}
                    vector.Normalize ();
                    double isp = engine.atmosphereCurve.Evaluate ((float)engine.part.staticPressureAtm);
                    double fuelFlow = Mathf.Lerp (engine.minFuelFlow, engine.maxFuelFlow, engine.thrustPercentage / 100);
                    double thrust = fuelFlow * isp * engine.g;
                    sumVector += vector * thrust;
                }
                // In case there is more than one box on the vessel, return a fraction of the total
                // since they're being summed elsewhere and this is the easiest method of returning
                // the total overall.
                int boxes = 0;
                foreach (var p in vessel.FindPartModulesImplementing<LandertronBox>()) {
                    if (p.isArmed|p.isFiring)
                        boxes += 1;
                }
                sumVector = sumVector / boxes;
                log.debug ("LandertronBox: "+sumVector.magnitude.ToString()+" : "+boxes.ToString());
                return sumVector;
            }
        }

        override public double engineBurnTime
        {
            get
            {
                double result = double.PositiveInfinity;
                foreach (var engine in engines) {
                    double fuelFlow = Mathf.Lerp (engine.minFuelFlow, engine.maxFuelFlow, engine.thrustPercentage / 100);
                    foreach (var propellant in engine.propellants) {
                        if (propellant.ignoreForIsp)
                            continue;
                        double propFlow = fuelFlow * propellant.ratio / engine.ratioSum;
						if (propellant.totalResourceAvailable > 0) {
							double propDens = PartResourceLibrary.Instance.GetDefinition(propellant.id).density;
                            if (propDens <= 0)
                                continue;
                            double fuelMass = propellant.currentAmount * propDens;
                            result = Math.Min(result,fuelMass/propFlow);
                        } else {
                            return 0;
                        }
                    }
                }
                // Since the above seems to be giving too small a number, it's up to the user to ensure sufficient propellant
                result = double.PositiveInfinity;
                return result;
            }
        }

        //override public double engineFuelFlow
        //{
        //    get
        //    {
        //        double result = 0;
        //        foreach (var engine in engines) {
        //            result += Mathf.Lerp (engine.minFuelFlow, engine.maxFuelFlow, engine.thrustPercentage / 100);
        //        }
        //        return result;
        //    }
        //}

        List<ModuleEngines> engines = new List<ModuleEngines>();
        Logger log = new Logger("[Landertron] ");

        public override void OnStart(PartModule.StartState state)
        {
            if(!(status == Status.Idle))
                checkEngines ();
        }

        public override void OnUpdate()
        {
        }

        private void checkEngines ()
        {
            log.prefix = "[Landertron:" + part.flightID + "] ";
            //log.debug("Checking for engines.");
            engines.Clear ();
            foreach (var p in vessel.parts) {
            //foreach (var p in vessel.FindPartModulesImplementing<ModuleEngines>()){
                ModuleEngines engine = new ModuleEngines();
                if (!p.Modules.Contains ("Landertron")) {
                    if (p.Modules.Contains ("ModuleEngines")) {
                        engine = p.Modules ["ModuleEngines"] as ModuleEngines;
                    } else if (p.Modules.Contains ("ModuleEnginesFX")) {
                        engine = p.Modules ["ModuleEnginesFX"] as ModuleEngines;
                    } else if (p.Modules.Contains ("ModuleEnginesRF")) {
                        engine = p.Modules ["ModuleEnginesRF"] as ModuleEngines;
                    } else {
                        continue;
                    }
                }
                if (engine.EngineIgnited) {
                    //log.debug ("Added engine." + engine.maxThrust.ToString());
                    if (!engine.flameout) {
                        engines.Add (engine);
                    }
                }
            }

            //if (engines.Count == 0)
            //    log.error("No engine found! Will crash!");
        }

        public override void OnFixedUpdate()
        {
            if (!(status == Status.Idle)) {
                checkEngines ();
                if (engines.Count <= 0) {
                    if (status == Status.Armed)
                        ScreenMessages.PostScreenMessage ("Landertron could not find an active engine, disarming!", 5, ScreenMessageStyle.UPPER_CENTER);
                    status = Status.Empty;
                } else if (status == Status.Empty) {
                    status = Status.Idle;
                }
            }

            if (status == Status.Armed)
            {
                float electricReq = electricRate * TimeWarp.fixedDeltaTime;
                if (part.RequestResource("ElectricCharge", electricReq) < electricReq)
                {
                    disarm();
                    ScreenMessages.PostScreenMessage("Landertron out of electric charge, disarming!", 5, ScreenMessageStyle.UPPER_CENTER);
                }
            }
            // If the StayPut mode has been firing for more than 3 seconds, stop firing.
            else if ((mode == Mode.StayPut) && (status == Status.Firing) && (Planetarium.GetUniversalTime () > fireTime + 3))
                shutdown ();
            else if (status == Status.Firing) {
                throttle = 1.0f;
                vessel.OnFlyByWire += setThrottle;
            }
        }
        
        float throttle = 0.0f;
        double fireTime = 0.0f;

        override internal void fire()
        {
            log.prefix = "[Landertron:" + part.flightID + "] ";
            log.debug("Firing engines");
            status = Status.Firing;
            throttle = 1.0f;
            vessel.OnFlyByWire += setThrottle;
            if (mode == Mode.StayPut)
                fireTime = Planetarium.GetUniversalTime();
        }

        void setThrottle(FlightCtrlState s){
            if(status == Status.Firing)
                s.mainThrottle = throttle;
        }

        override internal void shutdown()
        {
            log.prefix = "[Landertron:" + part.flightID + "] ";
            log.debug("Shutting down engines");
            throttle = 0.0f;
            vessel.OnFlyByWire += setThrottle;
            foreach (var engine in engines) {
                if (engine.throttleLocked) {
                    engine.Shutdown ();
                }
            }
            double alt = new SoftLandingHandler (vessel).calculateDistanceToGround (vessel, this.engineThrust.normalized);
            if ((mode == Mode.SoftLanding) && (!vessel.LandedOrSplashed)) {
                if (alt * vessel.mainBody.GeeASL * 9.81 * 2 > Math.Sqrt(8)) {
                    status = Status.Armed;
                } else {
                    status = Status.Idle;
                }
            } else {
                status = Status.Idle;
            }
        }
    }
}

