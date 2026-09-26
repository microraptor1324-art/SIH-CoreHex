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
                        damageRate = 2.0f,
                        damageRadius = 1.2f,
                        isExtinguishable = true,
                        particleScale = 0.32f,
                        fireEmissionRate = 12f,
                        smokeEmissionRate = 5f,
                        lightIntensity = 1.3f,
                        lightRange = 1.8f
                    };
                case FireSeverity.Medium:
                    return new FireSeverityConfig
                    {
                        severity = FireSeverity.Medium,
                        damageRate = 8f,
                        damageRadius = 2.5f,
                        isExtinguishable = true,
                        particleScale = 0.90f,
                        fireEmissionRate = 45f,
                        smokeEmissionRate = 30f,
                        lightIntensity = 3.5f,
                        lightRange = 4.8f
                    };
                case FireSeverity.Large:
                default:
                    return new FireSeverityConfig
                    {
                        severity = FireSeverity.Large,
                        damageRate = 22f,
                        damageRadius = 4.5f,
                        isExtinguishable = false, // Rule: Large fire should NOT be fought
                        particleScale = 2.6f,
                        fireEmissionRate = 130f,
                        smokeEmissionRate = 110f,
                        lightIntensity = 8.0f,
                        lightRange = 11.0f
                    };
            }
        }
    }
}
