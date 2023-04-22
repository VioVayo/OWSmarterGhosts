using GhostEnums;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;
using static SmarterGhosts.SmarterGhosts;

namespace SmarterGhosts
{
    [HarmonyPatch]
    public class CallForHelp
    {
        public static void Setup()
        {
            CoordinateZone2();
            CoordinateZone3();
        }


        private static void CoordinateZone2()
        {
            if (DirectorZone2 == null) return;

            var pointCity = GameObject.Find("GuardVolume_LowerCity/ChokePoint").transform;
            var volumeCity = pointCity.parent.gameObject.GetComponent<OWTriggerVolume>();

            foreach (var ghost in DirectorZone2._undergroundGhosts) StartGuarding(ghost, pointCity, volumeCity, DirectorZone2._undergroundGhosts.Where(helper => helper != ghost));
        }

        private static void CoordinateZone3()
        {
            if (DirectorHotel == null) return;

            var pointGarden = GameObject.Find("GuardVolume_RockGarden/ChokePoint").transform;
            var volumeGarden = pointGarden.parent.gameObject.GetComponent<OWTriggerVolume>();
            volumeGarden.gameObject.GetComponent<BoxShape>().size += new Vector3(32, 0, 0);
            volumeGarden.gameObject.transform.localPosition += new Vector3(-8, 0, 0);
            pointGarden.localPosition = new(6, -6, 21);

            var pointDiningHall = GameObject.Find("GuardVolume_Library/ChokePoint").transform;
            var volumeDiningHall = pointDiningHall.parent.gameObject.GetComponent<OWTriggerVolume>();
            volumeDiningHall.gameObject.GetComponent<BoxShape>().size += new Vector3(10, -4, 31);
            volumeDiningHall.gameObject.transform.localPosition += new Vector3(-5, 13, -8.5f);
            pointDiningHall.localPosition = new(-5, -4, -30);
            pointDiningHall.rotation = Quaternion.AngleAxis(180, pointDiningHall.up) * pointDiningHall.rotation;

            var gardenGhost = DirectorHotel._hotelDepthsGhosts.FirstOrDefault(obj => obj.gameObject.name == "Prefab_IP_GhostBird_Bou");
            var diningHallGhost = DirectorHotel._hotelDepthsGhosts.FirstOrDefault(obj => obj.gameObject.name == "Prefab_IP_GhostBird_NoFace");
            if (gardenGhost == null || diningHallGhost == null) return;

            StartGuarding(gardenGhost, pointGarden, volumeGarden, new[] { diningHallGhost });
            StartGuarding(diningHallGhost, pointDiningHall, volumeDiningHall, new[] { gardenGhost });
        }

        private static void StartGuarding(GhostBrain ghost, Transform chokePoint, OWTriggerVolume guardVolume, IEnumerable<GhostBrain> helpers)
        {
            if (ghost == null) return;

            ghost._chokePoint = chokePoint;
            ghost._guardVolume = guardVolume;
            ghost._helperGhosts = helpers.ToArray();

            ghost._controller.OnNodeMapChanged += new OWEvent.OWCallback(() =>
            {
                if (ghost._chokePoint == null) return;
                ghost._data.chokePointLocalPosition = ghost._controller.WorldToLocalPosition(ghost._chokePoint.position);
                ghost._data.chokePointLocalFacing = ghost._controller.WorldToLocalDirection(ghost._chokePoint.forward);
            });

            if (ghost._helperGhosts.Length == 0) return;

            var actionList = ghost._actions.ToList();
            actionList.Add(GhostAction.Name.CallForHelp);
            ghost._actions = actionList.ToArray();
        }


        //-----Patches-----
        [HarmonyPrefix] //Don't trigger if helper is already guarding choke point
        [HarmonyPatch(typeof(CallForHelpAction), nameof(CallForHelpAction.CalculateUtility))]
        public static bool CallForHelpAction_CalculateUtility_Prefix(CallForHelpAction __instance, ref float __result)
        {
            __result = -100;
            var helpers = __instance._controller.gameObject.GetComponent<GhostBrain>()._helperGhosts;
            for (int n = 0; n < helpers.Length; ++n)
            {
                if (helpers[n].GetCurrentActionName() == GhostAction.Name.CallForHelp) return false;
            }
            return true;
        }

        [HarmonyTranspiler] //Lower illumination time requirement to enter GhostAction
        [HarmonyPatch(typeof(CallForHelpAction), nameof(CallForHelpAction.CalculateUtility))]
        public static IEnumerable<CodeInstruction> CallForHelpAction_CalculateUtility_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions).MatchForward(true, 
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(GhostData), nameof(GhostData.illuminatedByPlayerMeter))),
                new CodeMatch(OpCodes.Ldc_R4)
            );

            matcher.SetOperandAndAdvance(1.5f);

            return matcher.InstructionEnumeration();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CallForHelpAction), nameof(CallForHelpAction.OnEnterAction))]
        public static void CallForHelpAction_OnEnterAction_Postfix(CallForHelpAction __instance)
        {
            __instance._effects.SetMovementStyle(GhostEffects.MovementStyle.Stalk);
        }

        [HarmonyPostfix] //Turn faster, else it looks weird
        [HarmonyPatch(typeof(CallForHelpAction), nameof(CallForHelpAction.FixedUpdate_Action))]
        public static void CallForHelpAction_FixedUpdate_Action_Postfix(CallForHelpAction __instance)
        {
            if (__instance.GetActionTimeElapsed() < 0.5f) __instance._controller.FaceLocalPosition(__instance._data.lastKnownPlayerLocation.localPosition, TurnSpeed.FASTEST);
        }
    }
}
