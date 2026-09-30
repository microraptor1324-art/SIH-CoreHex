using System;
using UnityEngine;

namespace ARMiningSimulator.Extinguisher
{
    public enum ExtinguisherType
    {
        CO2 = 0,         // Carbon Dioxide (Black Band) - Best for Electrical / Class C
        DryPowder = 1,   // ABC Chemical Powder (Blue Band) - Multi-purpose / Class A, B, C
        WaterFoam = 2    // AFFF Foam (Cream Band) - Class A & B Hydrocarbons. DANGEROUS on electrical!
    }

    [Serializable]
    public class ExtinguisherConfig
    {
        public ExtinguisherType type;
        public string displayName;
        public string colorBandName;
        public Color bandColor;
        public float effectiveRange = 3.5f;
        public float maxDischargeDuration = 12f;
        public float suppressionRate = 40f; // Informational only — suppression speed is ExtinguisherController._secondsToExtinguish (7s)
        public bool isEffectiveOnElectrical = true;
        public bool isHazardousOnElectrical = false;
        public string description;

        public static ExtinguisherConfig GetConfig(ExtinguisherType type)
        {
            switch (type)
            {
                case ExtinguisherType.CO2:
                    return new ExtinguisherConfig
                    {
                        type = ExtinguisherType.CO2,
                        displayName = "Carbon Dioxide (CO2) Extinguisher",
                        colorBandName = "Black Band",
                        bandColor = new Color(0.12f, 0.12f, 0.14f),
                        effectiveRange = 3.2f,
                        maxDischargeDuration = 14f,
                        suppressionRate = 45f,
                        isEffectiveOnElectrical = true,
                        isHazardousOnElectrical = false,
                        description = "Non-conductive gas that displaces oxygen and cools electrical components without residue. Standard choice for mining electrical panels."
                    };

                case ExtinguisherType.DryPowder:
                    return new ExtinguisherConfig
                    {
                        type = ExtinguisherType.DryPowder,
                        displayName = "ABC Dry Chemical Powder Extinguisher",
                        colorBandName = "Blue Band",
                        bandColor = new Color(0.15f, 0.45f, 0.90f),
                        effectiveRange = 4.0f,
                        maxDischargeDuration = 14f,
                        suppressionRate = 40f,
                        isEffectiveOnElectrical = true,
                        isHazardousOnElectrical = false,
                        description = "Multi-purpose ammonium phosphate powder. Smothers flammable liquids, mechanical oil fires, and electrical machinery."
                    };

                case ExtinguisherType.WaterFoam:
                default:
                    return new ExtinguisherConfig
                    {
                        type = ExtinguisherType.WaterFoam,
                        displayName = "AFFF Aqueous Foam Extinguisher",
                        colorBandName = "Cream Band",
                        bandColor = new Color(0.92f, 0.88f, 0.72f),
                        effectiveRange = 4.5f,
                        maxDischargeDuration = 15f,
                        suppressionRate = 35f,
                        isEffectiveOnElectrical = false,
                        isHazardousOnElectrical = true,
                        description = "Forms a blanket over burning hydrocarbons and wood. CRITICAL HAZARD: Conducts electricity! NEVER use on energized mining equipment."
                    };
            }
        }
    }
}
