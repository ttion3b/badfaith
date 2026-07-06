using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;

namespace BadFaith.Gameplay
{
    /// <summary>
    /// Replace tout le loot pour une nouvelle manche : les cubes déposés au
    /// Terminal (despawnés) sont respawnés, tous sont redistribués sur la map
    /// avec une nouvelle valeur.
    /// </summary>
    public class LootRespawner : NetworkBehaviour
    {
        private readonly List<NetworkObject> _lootObjects = new List<NetworkObject>();

        public override void OnStartServer()
        {
            base.OnStartServer();
            foreach (var loot in FindObjectsByType<LootItem>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var nob = loot.GetComponent<NetworkObject>();
                if (nob != null)
                    _lootObjects.Add(nob);
            }
        }

        /// <summary>Serveur : redistribue le loot (positions + valeurs), réparti dans les zones.</summary>
        public void ServerResetRound()
        {
            var rng = new System.Random();
            int index = 0;
            foreach (var nob in _lootObjects)
            {
                if (nob == null)
                    continue;

                Vector3 position;
                if (MapZone.All.Count > 0)
                {
                    var zone = MapZone.All[index % MapZone.All.Count];
                    position = zone.RandomPointInside(rng, shrink: 0.6f, y: 0.7f);
                }
                else
                {
                    position = new Vector3((float)(rng.NextDouble() * 20.0 - 10.0), 0.7f, (float)(rng.NextDouble() * 20.0 - 10.0));
                }
                index++;
                nob.transform.position = position;

                var rb = nob.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }

                if (!nob.IsSpawned)
                    ServerManager.Spawn(nob); // objet de scène despawné (déposé) : on le réactive

                var loot = nob.GetComponent<LootItem>();
                if (loot != null)
                    loot.ServerReroll();
            }
        }
    }
}
