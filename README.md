# Landertrons

*Automatic retro-rockets for landing.*

![Landertrons logo](https://github.com/Kerbas-ad-astra/XTLandertron/raw/master/Landertron-logo.png)

##Features

Landertrons are small solid rocket motors which can be mounted on a craft and set to automatically fire in one of three modes:

* **SoftLanding**, firing to *brake* a rocket's descent and prevent the rocket from *breaking*.  Note that the engines will cut out when the craft is slightly above the ground, so be ready for a bit of a drop!
* **ShortLanding**, firing to arrest a spaceplane's momentum on landing.
* **StayPut**, firing downward to prevent a vehicle from bouncing or tipping (or at least try).

A landertron will only fire if it can make a positive contribution to its desired mode, and will cut out automatically, but it's up to you to put them in the proper quantity and orientation to get the job done.

If KAS and KIS are installed, then the landertrons can be refueled using the included hex-cans of solid fuel.  Just attach them to spent landertrons, and you will be ready to fly again in no time.

There is also a "Landertron Box", which will attempt to control a spacecraft's engines as if they were landertrons.  Warranty void if used for critical mission phases!

##Recommended addons

* [**Home Grown Rockets**](http://forum.kerbalspaceprogram.com/index.php?/topic/55521-102hgr-1875m-partsv130-released/) (plus [HGR Community Fixes](http://forum.kerbalspaceprogram.com/index.php?/topic/131556-104-5-hgr-community-fixes-home-grown-fixes-for-home-grown-rockets-v12-2016-mar-01/)) have retro-rockets in their "SoyJuice" descent modules.  With Landertron, they will automatically fire to soften their landings, just like the real Soyuz pods.  (Note that they are not capable of substituting for parachutes!)

##Download and install

* [**GitHub**](https://github.com/Kerbas-ad-astra/XTLandertron/releases)
* CurseForge

From there, just unzip the "Landertron" folder into your GameData directory.

##Known and anticipated issues

None at this time.  Please let us know in the thread or on the [**issue tracker**](https://github.com/Kerbas-ad-astra/XTLandertron/issues) if you find any.

##Version history and changelog

* 2014 Oct 10 (0.08): [XanderTek's last release](http://forum.kerbalspaceprogram.com/index.php?/topic/55798-10-xt-landertron-smart-retrorockets-for-landers-and-spaceplanes-v008-oct-10/), for 0.25.
* 2015 Sep 06 (v0.09-beta.KAA): Restored functionality.
	* Fixed Landertron module to work with 1.0.x atmosphere changes (new Isp calculation code based on Kerbal Engineer).
	* Fixed Landertron Short Landing mode to be less sensitive to bouncy landings.
	* Updated parts to 1.0.4 thermal and atmosphereCurve properties.
    * Changed scale (and updated nodes and FX accordingly) to avoid bug with MODEL{} nodes and rescaleFactor.
* 2015 Nov 03 (v0.10.0-alpha): Complete code rewrite (by charfa)
	* Config/persistence file changes:
		* Fields endspeed, boom and showgui removed. If you depended on functionality switched by those fields, it won't work.
		* Field names of electricrate and AnimationName changed to electricRate and animationName. Yeah, I know, silly little changes that require you to fix the save files, but I like consistent naming.
		* Field mode now stores literal mode name instead of a number. You need to substitute 1, 2 and 3 with SoftLanding, ShortLanding and StayPut respectively.
	* Functional changes:
		* Methods of determining when to fire and shutdown landertrons in all modes have changed. This mostly results in improvements and avoids some bugs that were reported. One case that may be seen as a regression is the removal of throttling at the end of burn that allowed softer landings. This version burns full throttle until complete stop, which may cause the vessel to stop up to a few meters above ground due to discreet nature of physics in game. I felt like throttling solid fuel rockets was cheating.
		* Some GUI changes, mostly decluttering. Of useful stuff, gone is the braking distance display and notification when there's not enough deltaV.
		* Landertron now consumes electric charge when it's armed and disarms when it runs out of juice. You can arm it again when you refill your batteries. I honestly couldn't figure out what the behavior was in old code, so I don't know if it is a change.
	* New features:
		* Landertron module should now support being used on parts with ModuleEnginesFX and ModuleEnginesRF, and using other propellant that SolidFuel (as long as it's a single propellant only). Not tested though.
	* There might be other changes not listed here. Like I said it's a complete code rewrite so it's hard to figure out how the behavior will differ specifically.
* 2015 Nov 07 (v0.10.0-beta): *Also by charfa.*
	* Removed features: 
		* Config parameters endspeed, boom and showgui are removed. End speed is always 0, parts never go boom, GUI is always visible.
		* Icon is no longer colored when Landertron does not have enough electric charge or deltaV. When running out of electric charge Landertron disarms and shows a screen message. Not having enough deltaV is not notified in any way.
		* Removed throttling of Landertron to provide a soft touchdown. You may end up being stopped a few meters above ground now. I felt like throttling of solid fuel rockets was cheating. If you want a gentle touchdown use liquid fuel engines and MechJeb autopilot.
	* Bug fixes:
		* XT-L1 and XT-L2B can now be refueled.
		* Mode is now initialized properly to SoftLanding and ShortLanding in VAB and SPH respectively.
	* New features:
		* Landertron module should now work when added to parts with ModuleEnginesFX and ModuleEnginesRF.
		* Landertron module should now work with engines running on any propellant, not only SolidFuel, as long as it's a single propellant (i.e. won't work with LF+O, but should work with RealFuels' solid fuels).
	* Other changes/improvements:
		* GUI is significantly decluttered. Landertron module itself only adds mode switch button in editor and arm/disarm button and status information in flight.
		* Logic behind deciding when to fire and stop Landertrons has been rewritten. It should now be more robust and better handle some situations that I saw reported on forum as causing problems (e.g. building landers in SPH, placing Landertrons at various angles, mixing different Landertrons in a stage...).
		* All textures are converted to DDS.
		* Other? It's a complete code rewrite, it's a hard to predict if the behavior hasn't changed slightly in other aspects.
* 2015 Nov 19 (v0.11.0): *Also by charfa.*
	* Bug fixes:
		* Fix "Look rotation viewing vector is zero" log spam.
		* Turn off decoupler Enable/Disable Staging switch
	* Internal:
		* Compile for KSP 1.0.5
		* Switch to use 'stagingEnabled' to prevent engine and decoupler from activating upon staging.
* 2016 Mar 25 (v0.12): Ocean landings
	* Landertrons will now soft-land over oceans as well as on land.
* 2016 Apr 24 (v0.13): Vnity 5
	* Incorporated "LandertronBox" module from Booots.  A part with this module will control a craft's engines to achieve landertron functionality (at least, it will try -- this feature is a bit finicky).
		* Added the "XT-LB Landertron Box" part (welded from the stock Small Inline Reaction Wheel and Atmospheric Fluid Spectro-Variometer models, so be careful when pruning).
	* Compiled against 1.1 libraries.
* 2016 Oct 12 (v0.14): Future Proofing.
	* Added support for thrustTransformMultipliers.
	* Updated for KSP 1.2.
	* Added .version file.
* 02016 Oct 13 0340 UTC (v0.15): Crunch!
	* Packaged the right DLL this time...
* 02016 Oct XX (v1.0): One Point Zero
	* New XT-LB model, from steedcrugeon.  (The old model has been made unresearchable, and will be removed in the next release.)

##Roadmap

Just keeping it ticking along.   Please let us know in the thread or on the [**issue tracker**](https://github.com/Kerbas-ad-astra/XTLandertron/issues) if you have any suggestions.

##Credits / License

* Source code and DLL licensed under [**GNU GPL**](http://www.gnu.org/licenses/gpl.html) (v3 or later).  Variously by XanderTek, Kerbas_ad_astra, charfa, and Booots.  If you rele
* SolidFuel HexCan part based on Greys' [**HexCans**](http://forum.kerbalspaceprogram.com/threads/33754-0-25-HexCans-Standardized-Resource-Canisters-0-7-1-Breaking-Ground-Edition), licensed under [**CC-BY-SA**](https://creativecommons.org/licenses/by-sa/2.0/).
* Landertron parts based on [**models by BahamutoD**](http://forum.kerbalspaceprogram.com/threads/82341-1-0-B-Dynamics-Retracting-vectoring-engines-etc-v1-2-0-%28May-6%29), licensed under [**CC-BY-SA**](https://creativecommons.org/licenses/by-sa/2.0/).
* XT-L-KRAB model by steedcrugeon, licensed under [**CC-BY-SA**](https://creativecommons.org/licenses/by-sa/4.0/) (4.0 or later).

Any redistributions must use a different name and folder (per the GPL section 7c).  All other rights (e.g. the Landertrons logo) reserved.