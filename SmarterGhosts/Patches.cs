using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace SmarterGhosts
{
    [HarmonyPatch]
    public class Patches
    {
        [HarmonyTranspiler] //Lower illumination time requirement to enter GhostAction
        [HarmonyPatch(typeof(CallForHelpAction), nameof(CallForHelpAction.CalculateUtility))]
        public static IEnumerable<CodeInstruction> CallForHelpAction_CalculateUtility_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions).MatchForward(false, new CodeMatch(OpCodes.Ldc_R4, 4f));

            matcher.RemoveInstruction().Insert(new CodeInstruction(OpCodes.Ldc_R4, 1f));

            return matcher.InstructionEnumeration();
        }


        //Steve no longer tolerates having their movie time interrupted
        public static DreamObjectProjection TheaterDoor;
        public static GhostBrain TheaterGhost;

        [HarmonyTranspiler] 
        [HarmonyPatch(typeof(GhostHotelDirector), nameof(GhostHotelDirector.Update))]
        public static IEnumerable<CodeInstruction> GhostHotelDirector_Update_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions).MatchForward(false, 
                new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(AutoSlideProjector), nameof(AutoSlideProjector.TurnOff)))
                ).Advance(1);

            matcher.Insert(Transpilers.EmitDelegate(() =>
            {
                if (TheaterDoor == null || TheaterGhost == null) return;
                TheaterDoor.SetVisible(false);
                TheaterGhost.WakeUp();
                TheaterGhost.EscalateThreatAwareness(GhostData.ThreatAwareness.SomeoneIsInHere);
            }));

            return matcher.InstructionEnumeration();
        }
    }
}
