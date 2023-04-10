using HarmonyLib;
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
        private static DreamObjectProjection theaterDoor;
        private static GhostBrain theaterGhost;


        public static void Setup()
        {
            var door = GameObject.Find("Interactibles_Hotel/DoorProjections")?.transform.Cast<Transform>().FirstOrDefault(obj => obj.localPosition == new Vector3(32, -21, 66));
            theaterDoor = door?.Find("Door_Projection").gameObject.GetComponent<DreamObjectProjection>();
            theaterGhost = DirectorHotel._hotelDepthsGhosts.FirstOrDefault(obj => obj.gameObject.name == "Prefab_IP_GhostBird_Bou");
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
    }
}
