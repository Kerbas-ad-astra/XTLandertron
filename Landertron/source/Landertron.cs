/* Copyright 2015 XanderTek (contributions by TheDog & Kerbas_ad_astra).
 * Copyright 2015 charfa
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

    public class Landertron : PartModule
    {
        public enum Mode
        {
            SoftLanding,
            ShortLanding,
            StayPut
        }

        public enum Status
        {
            Idle,
            Armed,
            Firing,
            Empty
        }

        [KSPField]
        public bool refuelable = true;

        [KSPField]
        public float electricRate = 0.05f;

        [KSPField(isPersistant = false, guiActive = true, guiName = "Status")]
        public string displayStatus = "Idle";

        [KSPField(isPersistant = false)]
        public string animationName;

        // KSPField doesn't like enums or properties so this will be persisted in OnLoad/OnSave.
        Mode _mode = Mode.SoftLanding;
        public Mode mode
        {
            get
            {
                return _mode;
            }
            private set
            {
                if (_mode != value)
                    setMode(value);
            }
        }

        // KSPField doesn't like enums or properties so this will be persisted in OnLoad/OnSave.
        Status _status = Status.Idle;
        public Status status
        {
            get
            {
                return _status;
            }
            private set
            {
                if (_status != value)
                    setStatus(value);
            }
        }

        public bool isArmed
        {
            get
            {
                return _status == Status.Armed;
            }
        }

        public bool isFiring
        {
            get
            {
                return _status == Status.Firing;
            }
        }

        public Vector3d engineThrust
        {
            get
            {
                Vector3d vector = Vector3d.zero;
                foreach (var transform in engine.thrustTransforms)
                    vector -= transform.forward;
                vector.Normalize();
                double isp = engine.atmosphereCurve.Evaluate((float)engine.part.staticPressureAtm);
                double fuelFlow = Mathf.Lerp(engine.minFuelFlow, engine.maxFuelFlow, engine.thrustPercentage / 100);
                double thrust = fuelFlow * isp * engine.g;
                return vector * thrust;
            }
        }

        public double engineBurnTime
        {
            get
            {
                double fuelFlow = Mathf.Lerp(engine.minFuelFlow, engine.maxFuelFlow, engine.thrustPercentage / 100);
                double fuelMass = solidFuel.amount * solidFuel.info.density;
                return fuelMass / fuelFlow;
            }
        }

        ModuleEngines engine;
        PartResource solidFuel;
        AnimationState[] animStates;
        bool animDeployed = false;
        Logger log = new Logger("[Landertron] ");

        [KSPEvent(guiName = "Mode: ", guiActiveEditor = true)]
        public void nextMode()
        {
            switch (mode)
            {
                case Mode.SoftLanding:
                    mode = Mode.ShortLanding;
                    break;
                case Mode.ShortLanding:
                    mode = Mode.StayPut;
                    break;
                case Mode.StayPut:
                    mode = Mode.SoftLanding;
                    break;
            }
            forAllSym();
        }

        [KSPEvent(guiName = "Arm", guiActive = true)]
        public void arm()
        {
            if (status != Status.Idle)
                throw new InvalidOperationException("Can only be armed when Idle, was " + status.ToString());
            animDeployed = true;
            status = Status.Armed;
            part.force_activate();
        }
        [KSPAction("Arm")]
        public void armAction(KSPActionParam param)
        {
            arm();
        }

        [KSPEvent(guiName = "Disarm", guiActive = true)]
        public void disarm()
        {
            if (status != Status.Armed)
                throw new InvalidOperationException("Can only be disarmed when Armed, was " + status.ToString());
            animDeployed = false;
            status = Status.Idle;
        }
        [KSPAction("Disarm")]
        public void disarmAction(KSPActionParam param)
        {
            disarm();
        }

        [KSPEvent(guiName = "Decouple", guiActive = true, guiActiveUnfocused = true, externalToEVAOnly = true, unfocusedRange = 3.0f)]
        public void decouple()
        {
            part.decouple(2000);
        }
        [KSPAction("Decouple")]
        public void decoupleAction(KSPActionParam param)
        {
            decouple();
        }

        public override void OnLoad(ConfigNode node)
        {
            if (node.HasValue("mode"))
            {   // if mode is set in config (usually in craft or persistence) load from it
                setMode((Mode)ConfigNode.ParseEnum(typeof(Mode), node.GetValue("mode")));
            }
            else if (HighLogic.LoadedSceneIsEditor)
            {   // else try to guess the mode based on VAB vs SPH
                if (HighLogic.CurrentGame.editorFacility == EditorFacility.VAB)
                {
                    setMode(Mode.SoftLanding);
                }
                else if (HighLogic.CurrentGame.editorFacility == EditorFacility.SPH)
                {
                    setMode(Mode.ShortLanding);
                }
            }
            if (node.HasValue("status"))
            {
                setStatus((Status)ConfigNode.ParseEnum(typeof(Status), node.GetValue("status")));
            }
        }

        public override void OnSave(ConfigNode node)
        {
            node.SetValue("mode", mode.ToString(), true);
            node.SetValue("status", status.ToString(), true);
        }

        public override void OnStart(PartModule.StartState state)
        {
            log.prefix = "[Landertron:" + part.flightID + "] ";

            engine = part.Modules["ModuleEngines"] as ModuleEngines;
            if (engine == null)
                engine = part.Modules["ModuleEnginesFX"] as ModuleEngines;
            if (engine == null)
                engine = part.Modules["ModuleEnginesRF"] as ModuleEngines;
            if (engine == null)
                log.error("No engine found! Will crash!");
            engine.manuallyOverridden = true;
            solidFuel = part.Resources["SolidFuel"];
            animStates = setUpAnimation(animationName, part);
        }

        public override void OnActive()
        {
            if (status == Status.Idle)
                arm();
        }

        public override void OnUpdate()
        {
            if (refuelable)
            {
                refuel();
            }

            updateAnimation();
        }

        public override void OnFixedUpdate()
        {
            if (status != Status.Empty)
            {
                if (solidFuel.amount == 0)
                {
                    shutdown();
                    status = Status.Empty;
                }
            }
            if (status == Status.Armed)
            {
                float electricReq = electricRate * TimeWarp.fixedDeltaTime;
                if (part.RequestResource("ElectricCharge", electricReq) < electricReq)
                {
                    disarm();
                    ScreenMessages.PostScreenMessage("Landertron ouf of electric charge, disarming!", 5, ScreenMessageStyle.UPPER_CENTER);
                }
            }
        }

        internal void fire()
        {
            log.debug("Firing engine");
            engine.Activate();
            status = Status.Firing;
        }

        internal void shutdown()
        {
            if (engine.allowShutdown)
            {
                engine.Shutdown();
            }
            else
            {
                ventFuel();
                engine.allowShutdown = true;
                engine.Shutdown();
                engine.allowShutdown = false;
            }
        }

        private void ventFuel()
        {
            log.debug("Venting fuel: " + solidFuel.amount);
            solidFuel.amount = 0;
            status = Status.Empty;
        }

        private void setMode(Mode value)
        {
            _mode = value;
            Events["nextMode"].guiName = "Mode: " + mode.ToString();
        }

        private void setStatus(Status value)
        {
            _status = value;
            log.info("Status set to " + _status.ToString());
            switch (_status)
            {
                case Status.Idle:
                    displayStatus = "Idle";
                    part.stackIcon.SetIconColor(XKCDColors.White);
                    Events["arm"].active = true;
                    Events["disarm"].active = false;
                    Actions["armAction"].active = true;
                    Actions["disarmAction"].active = false;
                    break;
                case Status.Armed:
                    displayStatus = "Armed";
                    part.stackIcon.SetIconColor(XKCDColors.LightCyan);
                    Events["arm"].active = false;
                    Events["disarm"].active = true;
                    Actions["armAction"].active = false;
                    Actions["disarmAction"].active = true;
                    break;
                case Status.Firing:
                    displayStatus = "Firing!";
                    part.stackIcon.SetIconColor(XKCDColors.RadioactiveGreen);
                    Events["arm"].active = false;
                    Events["disarm"].active = false;
                    Actions["armAction"].active = false;
                    Actions["disarmAction"].active = false;
                    break;
                case Status.Empty:
                    displayStatus = "Empty";
                    part.stackIcon.SetIconColor(XKCDColors.DarkGrey);
                    Events["arm"].active = false;
                    Events["disarm"].active = false;
                    Actions["armAction"].active = false;
                    Actions["disarmAction"].active = false;
                    break;
            }
        }

        private void refuel()
        {
            bool justrefueled = false;
            for (int i = 0; i < part.children.Count; )
            {
                Part p = part.children[i];
                if (p.Resources.Contains("SolidFuel") && solidFuel.amount < solidFuel.maxAmount)
                {
                    PartResource sfp = p.Resources["SolidFuel"];
                    solidFuel.amount = Math.Min(solidFuel.amount + sfp.amount, solidFuel.maxAmount);
                    p.Die();
                    justrefueled = true;
                }
                else
                {
                    ++i;
                }
            }
            if (justrefueled)
            {
                part.deactivate();
                part.inverseStage = 0;
                animDeployed = false;
                status = Status.Idle;
            }
        }

        private void forAllSym()
        {
            foreach (Part p in part.symmetryCounterparts)
            {
                Landertron ltron = p.Modules["Landertron"] as Landertron;
                ltron.mode = mode;
            }
        }

        private static AnimationState[] setUpAnimation(string animationName, Part part)  //Thanks Majiir!
        {
            var states = new List<AnimationState>();
            foreach (var animation in part.FindModelAnimators(animationName))
            {
                var animationState = animation[animationName];
                animationState.speed = 0;
                animationState.enabled = true;
                animationState.wrapMode = WrapMode.ClampForever;
                animation.Blend(animationName);
                states.Add(animationState);
            }
            return states.ToArray();
        }

        private void updateAnimation()
        {
            foreach (AnimationState anim in animStates)
            {
                if (animDeployed)
                {
                    if (anim.normalizedTime < 1)
                    {
                        anim.speed = 1;
                    }
                    else
                    {
                        anim.speed = 0;
                        anim.normalizedTime = 1;
                    }
                }
                else
                {
                    if (anim.normalizedTime > 0)
                    {
                        anim.speed = -1;
                    }
                    else
                    {
                        anim.speed = 0;
                        anim.normalizedTime = 0;
                    }
                }
            }
        }
    }
}
