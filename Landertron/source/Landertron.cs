/* Copyright 2015 XanderTek (contributions by TheDog & Kerbas_ad_astra).
 * Copyright 2015-2017 charfa, Kerbas_ad_astra.
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
using KSP.Localization;

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
            protected set
            {
                if (_mode != value)
                    setMode(value);
            }
        }

        // KSPField doesn't like enums or properties so this will be persisted in OnLoad/OnSave.
        //Status _status = Status.Idle;
        public Status _status = Status.Idle;
        virtual public Status status
        {
            get
            {
                return _status;
            }
            protected set
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

        virtual public Vector3d engineThrust
        {
            get
            {
                Vector3d vector = Vector3d.zero;
                for (int i = 0; i < engine.thrustTransforms.Count; i++)
                {
                    vector -= engine.thrustTransforms[i].forward * engine.thrustTransformMultipliers[i];
                }
                vector.Normalize();
                double isp = engine.atmosphereCurve.Evaluate((float)engine.part.staticPressureAtm);
                double fuelFlow = Mathf.Lerp(engine.minFuelFlow, engine.maxFuelFlow, engine.thrustPercentage / 100);
                double thrust = fuelFlow * isp * engine.g;
                return vector * thrust;
            }
        }

        virtual public double engineBurnTime
        {
            get
            {
                double fuelFlow = Mathf.Lerp(engine.minFuelFlow, engine.maxFuelFlow, engine.thrustPercentage / 100);
                double fuelMass = propellantResource.amount * propellantResource.info.density;
                return fuelMass / fuelFlow;
            }
        }

        //virtual public double engineFuelFlow
        //{
        //    get
        //    {
        //        return Mathf.Lerp(engine.minFuelFlow, engine.maxFuelFlow, engine.thrustPercentage / 100);
        //    }
        //}

        ModuleEngines engine;
        PartResource propellantResource;
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
                throw new InvalidOperationException(Localizer.Format("#autoLOC_XTL_invalid_arm", statusString(status)));
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
                throw new InvalidOperationException(Localizer.Format("#autoLOC_XTL_invalid_disarm", statusString(status)));
            status = Status.Idle;
        }
        [KSPAction("Disarm")]
        public void disarmAction(KSPActionParam param)
        {
            disarm();
        }

        public override void OnAwake()
        {
            switch (EditorDriver.editorFacility)
            {
                case EditorFacility.SPH:
                    setMode(Mode.ShortLanding);
                    break;
                case EditorFacility.VAB:
                    setMode(Mode.SoftLanding);
                    break;
                default:
                    setMode(Mode.SoftLanding);
                    break;
            }
            setStatus(Status.Idle);
        }

        public override void OnLoad(ConfigNode node)
        {
            if (node.HasValue("mode"))
                setMode((Mode)ConfigNode.ParseEnum(typeof(Mode), node.GetValue("mode")));
            if (node.HasValue("status"))
                setStatus((Status)ConfigNode.ParseEnum(typeof(Status), node.GetValue("status")));
        }

        public override void OnSave(ConfigNode node)
        {
            node.SetValue("mode", mode.ToString(), true);  // Do not localize these!  This is for internal state saving, not external display.
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

            if (engine.propellants.Count > 1)
                log.error("Engine runs on multiple propellants! Will not work correctly!");
            propellantResource = part.Resources.Get(engine.propellants[0].id);

            // To allow refueling with KIS
            if (HighLogic.LoadedSceneIsFlight)
                part.attachRules.allowSrfAttach = true;

            animStates = setUpAnimation(animationName, part);

            // Localize event and action names
            Events["arm"].guiName = Localizer.Format("#autoLOC_XTL_arm_guiname");
            Events["disarm"].guiName = Localizer.Format("#autoLOC_XTL_disarm_guiname");
			Events["nextMode"].guiName = Localizer.Format("#autoLOC_XTL_setMode_guiname", modeString(_mode));
            Actions["armAction"].guiName = Localizer.Format("#autoLOC_XTL_arm_guiname");
            Actions["disarmAction"].guiName = Localizer.Format("#autoLOC_XTL_disarm_guiname");
            Fields["displayStatus"].guiName = Localizer.Format("#autoLOC_XTL_displayStatus_guiname");
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
                if (propellantResource.amount == 0)
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
                    ScreenMessages.PostScreenMessage("Landertron out of electric charge, disarming!", 5, ScreenMessageStyle.UPPER_CENTER);
                }
            }
        }

        virtual internal void fire()
        {
            log.debug("Firing engine");
            engine.Activate();
            status = Status.Firing;
        }

        virtual internal void shutdown()
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

        protected void ventFuel()
        {
            log.debug("Venting fuel: " + propellantResource.amount);
            propellantResource.amount = 0;
            status = Status.Empty;
        }

        protected void setMode(Mode value)
        {
            _mode = value;
            Events["nextMode"].guiName = Localizer.Format("#autoLOC_XTL_setMode_guiname", modeString(mode));
        }

        protected void setStatus(Status value)
        {
            _status = value;
            log.info("Status set to " + _status.ToString());
            switch (_status)
            {
                case Status.Idle:
                    displayStatus = Localizer.Format("#autoLOC_XTL_Status_Idle");
                    part.stackIcon.SetIconColor(XKCDColors.White);
                    animDeployed = false;
                    Events["arm"].active = true;
                    Events["disarm"].active = false;
                    Actions["armAction"].active = true;
                    Actions["disarmAction"].active = false;
                    break;
                case Status.Armed:
                    displayStatus = Localizer.Format("#autoLOC_XTL_Status_Armed");
                    part.stackIcon.SetIconColor(XKCDColors.LightCyan);
                    animDeployed = true;
                    Events["arm"].active = false;
                    Events["disarm"].active = true;
                    Actions["armAction"].active = false;
                    Actions["disarmAction"].active = true;
                    break;
                case Status.Firing:
                    displayStatus = Localizer.Format("#autoLOC_XTL_Status_Firing");
                    part.stackIcon.SetIconColor(XKCDColors.RadioactiveGreen);
                    animDeployed = true;
                    Events["arm"].active = false;
                    Events["disarm"].active = false;
                    Actions["armAction"].active = false;
                    Actions["disarmAction"].active = false;
                    break;
                case Status.Empty:
                    displayStatus = Localizer.Format("#autoLOC_XTL_Status_Empty");
                    part.stackIcon.SetIconColor(XKCDColors.DarkGrey);
                    animDeployed = true;
                    Events["arm"].active = false;
                    Events["disarm"].active = false;
                    Actions["armAction"].active = false;
                    Actions["disarmAction"].active = false;
                    break;
            }
        }

        protected void refuel()
        {
            bool justrefueled = false;
            for (int i = 0; i < part.children.Count;)
            {
                Part p = part.children[i];
                if (p.Resources.Contains(propellantResource.info.id) && propellantResource.amount < propellantResource.maxAmount)
                {
                    PartResource additionalPropellant = p.Resources.Get(propellantResource.info.id);
                    propellantResource.amount = Math.Min(propellantResource.amount + additionalPropellant.amount, propellantResource.maxAmount);
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
                status = Status.Idle;
            }
        }

        protected void forAllSym()
        {
            foreach (Part p in part.symmetryCounterparts)
            {
                Landertron ltron = p.Modules["Landertron"] as Landertron;
                ltron.mode = mode;
            }
        }

        protected static AnimationState[] setUpAnimation(string animationName, Part part)  //Thanks Majiir!
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

        protected void updateAnimation()
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

        protected String statusString(Status stat)
        {
            switch (stat)
            {
                case Status.Idle:
                    return Localizer.Format("#autoLOC_XTL_Status_Idle");
                case Status.Armed:
                    return Localizer.Format("#autoLOC_XTL_Status_Armed");
                case Status.Firing:
                    return Localizer.Format("#autoLOC_XTL_Status_Firing");
                case Status.Empty:
                    return Localizer.Format("#autoLOC_XTL_Status_Empty");
                default:
                    return "";
            }
        }

        protected String modeString(Mode mode)
        {
            switch (mode)
            {
                case Mode.StayPut:
                    return Localizer.Format("#autoLOC_XTL_Mode_StayPut");
                case Mode.SoftLanding:
                    return Localizer.Format("#autoLOC_XTL_Mode_SoftLanding");
                case Mode.ShortLanding:
                    return Localizer.Format("#autoLOC_XTL_Mode_ShortLanding");
                default:
                    return "";
            }
        }
    }
}