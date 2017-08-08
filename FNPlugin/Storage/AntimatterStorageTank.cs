using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TweakScale;

namespace FNPlugin
{
    [KSPModule("Antimatter Storage")]
    class AntimatterStorageTank : FNResourceSuppliableModule, IPartMassModifier, IRescalable<FNGenerator> , IPartCostModifier
    {
        [KSPField(isPersistant = true)]
        public double chargestatus = 1000;
        [KSPField(isPersistant = false)]
        public double maxCharge = 1000;
        [KSPField(isPersistant = false)]
        public float massExponent = 3;
        [KSPField(isPersistant = false)]
        public double chargeNeeded = 100;
        [KSPField(isPersistant = false)]
        public string resourceName = "Antimatter";
        [KSPField(isPersistant = false)]
        public string animationName;
        [KSPField(isPersistant = false)]
        public double animationExponent = 1;

        [KSPField(isPersistant = false, guiActive = false, guiName = "Exploding")]
        bool exploding = false;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Charge")]
        public string chargeStatusStr;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Status")]
        public string statusStr;
        [KSPField(isPersistant = false, guiActiveEditor = true, guiActive = true, guiName = "Current")]
        public string capacityStr;
        [KSPField(isPersistant = false, guiActiveEditor = true, guiActive = true, guiName = "Maximum")]
        public string maxAmountStr;

        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiUnits = "K",  guiName = "Maximum Temperature"), UI_FloatRange(stepIncrement = 10f, maxValue = 1000f, minValue = 40f)]
        public float maxTemperature = 1000;
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiUnits = "g", guiName = "Maximum Acceleration"), UI_FloatRange(stepIncrement = 0.1f, maxValue = 10f, minValue = 0.1f)]
        public float maxGeeforce = 10;

        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = true, guiName = "Cur/Max Temp", guiFormat = "F3")]
        public string TemperatureStr;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Cur/Max Geeforce")]
        public string GeeforceStr;

        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Module Cost")]
        public float moduleCost = 1;
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Stored Mass")]
        public double storedMassMultiplier = 1;
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Scaling Factor")]
        public double storedScalingfactor = 1;
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Fixed Delta Time")]
        public double fixedDeltaTime;
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Antomatter Density")]
        public double antimatterDensity;

        [KSPField(isPersistant = false, guiActiveEditor = false)]
        public bool calculatedMass = false;
        [KSPField(isPersistant = false, guiActiveEditor = false)]
        public bool canExplodeFromGeeForce = false;
        [KSPField(isPersistant = false, guiActiveEditor = false)]
        public bool canExplodeFromHeat = false;
        [KSPField(isPersistant = false, guiActiveEditor = true, guiActive = true, guiName = "Part Mass", guiUnits = " t", guiFormat = "F3" )]
        public double partMass;
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Initial Mass", guiUnits = " t", guiFormat = "F3")]
        public double initialMass;
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Target Mass", guiUnits = " t", guiFormat = "F3")]
        public double targetMass;
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Delta Mass", guiUnits = " t", guiFormat = "F3")]
        public float moduleMassDelta;
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Attached Tanks Count")]
        public double attachedAntimatterTanksCount;



        [KSPField(isPersistant = false, guiName = "Animation Ratio", guiActiveEditor = true, guiActive = true, guiFormat = "F3")]
        public float animationRatio;

        


        [KSPField(isPersistant = true)]
        public float emptyCost = 0;
        [KSPField(isPersistant = true)]
        public float dryCost = 0;
        [KSPField(isPersistant = true)]
        public double partCost;

        //[KSPField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiUnits = "%", guiName = "Anti Hydrogen"), UI_FloatRange(stepIncrement = 0.1f, maxValue = 100f, minValue = 0f)]
        //public float resourceFloatRange = 0;

        bool charging = false;
        bool should_charge = false;
        float explosion_time = 0.35f;
        
        float explosion_size = 5000;
        float cur_explosion_size = 0;
        double current_antimatter = 0;
        double minimimAnimatterAmount;

        int startup_timeout = 200;
        int power_explode_counter = 0;
        int geeforce_explode_counter = 0;
        int temperature_explode_counter = 0;

        AnimationState[] containerStates;
        GameObject lightGameObject;
        PartResource antimatterResource;
        PartResourceDefinition antimatterDefinition;
        List<AntimatterStorageTank> attachedAntimatterTanks;

        [KSPEvent(guiActive = true, guiName = "Start Charging", active = true)]
        public void StartCharge()
        {
            should_charge = true;
        }

        [KSPEvent(guiActive = true, guiName = "Stop Charging", active = true)]
        public void StopCharge()
        {
            should_charge = false;
        }

        public virtual void OnRescale(TweakScale.ScalingFactor factor)
        {
            try
            {
                Debug.Log("FNGenerator.OnRescale called with " + factor.absolute.linear);
                storedScalingfactor = factor.absolute.linear;
                storedMassMultiplier = Math.Pow(storedScalingfactor, massExponent);
                initialMass = part.prefabMass * storedMassMultiplier;
                chargestatus = maxCharge;
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI] - FNGenerator.OnRescale " + e.Message);
            }
        }

        public float GetModuleCost(float defaultCost, ModifierStagingSituation sit)
        {
            if (storedMassMultiplier == 1 && emptyCost != 0)
                moduleCost = emptyCost; 
            else
                moduleCost = dryCost * Mathf.Pow((float)storedScalingfactor, 3);

            return moduleCost;
        }
        
        public ModifierChangeWhen GetModuleCostChangeWhen()
        {
            if (HighLogic.LoadedSceneIsEditor)
                return ModifierChangeWhen.CONSTANTLY;
            else
                return ModifierChangeWhen.STAGED;
        }

        private void UpdateTargetMass()
        {
            // verify if mass calculation is active
            if (!calculatedMass)
            {
                targetMass = part.mass;
                return;
            }

            targetMass = (((maxTemperature - 30d) / 2000d) + ((double)(decimal)maxGeeforce / 20d)) * storedMassMultiplier;
            targetMass *= (1d - (0.2 * attachedAntimatterTanksCount));
        }

        public ModifierChangeWhen GetModuleMassChangeWhen()
        {
            if (HighLogic.LoadedSceneIsFlight)
                return ModifierChangeWhen.STAGED;
            else
                return ModifierChangeWhen.CONSTANTLY;
        }

        public float GetModuleMass(float defaultMass, ModifierStagingSituation sit)
        {
            if (!calculatedMass)
                return 0;

            moduleMassDelta = (float)(targetMass - initialMass);

            return moduleMassDelta;
        }

        public void doExplode(string reason = null)
        {
            if (current_antimatter <= 0.1f) return;

            if (!string.IsNullOrEmpty(reason))
            {
                Debug.Log("[KSPI] - " + reason);
                ScreenMessages.PostScreenMessage(reason, 10.0f, ScreenMessageStyle.UPPER_CENTER);
            }

            lightGameObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            lightGameObject.GetComponent<Collider>().enabled = false;
            lightGameObject.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            lightGameObject.AddComponent<Light>();
            var renderer = lightGameObject.GetComponent<Renderer>();
            renderer.material.shader = Shader.Find("Unlit/Transparent");
            renderer.material.mainTexture = GameDatabase.Instance.GetTexture("WarpPlugin/ParticleFX/explode", false);
            renderer.material.color = new Color(Color.white.r, Color.white.g, Color.white.b, 0.9f);
            Light light = lightGameObject.GetComponent<Light>();
            lightGameObject.transform.position = part.transform.position;
            light.type = LightType.Point;
            light.color = Color.white;
            light.range = 100f;
            light.intensity = 500000.0f;
            light.renderMode = LightRenderMode.ForcePixel;
            Destroy(lightGameObject, 0.25f);
            exploding = true;
        }

        public override void OnStart(PartModule.StartState state)
        {
            if (canExplodeFromHeat)
                part.maxTemp = (double)(decimal)maxTemperature;

            if (canExplodeFromGeeForce)
            {
                part.crashTolerance = maxGeeforce;
                part.gTolerance = maxGeeforce;
            }

            containerStates = SetUpAnimation(animationName, this.part);

            part.OnJustAboutToBeDestroyed += OnJustAboutToBeDestroyed;

            antimatterResource = part.Resources[resourceName];

            antimatterResource.isTweakable = true;

            antimatterDefinition = PartResourceLibrary.Instance.GetDefinition(resourceName);

            antimatterDensity = (double)(decimal)antimatterDefinition.density;

            minimimAnimatterAmount = 1e-13 /  antimatterDefinition.density * antimatterResource.maxAmount;

            partMass = part.mass;
            initialMass = part.prefabMass * storedMassMultiplier;

            if (state == StartState.Editor)
            {
                part.OnEditorAttach += OnEditorAttach;
                part.OnEditorDetach += OnEditorDetach;

                UpdateTargetMass();
                return;
            }
            else
                UpdateTargetMass();

            // charge if there is any significant antimatter
            should_charge = antimatterResource.amount > minimimAnimatterAmount;

            this.enabled = true;

            UpdateAttachedTanks();
        }

        void OnJustAboutToBeDestroyed()
        {
            if (HighLogic.LoadedSceneIsEditor || current_antimatter <= minimimAnimatterAmount || !FlightGlobals.VesselsLoaded.Contains(this.vessel)) return;

            if (part.temperature >= part.maxTemp)
                doExplode("Antimatter container exploded because antimatter melted and breached containment");
            else if (part.vessel.geeForce >= part.gTolerance)
                doExplode("Antimatter container exploded because exceeding gee force Tolerance");
            else
                doExplode("Antimatter container exploded because containment was breached");
            ExplodeContainer();
        }

        private void OnEditorAttach()
        {
            UpdateAttachedTanks();
            UpdateTargetMass();
        }

        private void OnEditorDetach()
        {
            if (attachedAntimatterTanks != null)
                attachedAntimatterTanks.ForEach(m => m.UpdateMass());

            UpdateAttachedTanks();
            UpdateTargetMass();
        }

        private void UpdateMass()
        {
            if (part.attachNodes == null) return;

            attachedAntimatterTanksCount = part.attachNodes.Where(m => m.nodeType == AttachNode.NodeType.Stack && m.attachedPart != null).Select(m => m.attachedPart.FindModuleImplementing<AntimatterStorageTank>()).Where(m => m != null).Count();
            UpdateTargetMass();
        }

        private void UpdateAttachedTanks()
        {
            if (part.attachNodes == null) return;

            attachedAntimatterTanks = part.attachNodes.Where(m => m.nodeType == AttachNode.NodeType.Stack && m.attachedPart != null).Select(m => m.attachedPart.FindModuleImplementing<AntimatterStorageTank>()).Where(m => m != null).ToList();
            attachedAntimatterTanks.ForEach(m => m.UpdateMass());
            attachedAntimatterTanksCount = attachedAntimatterTanks.Count();
        }

        public void Update()
        {
            //if (HighLogic.LoadedSceneIsEditor)
            //{
            //    antimatterResource.amount = ((double)(decimal)resourceFloatRange / 100) * antimatterResource.maxAmount ;
            //}

            if (containerStates != null)
            {
                animationRatio = (float)Math.Round(Math.Pow(antimatterResource.maxAmount > 0 ? antimatterResource.amount / antimatterResource.maxAmount : 0, animationExponent), 3);
                foreach (var cs in containerStates)
                {
                    cs.normalizedTime = animationRatio;
                }
            }

            UpdateAmounts();
            UpdateTargetMass();
            partMass = part.mass;
            partCost = part.partInfo.cost;

            if (HighLogic.LoadedSceneIsEditor)
            {
                chargestatus = maxCharge;

                Fields["maxGeeforce"].guiActiveEditor = canExplodeFromGeeForce;
                Fields["maxTemperature"].guiActiveEditor = canExplodeFromHeat;
                return;
            }

            Fields["TemperatureStr"].guiActive = canExplodeFromHeat;
            Fields["GeeforceStr"].guiActive = canExplodeFromGeeForce;

            Events["StartCharge"].active = current_antimatter <= 0.1 && !should_charge;
            Events["StopCharge"].active = current_antimatter <= 0.1 && should_charge;

            chargeStatusStr = chargestatus.ToString("0.0") + " / " + maxCharge.ToString("0.0");
            TemperatureStr = part.temperature.ToString("0") + " / " + maxTemperature.ToString("0");
            GeeforceStr = part.vessel.geeForce.ToString("0.0") + " / " + maxGeeforce.ToString("0.0");

            if (chargestatus <= 60 && !charging && current_antimatter > minimimAnimatterAmount)
                ScreenMessages.PostScreenMessage("Warning!: Antimatter storage unpowered, tank explosion in: " + chargestatus.ToString("0") + "s", 0.5f, ScreenMessageStyle.UPPER_CENTER);

            if (current_antimatter > 0.1)
            {
                if (charging)
                    statusStr = "Charging.";
                else
                    statusStr = "Discharging!";
            }
            else
            {
                if (should_charge)
                    statusStr = "Charging.";
                else
                    statusStr = "No Power Required.";
            }

            UpdateAmounts();
        }

        private void UpdateAmounts()
        {
            capacityStr = formatMassStr(antimatterResource.amount * antimatterDensity);
            maxAmountStr = formatMassStr(antimatterResource.maxAmount * antimatterDensity);
        }

        public void FixedUpdate()
        {
            if (HighLogic.LoadedSceneIsEditor)
                return;

            fixedDeltaTime = (double)(decimal)Math.Round(TimeWarp.fixedDeltaTime,7);

            MaintainContainment();

            ExplodeContainer();
        }

        [KSPEvent(guiActive = true, guiName = "Self Destruct", active = true)]
        public void SelfDestruct()
        {
            if (HighLogic.LoadedSceneIsEditor || current_antimatter <= minimimAnimatterAmount) return;

            doExplode("Antimatter container exploded because self destruct was activated");
        }

        private void MaintainContainment()
        {
            if (antimatterResource == null) return;

            double mult = 1;
            current_antimatter = antimatterResource.amount;

            if (chargestatus > 0 && (current_antimatter > 0.00001 * antimatterResource.maxAmount))
                chargestatus -= fixedDeltaTime;

            if (chargestatus >= maxCharge)
                mult = 0.5;

            if (!should_charge && current_antimatter <= 0.00001 * antimatterResource.maxAmount) return;

            var powerRequest = mult * 2 * chargeNeeded / 1000 * fixedDeltaTime;

            // first try to accespower  megajoules
            double charge_to_add = CheatOptions.InfiniteElectricity
                ? powerRequest 
                : consumeFNResource(powerRequest, FNResourceManager.FNRESOURCE_MEGAJOULES) * 1000 / chargeNeeded;

            chargestatus += charge_to_add;

            // alternatively  just look for any reserves of stored megajoules
            if (charge_to_add == 0)
            {
                double more_charge_to_add = part.RequestResource(FNResourceManager.FNRESOURCE_MEGAJOULES, powerRequest) * 1000 / chargeNeeded;

                charge_to_add += more_charge_to_add;
                chargestatus += more_charge_to_add;
            }

            // if still not found any power attempt to find any electricc charge to survive
            if (charge_to_add < fixedDeltaTime)
            {
                double more_charge_to_add = part.RequestResource(FNResourceManager.STOCK_RESOURCE_ELECTRICCHARGE, mult * 2 * chargeNeeded * fixedDeltaTime) / chargeNeeded;
                charge_to_add += more_charge_to_add;
                chargestatus += more_charge_to_add;
            }

            if (charge_to_add >= fixedDeltaTime)
                charging = true;
            else
            {
                charging = false;
                if (TimeWarp.CurrentRateIndex > 3 && (current_antimatter > minimimAnimatterAmount))
                {
                    TimeWarp.SetRate(3, true);
                    ScreenMessages.PostScreenMessage("Cannot Time Warp faster than 50x while Antimatter Tank is Unpowered", 1, ScreenMessageStyle.UPPER_CENTER);
                }
            }

            if (startup_timeout > 0)
                startup_timeout--;

            if (startup_timeout == 0 && current_antimatter > minimimAnimatterAmount)
            {
                //verify temperature
                if (!CheatOptions.IgnoreMaxTemperature &&  canExplodeFromHeat && part.temperature > maxTemperature)
                {
                    temperature_explode_counter++;
                    if (temperature_explode_counter > 10)
                        doExplode("Antimatter container exploded due to reaching critical temperature");
                }
                else
                    temperature_explode_counter = 0;

                //verify geeforce
                if (!CheatOptions.UnbreakableJoints && canExplodeFromGeeForce && part.vessel.geeForce > maxGeeforce)
                {
                    geeforce_explode_counter++;
                    if (geeforce_explode_counter > 10)
                        doExplode("Antimatter container exploded due to reaching critical geeforce");
                }
                else
                    geeforce_explode_counter = 0;

                //verify power
                if (chargestatus <= 0)
                {
                    chargestatus = 0;
                    if (!CheatOptions.InfiniteElectricity && current_antimatter > 0.00001 * antimatterResource.maxAmount)
                    {
                        power_explode_counter++;
                        if (power_explode_counter > 10)
                            doExplode("Antimatter container exploded due to running out of power");
                    }
                }
                else
                    power_explode_counter = 0;
            }
            else
            {
                temperature_explode_counter = 0;
                geeforce_explode_counter = 0;
                power_explode_counter = 0;
            }


            if (chargestatus > maxCharge)
                chargestatus = maxCharge;
        }

        private void ExplodeContainer()
        {
            if (!exploding || lightGameObject == null) return;

            explosion_size = Mathf.Sqrt((float)current_antimatter) * 5;

            cur_explosion_size += (float)fixedDeltaTime * explosion_size * explosion_size / explosion_time;
            lightGameObject.transform.localScale = new Vector3(Mathf.Sqrt(cur_explosion_size), Mathf.Sqrt(cur_explosion_size), Mathf.Sqrt(cur_explosion_size));
            lightGameObject.GetComponent<Light>().range = Mathf.Sqrt(cur_explosion_size) * 15f;
            lightGameObject.GetComponent<Collider>().enabled = false;

            TimeWarp.SetRate(0, true);
            vessel.GoOffRails();

            Vessel[] list_of_vessels_to_explode = FlightGlobals.Vessels.ToArray();
            foreach (Vessel vess_to_explode in list_of_vessels_to_explode)
            {
                if (Vector3d.Distance(vess_to_explode.transform.position, vessel.transform.position) > explosion_size) continue;

                if (vess_to_explode.packed) continue;

                Part[] parts_to_explode = vess_to_explode.Parts.ToArray();
                foreach (Part part_to_explode in parts_to_explode)
                {
                    if (part_to_explode != null)
                        part_to_explode.explode();
                }
            }

            Part[] explode_parts = vessel.Parts.ToArray();
            foreach (Part explode_part in explode_parts)
            {
                if (explode_part != vessel.rootPart && explode_part != this.part)
                    explode_part.explode();
            }
            vessel.rootPart.explode();
            this.part.explode();
        }

        public override string GetInfo()
        {
            return "Maximum Power Requirements: " + (chargeNeeded * 2).ToString("0") + " KW\nMinimum Power Requirements: " + chargeNeeded.ToString("0") + " KW";
        }

        public override int getPowerPriority()
        {
            return 1;
        }

        protected string formatMassStr(double mass)
        {
            if (mass >= 1)
                return (mass / 1e+0).ToString("0.0000000") + " t";
            else if (mass >= 1e-3)
                return (mass / 1e-3).ToString("0.0000000") + " kg";
            else if (mass >= 1e-6)
                return (mass / 1e-6).ToString("0.0000000") + " g";
            else if (mass >= 1e-9)
                return (mass / 1e-9).ToString("0.0000000") + " mg";
            else if (mass >= 1e-12)
                return (mass * 1e-12).ToString("0.000000") + " ug";
            else if (mass > 1e-15)
                return (mass * 1e-15).ToString("0.0000000") + " ng";
            else
                return (mass * 1e-18).ToString("0.0000000") + " pg";
        }

        public static AnimationState[] SetUpAnimation(string animationName, Part part) 
        {
            if (String.IsNullOrEmpty(animationName))
                return null;

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

}

