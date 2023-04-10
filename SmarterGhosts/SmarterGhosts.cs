using HarmonyLib;
using OWML.ModHelper;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace SmarterGhosts
{
    public class SmarterGhosts : ModBehaviour
    {
        public static SmarterGhosts ModInstance;

        public static List<GhostBrain> Ghosts;
        public static GhostZone2Director DirectorZone2;
        public static GhostHotelDirector DirectorHotel;


        private void Awake()
        {
            ModInstance = this;
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
        }

        private void Start()
        {
            LoadManager.OnCompleteSceneLoad += (scene, loadScene) =>
            {
                if (loadScene != OWScene.SolarSystem) return;
                ModSetup();

                MoreGhosts.Setup();
                CallForHelp.Setup();
                ListenForSounds.Setup();
                WakeEarly.Setup();
            };
        }

        private void ModSetup()
        {
            Ghosts = FindObjectsOfType<GhostBrain>().ToList();

            DirectorZone2 = GameObject.Find("GhostDirector_Zone2")?.GetComponent<GhostZone2Director>();
            DirectorZone2._startAwake = false;

            DirectorHotel = GameObject.Find("GhostDirector_Hotel")?.GetComponent<GhostHotelDirector>();
        }
    }
}