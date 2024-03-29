﻿using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;
using static SmarterGhosts.SmarterGhosts;

namespace SmarterGhosts
{
    [HarmonyPatch]
    public class WakeEarly
    {
        private static DreamObjectProjection theaterDoor, upstairsDoor;
        private static GhostBrain theaterGhost;


        public static void Setup()
        {
            var doors = GameObject.Find("Interactibles_Hotel/DoorProjections");

            var door1 = doors?.transform.Cast<Transform>().FirstOrDefault(obj => obj.localPosition == new Vector3(32, -21, 66));
            theaterDoor = door1?.Find("Door_Projection").gameObject.GetComponent<DreamObjectProjection>();
            theaterGhost = DirectorHotel._hotelDepthsGhosts.FirstOrDefault(obj => obj.gameObject.name == "Prefab_IP_GhostBird_Bou");

            var door2 = doors?.transform.Find("Prefab_IP_DW_Door_Projection (1)").transform;
            upstairsDoor = door2?.Find("Door_Projection").gameObject.GetComponent<DreamObjectProjection>();
        }


        //-----Patches-----
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(GhostHotelDirector), nameof(GhostHotelDirector.Update))]
        public static IEnumerable<CodeInstruction> GhostHotelDirector_Update_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions).MatchForward(false,
                new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(AutoSlideProjector), nameof(AutoSlideProjector.TurnOff)))
            ).Advance(1);

            matcher.Insert(Transpilers.EmitDelegate(() =>
            {
                if (theaterDoor == null || theaterGhost == null) return;
                theaterDoor.SetVisible(false);
                theaterGhost.WakeUp();
                theaterGhost.EscalateThreatAwareness(GhostData.ThreatAwareness.SomeoneIsInHere);
            }));

            return matcher.InstructionEnumeration();
        }


        [HarmonyPrefix]
        [HarmonyPatch(typeof(GhostBrain), nameof(GhostBrain.HearCallForHelp))]
        public static void GhostBrain_HearCallForHelp_Prefix(GhostBrain __instance)
        {
            if (__instance._data.hasWokenUp) return;

            if (__instance == DirectorHotel._cafeGhost)
            {
                if (upstairsDoor == null) return;
                upstairsDoor.SetVisible(false);
                __instance.WakeUp();
            }
            else if (DirectorZone2._undergroundGhosts.Contains(__instance)) 
            {
                __instance.WakeUp();
                __instance.GetEffects().CancelStompyFootsteps();

                for (int n = 0; n < DirectorZone2._elevatorsStatus.Length; ++n) if (DirectorZone2._elevatorsStatus[n].elevatorPair.ghost == __instance)
                {
                    DirectorZone2._elevatorsStatus[n].elevatorPair.elevator.topLight.FadeTo(0f, 0.2f);
                    DirectorZone2._elevatorsStatus[n].elevatorAction = DirectorZone2._elevatorsStatus[n].elevatorPair.ghost.GetAction(GhostAction.Name.ElevatorWalk) as ElevatorWalkAction;
                    DirectorZone2._elevatorsStatus[n].elevatorAction.CallToUseElevator();
                    DirectorZone2._elevatorsStatus[n].ghostController = DirectorZone2._elevatorsStatus[n].elevatorPair.ghost.GetComponent<GhostController>();
                }
            }
        }

        [HarmonyTranspiler] //Move elevator code outside of the if check and add null checks to prevent resulting NREs
        [HarmonyPatch(typeof(GhostZone2Director), nameof(GhostZone2Director.Update))]
        public static IEnumerable<CodeInstruction> GhostZone2Director_Update_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator).MatchForward(false,
                new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(DreamObjectProjector), nameof(DreamObjectProjector.SetLit)))
            ).Advance(1).CreateLabel(out Label loopStart);

            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldloc_0),
                new CodeMatch(OpCodes.Ldc_I4_1),
                new CodeMatch(OpCodes.Add),
                new CodeMatch(OpCodes.Stloc_0)
            ).CreateLabel(out Label loopEnd);

            matcher.Advance(-matcher.Pos);

            matcher.MatchForward(true,
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(GhostZone2Director), nameof(GhostZone2Director._lightsProjectorExtinguished))),
                new CodeMatch(OpCodes.Brfalse)
            ).SetOperandAndAdvance(loopStart);

            matcher.MatchForward(true, 
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(GhostZone2Director), nameof(GhostZone2Director._elevatorsStatus))),
                new CodeMatch(OpCodes.Ldloc_0)
            ).Advance(1).Insert(
                Transpilers.EmitDelegate<Func<GhostZone2Director.ElevatorStatus[], int, bool>>((elevatorsStatus, index) =>
                {
                    return elevatorsStatus[index].elevatorAction == null || elevatorsStatus[index].ghostController == null;
                }),
                new CodeInstruction(OpCodes.Brtrue, loopEnd),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(GhostZone2Director), nameof(GhostZone2Director._elevatorsStatus))),
                new CodeInstruction(OpCodes.Ldloc_0)
            );

            return matcher.InstructionEnumeration();
        }
    }
}
