using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;


public class Landertron : PartModule
{
	//[KSPField(guiActive = true, guiActiveEditor=true ,isPersistant=true),UI_FloatRange(maxValue=1.5f,minValue=0.5f,stepIncrement=0.05f)]
	//public float heightmultiplier = 1;
	//[KSPField(guiActive = true, guiActiveEditor = true, isPersistant = true), UI_FloatRange(maxValue = 3, minValue = -3f, stepIncrement = 0.1f)]
	//public float offset = 0;
	//[KSPField(guiActive = false)]
	//public float endheight = 0;
	[KSPField(guiActive = false, guiActiveEditor = true, isPersistant = true), UI_FloatRange(maxValue = 10, minValue = 0, stepIncrement = 0.5f)]
	public float endspeed = 0;
	[KSPField(guiActive = false)]
	public bool boom = false;
	[KSPField(guiActive = false)]
	public bool showgui = true;
	[KSPField(guiActive = false)]
	public bool refuelable = true;
	//[KSPField(guiActive = false)]
	//public bool engineShutdown = true;
	[KSPField(guiActive = false)]
	public float electricrate = 0.05f;
	[KSPField(guiActive = false, guiActiveEditor=false ,isPersistant=true)]
	public int mode = 0;
	public float thrust = 0;
	[KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false)]
	public string Status = "Idle";
	[KSPField(isPersistant = false, guiActiveEditor = true, guiActive = true)]
	public string ModeName = " ";
	//[KSPField(isPersistant = false, guiActiveEditor = false, guiActive = true)]
	public double deltaV = 0;
	public float m = 0;
	public float elec_dem = 0;
	public float elec_rec = 0;
	public float totalfuelmass = 0;
	public double totalthrust = 0;
	public double partanglethrust = 0;
	public double vesselanglethrust = 0;
	public double localg;
	public double gee = 0;
	public double vaccel = 0;
	public double backgroundaccel = 0;
	public double vh_prev = 1000;
	public double v = 0;
	[KSPField(isPersistant = false, guiActive = true, guiName = "Braking Distance", guiUnits = "m")]
	public float displayd = 0;
	public double dfinal = 0;
	public double dmin = 0;
	public bool fire = false;
	public bool arm = false;
	public bool end = false;
	public bool firing = false;
	public bool warning = false;
	public bool prevland = true;
	public static bool globaljustrefueled = false;
	public bool justrefueled = false;
	protected ModuleEngines engine;
	protected PartResource sf;
	protected Landertron ltron;
	public Vector3d pos;
	public Vector3d up;
	public Vector3d acc;
	public Vector3d vel;
	[KSPField(guiActive=false)]
	public int lcount = 0;
	public RaycastHit craft;
	public Part mpart;
	public float isp;
	public float burntime;

	[KSPField(isPersistant = false)]
	public string AnimationName;

	private AnimationState[] animStates;
	private bool animdeployed=false;

	public override void OnAwake()
	{
		lcount = 0;

	}
	public override void OnStart(PartModule.StartState state)
	{
		engine = this.part.Modules["ModuleEngines"] as ModuleEngines;

		sf = this.part.Resources["SolidFuel"];
		if (HighLogic.LoadedScene == GameScenes.SPH && mode == 0)
		{ mode = 2; }
		else if (HighLogic.LoadedScene == GameScenes.EDITOR && mode == 0)
		{ mode = 1; }
		switch(mode)
		{
		case 1:
			ModeName = "Soft Landing";
			break;
		case 2:
			ModeName = "Short Landing";
			break;
		case 3:
			ModeName = "StayPut";
			break;
		}
		guifixer();
		animStates = SetUpAnimation(AnimationName, this.part);

		//GameEvents.onJointBreak.Add(onJointBreak);
	}
	//private void onJointBreak(EventReport eventreport)
	//{ print("Something detatched! ");}
	/*[KSPEvent(guiName = "Refuel",guiActive=false ,externalToEVAOnly = true, guiActiveUnfocused = true, unfocusedRange = 3.0f)]
    public void Refuel()
    {
        double sf_available = 0;
        int containercount=0;
        foreach (Part p in vessel.parts)
        {
            if (!p.Modules.Contains("Landertron") && p.Resources.Contains("SolidFuel"))
            {
                PartResource sfp = p.Resources["SolidFuel"];
                sf_available += sfp.amount;
                containercount++;
            }
        }
                //print("avail: " + sf_available);
        double sf_needed = sf.maxAmount - sf.amount;
        double sf_added = Math.Min(sf_available, sf_needed);
        sf.amount += sf_added;
        foreach (Part p in vessel.parts)
        {
            if (!p.Modules.Contains("Landertron") && p.Resources.Contains("SolidFuel"))
            {
                PartResource sfp = p.Resources["SolidFuel"];
                sfp.amount -= sf_added/containercount;

            }
        }
        justrefueled = true;
        animdeployed = false;
    }*/
	public void Refuel()
	{
		/*int ccount = 0;
        foreach (Part p in this.part.children)
        {
            print("name: "+p.name);
            p.Die();
            //p.explosionPotential = 0;
            //p.explode();
            //ccount++;
        }*/
		for (int i = 0; i < this.part.children.Count; )
		{
			Part p = this.part.children[i];
			if (p.Resources.Contains("SolidFuel") && sf.amount < sf.maxAmount)
			{

				PartResource sfp = p.Resources["SolidFuel"];
				sf.amount = Math.Min(sf.amount + sfp.amount, sf.maxAmount);
				//print("name: " + p.name);
				p.Die();
				justrefueled = true;
				animdeployed = false;
			}
			else ++i;
		}

		/*if (ccount > 0)
        {
            for (int i = 0; i < ccount; i++)
            {
                print("name: " + this.part.children   (i).name);
            }
        }*/
	}
	[KSPEvent(guiName = "Soft Landing", guiActiveEditor = true, guiActiveUnfocused = false, guiActive = false)]
	public void ClassicMode()
	{
		mode = 1;
		ModeName = "Soft Landing";
		forAllSym();        
	}
	[KSPEvent(guiName = "Short Landing", guiActiveEditor = true, guiActiveUnfocused = false, guiActive = false)]
	public void VSL()
	{
		mode = 2;
		ModeName = "Short Landing";
		forAllSym();        
	}
	[KSPEvent(guiName = "StayPut", guiActiveEditor = true, guiActiveUnfocused = false, guiActive = false)]
	public void SP()
	{
		mode = 3;
		ModeName = "StayPut";
		forAllSym();
	}
	[KSPEvent(guiName = "Decouple", guiActive = false, externalToEVAOnly = true, guiActiveUnfocused = true, unfocusedRange = 3.0f)]
	public void decoup()
	{
		this.part.decouple(2000);
	}
	/*[KSPEvent(guiName = "Escape!", guiActiveEditor = true, guiActiveUnfocused = false, guiActive = true)]
    public void escape()
    {
        mode = 4;
        ModeName = "Escape!";
        forAllSym();
    }*/
	[KSPAction("Arm")]
	public void armaction(KSPActionParam param)
	{
		this.part.force_activate();
	}
	[KSPAction("Decouple")]
	public void decoupleaction(KSPActionParam param)
	{
		this.part.decouple(2000);
	}
	/*[KSPAction("Abort", KSPActionGroup.Abort)]
    public void abort()
    {
        if (mode==4)
        {
            foreach (Part p in vessel.parts)
            {
                if (p.Modules.Contains("ModuleDecouple"))
                {
                    ModuleDecouple dec=p.Modules["ModuleDecouple"] as ModuleDecouple;
                    dec.Decouple();
                }
            }
        engine.Activate();
        }
    }*/
	public void Update()
	{
		if (HighLogic.LoadedSceneIsEditor)
		{
			guifixer();

			/*totalmass = 0;
            totalfuelmass = 0;
            foreach (Part p in EditorLogic.SortedShipList)
            {
                if (p.Modules.Contains("Landertron") && p.inverseStage == this.part.inverseStage)
                {
                    ltron = p.Modules["Landertron"] as Landertron;
                    if (ltron.mode == mode)
                    {
                        totalfuelmass = totalfuelmass+p.GetResourceMass();
                        
                    }
                }
                totalmass = totalmass+p.GetResourceMass()+p.mass;
            }
            //print("fuel: " + totalfuelmass);
            //print("ln: " + Mathf.Log(totalmass / (totalmass - totalfuelmass)));
            deltaV = engine.atmosphereCurve.Evaluate(1) * 9.81f * Mathf.Log(totalmass / (totalmass - totalfuelmass));*/
		}


	}
	public override void OnActive()
	{
		lcount = 0;
		foreach (Part p in vessel.parts)
		{

			if (p.Modules.Contains("Landertron") && p.inverseStage==this.part.inverseStage && p.GetResourceMass()>0)
			{

				ltron=p.Modules["Landertron"] as Landertron;
				if (ltron.mode == mode)
				{
					lcount = lcount + 1;

				}
			}

		}
		//partanglethrust = Vector3d.Dot(this.part.transform.up.normalized, this.vessel.transform.up.normalized);

		animdeployed = true;

	}
	public override void OnUpdate()
	{
		guifixer();
		if (justrefueled && this.vessel == FlightGlobals.ActiveVessel)
		{
			//if (globaljustrefueled)
			//{ 
			//    Staging.AddStageAt(0);
			//    globaljustrefueled = false;
			//}
			//int c = 0;
			//int stg = Staging.CurrentStage;
			//if (stg == c)
			//{ Staging.AddStageAt(0); }

			//this.part.stackIcon.SetIconColor(XKCDColors.White);
			Status = "Idle";
			this.part.deactivate();
			//print("deact");
			this.part.inverseStage = 0;
			//Staging.AddStageAt(Staging.CurrentStage+1);
			//Staging.AddStageAt(Staging.CurrentStage + 1);
			//print("stage: " + Staging.CurrentStage);
			//Staging.RecalculateVesselStaging(this.vessel);
			justrefueled = false;


		}
		if (refuelable)
		{
			Refuel();
		}


		foreach (AnimationState anim in animStates)
		{
			if (animdeployed && anim.normalizedTime < 1) { anim.speed = 1; }
			if (animdeployed && anim.normalizedTime >= 1)
			{
				anim.speed = 0;
				anim.normalizedTime = 1;
			}
			if (!animdeployed && anim.normalizedTime > 0) { anim.speed = -1; }
			if (!animdeployed && anim.normalizedTime <= 0)
			{
				anim.speed = 0;
				anim.normalizedTime = 0;
			}
		}
	}
	public override void OnFixedUpdate()
	{


		//if (engine.maxThrust != 0 && engine.maxThrust != thrust)
		//{ thrust = engine.maxThrust; }

		m = this.vessel.GetTotalMass();
		v = this.vessel.verticalSpeed;
		//gee = FlightGlobals.getGeeForceAtPosition(this.vessel.mainBody.position).magnitude;
		pos = this.vessel.findWorldCenterOfMass();
		up = (pos - this.vessel.mainBody.position).normalized;
		vesselanglethrust = Vector3d.Dot(this.vessel.transform.up.normalized, up);
		Vector3 thrustp = Vector3d.zero;
		foreach (var t in engine.thrustTransforms)
		{ thrustp -= t.forward / engine.thrustTransforms.Count; }

		Vector3 fwd = HighLogic.LoadedScene == GameScenes.EDITOR ? Vector3d.up : (HighLogic.LoadedScene == GameScenes.SPH ? Vector3d.forward : (Vector3d)engine.part.vessel.GetTransform().up);
		partanglethrust = Vector3.Dot(fwd, thrustp);
		acc = this.vessel.acceleration;
		vaccel = Vector3d.Dot(acc, up);
		totalthrust = engine.maxThrust * partanglethrust * vesselanglethrust * lcount * (engine.thrustPercentage / 100);
		//print(totalthrust);
		totalfuelmass = lcount * (float)sf.amount * 0.0075f; //this.part.GetResourceMass();
		isp = engine.atmosphereCurve.Evaluate((float)this.vessel.staticPressure);
		deltaV = isp * 9.81f * Mathf.Log(this.vessel.GetTotalMass() / (this.vessel.GetTotalMass() - totalfuelmass)) * partanglethrust;
		//burntime = this.part.GetResourceMass() / (engine.maxThrust / isp);
		burntime = (float)sf.amount *0.0075f / (engine.maxThrust*(engine.thrustPercentage/100) / (isp*9.81f));

		elec_dem = electricrate * TimeWarp.fixedDeltaTime;
		elec_rec = elec_dem;
		if (sf.amount > 0.1)
		{ elec_rec = this.part.RequestResource("ElectricCharge", elec_dem); }
		modeconfig();
		if (elec_rec < elec_dem)
		{
			this.part.stackIcon.SetIconColor(XKCDColors.BrightYellow);
			Status = "No Power";
		}
		else if (sf.amount == 0 && engine.getIgnitionState)
		{
			engine.allowShutdown = true;
			engine.Shutdown();
			engine.allowShutdown = false;
			this.part.stackIcon.SetIconColor(XKCDColors.White);
			Status = "Idle";
			this.part.deactivate();
		}
		else if (end)
		{
			if (boom)
			{ this.part.explode(); }
			else
			{
				//print("venting " + sf.amount + " fuel");
				sf.amount = 0; 
			}
			firing = false;
		}
		else if (fire)
		{
			Status = "Firing!";
			engine.Activate();
			firing = true;
			this.part.stackIcon.SetIconColor(XKCDColors.RadioactiveGreen);
		}
		else if (arm)
		{
			if (engine.getIgnitionState)
			{
				engine.allowShutdown = true;
				engine.Shutdown();
				engine.allowShutdown = false;
			}
			if (sf.amount > 0)
			{
				if (warning)
				{
					this.part.stackIcon.SetIconColor(XKCDColors.Red);
					Status = "Warning! Too Fast!";
				}
				else
				{
					this.part.stackIcon.SetIconColor(XKCDColors.LightCyan);
					Status = "Armed";
				}

			}
			else
			{
				this.part.stackIcon.SetIconColor(XKCDColors.White);
				Status = "Idle";
				this.part.deactivate();
			}
		}
		prevland = this.vessel.Landed;
	}
	protected void modeconfig()
	{
		switch (mode)
		{
		case 1:
			//Classic
			warning = -v - endspeed > deltaV * vesselanglethrust;
			if (vesselanglethrust < 0.2)
			{
				dmin = 0;
			}
			//else if (warning)
			//{
			//double realend = v + deltaV * vesselanglethrust;
			//dmin = -1* (realend * realend - v * v) / (2 * (totalthrust / m + vaccel));
			//dmin = -(v * burntime + 0.5 * (totalthrust / m + vaccel) * burntime * burntime);
			//dmin = -v * burntime;
			//}
			else if (v + (totalthrust / m + vaccel) * burntime > -endspeed)
			{

				dmin = -1 * (endspeed * endspeed - v * v) / (2 * (totalthrust * 0.90 / m + vaccel));
			}
			else
			{
				dmin = -(v * burntime + 0.5 * (totalthrust / m + vaccel) * burntime * burntime);
			}
			//double dfullburn = -(v + Math.Max(v + (totalthrust / m + vaccel) * burntime,0)) * burntime / 2;
			//double dfullburn = -(v * burntime + Math.Max(0.5 * (totalthrust / m + vaccel) * burntime * burntime,0));
			if (!firing)
			{ backgroundaccel = vaccel; }
			dfinal = dmin; //* heightmultiplier + offset;
			//dfinal = Math.Min(dmin, dfullburn);
			displayd = (float)dfinal;
			arm = !firing;
			float h = height();
			fire = h < dfinal && v < -1 && !this.vessel.Landed && sf.amount > 0;// && (h/v)<burntime;
			//end = (h < endheight || v > -1 * endspeed || sf.amount == 0) && firing;
			if (!firing)
			{
				//print("w: " + warning + " dmin: " + dmin + " vf: " + (v + (totalthrust / m + vaccel) * burntime));
			}
			end = (h < 0.1 || v > -1 * endspeed || sf.amount == 0) && firing;

			double areq = endspeed * endspeed -v * v / (2 * -1 * h) - backgroundaccel;
			double adiff = areq - vaccel;
			//float throt=areq * m / totalthrust;
			if (firing)
			{
				engine.throttleLocked = false;
				engine.useEngineResponseTime = true;
				engine.engineAccelerationSpeed = 0.0f;
				engine.engineDecelerationSpeed = 0.0f;
				//engine.currentThrottle = Mathf.Min((float)(areq * m / totalthrust), 0);
				//engine.currentThrottle = Mathf.Clamp(engine.currentThrottle + (float)(adiff * m / (vesselanglethrust * partanglethrust * engine.maxThrust)), 0, 1);
				//engine.currentThrottle = Mathf.Clamp(engine.currentThrottle + (float)(adiff * m / totalthrust), 0, 1);
				engine.currentThrottle = Mathf.Clamp((float)(areq * m *(engine.thrustPercentage/100) / totalthrust), 0, 1);
				//print(engine.requestedThrust);
				//print("areq: " + areq + " adiff: " + adiff);
				//print("v: " + v + " h: " + h + " rthrot: " + (areq * m / totalthrust));
				//print("Fuel: "+sf.amount+" v: " +v);
				//engine.throttleLocked = true;
			}

			break;
		case 2:
			//Space Plane
			double vh = this.vessel.srf_velocity.magnitude;

			arm = !firing;
			fire = this.vessel.Landed && !prevland && sf.amount > 0;
			end = (vh < endspeed || vh_prev<vh ||sf.amount <= 0) && firing;
			warning = vh > -deltaV;
			//if (firing)
			//{ print("vh: " + vh); }
			vh_prev = vh;
			break;
		case 3:
			//StayPut
			double staydir = vesselanglethrust;// Vector3d.Dot(this.part.transform.up.normalized, up);

			arm = !firing;
			fire = this.vessel.Landed  && sf.amount > 0; //&& v > 0.1
			end = (staydir<0.1 || sf.amount < 0.1) && firing;
			warning = false;

			break;
		default:
			break;
		}
	}
	protected float height()
	{
		float dsea = (float)FlightGlobals.getAltitudeAtPos(pos);
		float d = dsea;
		if (Physics.Raycast(pos, -up, out craft, dsea + 10000f, 1 << 15))
		{ d = Mathf.Min(dsea, craft.distance); }
		//else { d = dsea; }
		float surfheight = dsea - d;
		float lowestd = d;
		foreach (Part p in vessel.parts)
		{
			if (p.collider != null) //Makes sure the part actually has a collider to touch ground
			{
				Vector3 bottom = p.collider.ClosestPointOnBounds(vessel.mainBody.position); //Gets the bottom point
				float partAlt = FlightGlobals.getAltitudeAtPos(bottom) - surfheight;  //Gets the looped part alt
				lowestd = Mathf.Max(0, Mathf.Min(lowestd, partAlt));  //Stores the smallest value in all the parts
			}
		}
		d = lowestd;
		return d;
	}
	protected void forAllSym()
	{
		foreach (Part p in this.part.symmetryCounterparts)
		{

			ltron = p.Modules["Landertron"] as Landertron;
			ltron.mode = mode;
			ltron.ModeName = ModeName;
		}
	}
	protected void guifixer()
	{
		//engine.Events["Activate Engine"].guiActive = false;
		//engine.Actions["toggle"].active = false;
		if (!showgui)
		{
			ltron = this.part.Modules["Landertron"] as Landertron;
			Fields["endspeed"].guiActive = false;
			Fields["endspeed"].guiActiveEditor = false;
			Events["ClassicMode"].guiActiveEditor = false;
			Events["Decouple"].guiActiveUnfocused = false;
			Events["VSL"].guiActiveEditor = false;
			Events["SP"].guiActiveEditor = false;
			Fields["ModeName"].guiActive = false;
			Fields["ModeName"].guiActiveEditor = false;
			Fields["Status"].guiActive = false;
			Fields["displayd"].guiActive = false;

		}
		else if (mode == 1 || mode==2)
		{
			//Fields["heightmultiplier"].guiActive = true;
			//Fields["heightmultiplier"].guiActiveEditor = true;
			//Fields["offset"].guiActive = true;
			//Fields["offset"].guiActiveEditor = true;
			Fields["endspeed"].guiActive = true;
			Fields["endspeed"].guiActiveEditor = true;
		}
		else
		{
			//Fields["heightmultiplier"].guiActive = false;
			//Fields["heightmultiplier"].guiActiveEditor = false;
			//Fields["offset"].guiActive = false;
			//Fields["offset"].guiActiveEditor = false;
			Fields["endspeed"].guiActive = false;
			Fields["endspeed"].guiActiveEditor = false;
		}
	}
	public static AnimationState[] SetUpAnimation(string animationName, Part part)  //Thanks Majiir!
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

}

