using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;
using static SmarterGhosts.SmarterGhosts;

namespace SmarterGhosts
{
    [HarmonyPatch]
    public class ListenForSounds
    {
        private static PlayerNoiseMaker playerNoiseMaker;
        private static float
            verySoftRadius = 7.5f,
            softRadius = 10,
            mediumRadius = 17, 
            loudRadius = 22, 
            veryLoudRadius = 25,
            currentRadius;


        public static void Setup()
        {
            playerNoiseMaker = GameObject.Find("PlayerDetector").GetComponent<PlayerNoiseMaker>();

            foreach (var ghost in Ghosts)
            {
                var noiseSensor = ghost.gameObject.AddComponent<NoiseSensor>();
                noiseSensor.OnAudibleNoise += (NoiseMaker noiseMaker) =>
                {
                    if (!ghost._data.isAlive || !ghost._data.hasWokenUp) return;

                    var localNoisePosition = (noiseMaker is PlayerNoiseMaker) ?
                            ghost._data.playerLocation.localPosition :
                            ghost._controller.WorldToLocalPosition(noiseMaker.GetNoiseOrigin());
                    ghost.HintPlayerLocation(localNoisePosition, Time.time);

                    if (Vector3.Distance(localNoisePosition, ghost._controller.GetLocalFeetPosition()) <= veryLoudRadius) ReactToNoise(ghost);
                };
            }
        }

        private static void ReactToNoise(GhostBrain ghost)
        {
            ghost._data.reduceGuardUtility = true;
            if (ghost.GetThreatAwareness() >= GhostData.ThreatAwareness.IntruderConfirmed) return;
            if (ghost.GetAction(GhostAction.Name.IdentifyIntruder) != null && ghost.GetCurrentActionName() != GhostAction.Name.IdentifyIntruder)
                ghost.ChangeAction(GhostAction.Name.IdentifyIntruder);
            else if (ghost.GetCurrentActionName() == GhostAction.Name.Sentry)
            {
                var action = ghost.GetCurrentAction() as SentryAction;
                action._spotlighting = true;
                action._controller.ChangeLanternFocus(1f, 2f);
            }
        }


        private static AudioType MakeNoise(AudioType audioType, float volume)
        {
            if (PlayerState.InDreamWorld()) 
            {
                currentRadius = 0;

                switch (audioType)
                {
                    case AudioType.ImpactHighSpeed:
                        currentRadius = veryLoudRadius;
                        break;
                    case AudioType.ImpactMediumSpeed:
                    case AudioType.Splash_Water_Probe:
                        currentRadius = loudRadius;
                        break;
                    case AudioType.MovementShallowWaterFootstep:
                    case AudioType.ImpactLowSpeed:
                    case AudioType.LandingStone:
                    case AudioType.LandingIce:
                    case AudioType.MovementGlassLanding:
                    case AudioType.LandingMetal:
                    case AudioType.LandingNomaiMetal:
                    case AudioType.MovementWoodCreakLanding:
                    case AudioType.MovementGravelLanding:
                    case AudioType.LandingDirt:
                        currentRadius = (volume >= 0.7f) ? mediumRadius : softRadius;
                        break;
                    case AudioType.MovementMetalFootstep:
                    case AudioType.MovementWoodCreakFootstep:
                    case AudioType.MovementGravelFootsteps:
                    case AudioType.LandingGrass:
                    case AudioType.LandingSand:
                    case AudioType.MovementSnowLanding:
                    case AudioType.MovementWoodLanding:
                    case AudioType.MovementLeavesLanding:
                        currentRadius = (volume >= 0.7f) ? softRadius : verySoftRadius;
                        break;
                    case AudioType.MovementGrassFootstep:
                    case AudioType.MovementStoneFootstep:
                    case AudioType.MovementSandFootstep:
                    case AudioType.MovementSnowFootstep:
                    case AudioType.MovementIceFootstep:
                    case AudioType.MovementGlassFootsteps:
                    case AudioType.MovementWoodFootstep:
                    case AudioType.MovementLeavesFootsteps:
                    case AudioType.MovementNomaiMetalFootstep:
                    case AudioType.MovementDirtFootstep:
                        currentRadius = verySoftRadius;
                        break;
                    default: break;
                }
            }
            return audioType; //for use in the transpilers below, audioType is returned unaltered to the stack
                              //volume is loaded again by the transpiler itself because I can't think of a better way to do this send help
        }


        //-----Patches-----
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(PlayerMovementAudio), nameof(PlayerMovementAudio.PlayFootstep))]
        public static IEnumerable<CodeInstruction> PlayerMovementAudio_PlayFootstep_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions).MatchForward(true,
                new CodeMatch(OpCodes.Ldloc_0),
                new CodeMatch(OpCodes.Ldc_R4, 0.7f),
                new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(OWAudioSource), nameof(OWAudioSource.PlayOneShot), new Type[] { typeof(AudioType), typeof(float) }))
            );

            matcher.Insert(
                Transpilers.EmitDelegate((Func<AudioType, float, AudioType>)MakeNoise),
                new CodeInstruction(OpCodes.Ldc_R4, 0.7f)
            );

            return matcher.InstructionEnumeration();
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(PlayerImpactAudio), nameof(PlayerImpactAudio.OnImpact))]
        public static IEnumerable<CodeInstruction> PlayerImpactAudio_OnImpact_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions).MatchForward(true,
                new CodeMatch(OpCodes.Ldloc_S),
                new CodeMatch(OpCodes.Ldloc_1),
                new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(OWAudioSource), nameof(OWAudioSource.PlayOneShot), new Type[] { typeof(AudioType), typeof(float) }))
            );

            matcher.Insert(
                Transpilers.EmitDelegate((Func<AudioType, float, AudioType>)MakeNoise),
                new CodeInstruction(OpCodes.Ldloc_1)
            );

            return matcher.InstructionEnumeration();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerNoiseMaker), nameof(PlayerNoiseMaker.Update))]
        public static void PlayerNoiseMaker_Update_Postfix() { playerNoiseMaker._noiseRadius = Mathf.Max(currentRadius, playerNoiseMaker._noiseRadius); }
    }
}
