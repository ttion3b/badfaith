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

        /// <summary>Serveur : redistribue le loot (positions + valeurs).</summary>
        public void ServerResetRound()
        {
            var rng = new System.Random();
            foreach (var nob in _lootObjects)
            {
                if (nob == null)
                    continue;

                float x = (float)(rng.NextDouble() * 20.0 - 10.0);
                float z = (float)(rng.NextDouble() * 20.0 - 10.0);
                nob.transform.position = new Vector3(x, 0.7f, z);

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
