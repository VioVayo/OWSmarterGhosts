using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static SmarterGhosts.SmarterGhosts;

namespace SmarterGhosts
{
    public class MoreGhosts
    {
        public static void Setup()
        {
            var newGhosts = ReactivateGhosts("Prefab_IP_GhostBird_TheCollector");

            if (GameObject.Find("GhostDirector_Zone2")?.TryGetComponent(out GhostZone2Director zone2Director) ?? false)
            {
                AddGhosts(ref zone2Director._directedGhosts, newGhosts, "Prefab_IP_GhostBird_TheCollector");
                AddGhosts(ref zone2Director._undergroundGhosts, newGhosts, "Prefab_IP_GhostBird_TheCollector");
            }

            newGhosts.FirstOrDefault(obj => obj.gameObject.name == "Prefab_IP_GhostBird_TheCollector").WakeUp();
        }


        private static List<GhostBrain> ReactivateGhosts(params string[] objNames)
        {
            var newGhosts = new List<GhostBrain>(objNames.Length);
            var owlObjects = Resources.FindObjectsOfTypeAll<GameObject>().Where(obj => Array.Exists(objNames, name => obj.name == name));
            foreach (var owlObj in owlObjects)
            {
                owlObj.SetActive(true);
                owlObj.GetComponentInChildren<DreamLanternController>().enabled = true;
                owlObj.GetComponent<GhostBrain>().enabled = true;
                newGhosts.Add(owlObj.GetComponent<GhostBrain>());
                Ghosts.Add(owlObj.GetComponent<GhostBrain>());
            }
            return newGhosts;
        }

        private static void AddGhosts(ref GhostBrain[] addTo, List<GhostBrain> addFrom, params string[] objNames)
        {
            if (addTo == null) return;
            var list = addTo.ToList();
            list.AddRange(addFrom.Where(obj => Array.Exists(objNames, name => obj.gameObject.name == name)));
            addTo = list.ToArray();
        }
    }
}
