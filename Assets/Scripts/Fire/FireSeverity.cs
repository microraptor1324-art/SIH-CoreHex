using System;
using UnityEngine;

namespace ARMiningSimulator.Fire
{
    public enum FireSeverity
    {
        Small,
        Medium,
        Large
    }

    public enum FireState
    {
        Igniting,
        Burning,
        Extinguishing,
        Extinguished
    }

    /// <summary>
    /// Configuration profiles for each fire severity level.
    /// Controls damage rate, visual scale, smoke emission, and fightable status.
    /// </summary>
    [Serializable]
    public class FireSeverityConfig
    {
        [Header("Severity Level")]
        public FireSeverity severity;

        [Header("Combat & Damage")]
        [Tooltip("Health damage per second to trainee within hazard radius")]
        public float damageRate = 5f;

        [Tooltip("Effective damage radius in meters")]
        public float damageRadius = 2.0f;

        [Tooltip("Whether this fire can safely be fought by trainee with extinguisher")]
        public bool isExtinguishable = true;

        [Header("Visual Effects Scaling")]
        public float particleScale = 0.5f;
        public float fireEmissionRate = 25f;
        public float smokeEmissionRate = 15f;
        public float lightIntensity = 2.5f;
        public float lightRange = 3.5f;

        public static FireSeverityConfig GetDefault(FireSeverity level)
        {
            switch (level)
            {
                case FireSeverity.Small:
                    return new FireSeverityConfig
                    {
                        severity = FireSeverity.Small,
                        damageRate = 3f,
                        damageRadius = 1.8f,
                        isExtinguishable = true,
                        particleScale = 0.45f,
                        fireEmissionRate = 20f,
                        smokeEmissionRate = 12f,
                        lightIntensity = 2.0f,
                        lightRange = 3.0f
                    };
                case FireSeverity.Medium:
                    return new FireSeverityConfig
                    {
                        severity = FireSeverity.Medium,
                        damageRate = 8f,
                        damageRadius = 2.8f,
                        isExtinguishable = true,
                        particleScale = 0.85f,
                        fireEmissionRate = 45f,
                        smokeEmissionRate = 35f,
                        lightIntensity = 3.5f,
                        lightRange = 5.0f
                    };
                case FireSeverity.Large:
                default:
                    return new FireSeverityConfig
                    {
                        severity = FireSeverity.Large,
                        damageRate = 18f,
                        damageRadius = 4.0f,
                        isExtinguishable = false, // Rule: Large fire should NOT be fought
                        particleScale = 1.4f,
                        fireEmissionRate = 85f,
                        smokeEmissionRate = 75f,
                        lightIntensity = 5.5f,
                        lightRange = 8.0f
                    };
            }
        }
    }
}
