using HarmonyLib;
using OWML.Common;
using OWML.ModHelper;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace SmarterGhosts
{
    public class SmarterGhosts : ModBehaviour
    {
        private GhostBrain[] ghostsZone2, ghostsZone3;

        private void Awake()
        {
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
        }

        private void Start()
        {
            ModHelper.Console.WriteLine($"My mod {nameof(SmarterGhosts)} is loaded!", MessageType.Success);

            LoadManager.OnCompleteSceneLoad += (scene, loadScene) =>
            {
                if (loadScene != OWScene.SolarSystem) return;
                AddGhosts();
                FindGhosts();
                CoordinateZone2();
                CoordinateZone3();
                SlideProjectionInterruptionSetup();
            };
        }

        private void FindGhosts()
        {
            ghostsZone2 = FindGhosts("Prefab_IP_GhostBird_TheCollector", "Prefab_IP_GhostBird_Zote", "Prefab_IP_GhostBird_Nosk");
            ghostsZone3 = FindGhosts("Prefab_IP_GhostBird_Bou", "Prefab_IP_GhostBird_NoFace");
        }

        private GhostBrain[] FindGhosts(params string[] objNames)
        {
            return objNames.Select(objName => GameObject.Find(objName)?.GetComponent<GhostBrain>()).ToArray();
        }


        //-----Reactivate removed Ghosts-----

        private void AddGhosts()
        {
            var directorZone2 = GameObject.Find("GhostDirector_Zone2")?.GetComponent<GhostZone2Director>();
            AddGhosts(directorZone2, "Prefab_IP_GhostBird_TheCollector");
        }

        private void AddGhosts(GhostDirector director, params string[] objNames)
        {
            var list = director?._directedGhosts.ToList();

            foreach (var objName in objNames)
            {
                var owlObj = Resources.FindObjectsOfTypeAll<GameObject>().First(obj => obj.name == objName);
                owlObj.SetActive(true);
                owlObj.GetComponentInChildren<DreamLanternController>().enabled = true;
                owlObj.GetComponent<GhostBrain>().enabled = true;
                list?.Add(owlObj.GetComponent<GhostBrain>());
            }

            if (director != null) director._directedGhosts = list.ToArray();
        }


        //-----Call For Help Setup-----

        private void CoordinateZone2()
        {
            var pointCity = GameObject.Find("GuardVolume_LowerCity/ChokePoint").transform;
            var volumeCity = pointCity.parent.gameObject.GetComponent<OWTriggerVolume>();

            foreach (var ghost in ghostsZone2) StartGuarding(ghost, pointCity, volumeCity, ghostsZone2.Where(helper => helper != ghost));
        }

        private void CoordinateZone3()
        {
            var pointGarden = GameObject.Find("GuardVolume_RockGarden/ChokePoint").transform;
            var volumeGarden = pointGarden.parent.gameObject.GetComponent<OWTriggerVolume>();
            volumeGarden.gameObject.GetComponent<BoxShape>().size += new Vector3(32, 0, 0);
            volumeGarden.gameObject.transform.localPosition += new Vector3(-8, 0, 0);
            pointGarden.localPosition = new(6, -6, 22);

            var pointDiningHall = GameObject.Find("GuardVolume_Library/ChokePoint").transform;
            var volumeDiningHall = pointDiningHall.parent.gameObject.GetComponent<OWTriggerVolume>();
            volumeDiningHall.gameObject.GetComponent<BoxShape>().size += new Vector3(10, -4, 31);
            volumeDiningHall.gameObject.transform.localPosition += new Vector3(-5, 13, -8.5f);
            pointDiningHall.localPosition = new(-5, -4, -30);

            StartGuarding(ghostsZone3[0], pointGarden, volumeGarden, new[] { ghostsZone3[1] });
            StartGuarding(ghostsZone3[1], pointDiningHall, volumeDiningHall, new[] { ghostsZone3[0] });
        }

        private void StartGuarding(GhostBrain ghost, Transform chokePoint, OWTriggerVolume guardVolume, IEnumerable<GhostBrain> helpers)
        {
            if (ghost == null) return;

            ghost._chokePoint = chokePoint;
            ghost._guardVolume = guardVolume;
            ghost._helperGhosts = helpers.Where(helper => helper != null).ToArray();

            if (ghost._helperGhosts.Length == 0) return;

            var actionList = ghost._actions.ToList();
            actionList.Add(GhostAction.Name.CallForHelp);
            ghost._actions = actionList.ToArray();
        }


        //-----Justice for Projector Owl-----

        private void SlideProjectionInterruptionSetup()
        {
            var door = GameObject.Find("Interactibles_Hotel/DoorProjections")?.transform.Cast<Transform>().First(obj => obj.localPosition == new Vector3(32, -21, 66));
            Patches.TheaterDoor = door?.Find("Door_Projection").gameObject.GetComponent<DreamObjectProjection>();
            Patches.TheaterGhost = ghostsZone3[0];
        }
    }
}