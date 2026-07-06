# MAUVAISE FOI (BAD FAITH)

> Jeu multijoueur 4-8 joueurs de **trahison sociale à déni plausible**, pensé pour la viralité Twitch/TikTok.
> Coopérez pour survivre. Acceptez des Pactes secrets pour trahir. Mentez au Tribunal. Tout est filmé.

**Nouveau sur le projet ? Lis [docs/PITCH.md](docs/PITCH.md) (5 min) puis [docs/gdd/00-vision.md](docs/gdd/00-vision.md).**

---

## 🚀 Démarrage rapide (15 min)

### Prérequis
- **Unity 6000.5.2f1** exactement (Unity Hub → Installations → Install Editor → Archive si besoin), avec le module *Windows Build Support*.
- Git.

### Lancer le jeu
1. Clone le repo.
2. Unity Hub → **Add project from disk** → sélectionne le dossier **`game/`** (pas la racine du repo).
3. Ouvre le projet, laisse importer/compiler (5-10 min la première fois). Zéro erreur rouge attendu en Console.
4. Menu **MAUVAISE FOI → Construire la scène J0** (reconstruit la scène de test complète — relançable à volonté).
5. **Play** → bouton **HEBERGER**. Pour tester à deux sur une machine : *File → Build And Run* (un .exe) + Play dans l'éditeur, l'un héberge, l'autre **REJOINDRE** avec `localhost`. Sur le même réseau local : l'IP de l'hôte.

### Contrôles (boîte grise)
| Touche | Action |
|---|---|
| ZQSD / souris / Espace / Shift | Bouger, viser, sauter, sprinter |
| **E** | Prendre / poser un objet |
| **Clic gauche** | Lancer l'objet porté |
| **G / H** (près du Terminal, objet en main) | Déposer au **pot commun** / en **poche perso** |
| **Tab (tenir)** | Consulter sa montre — **geste public, les autres le voient !** |
| **F** (montre ouverte) | Accepter le Pacte affiché |
| **B** (dans la capsule) | Bouton rouge — départ anticipé |
| **0-8** (au Tribunal) | Voter : 0 = accident naturel, 1-8 = accuser un joueur |
| Échap / clic | Libérer / recapturer la souris |

---

## 🗺️ Structure du repo

```
docs/PITCH.md          ← le jeu expliqué en 5 min (commence ici)
docs/gdd/              ← LE GAME DESIGN DOCUMENT — source de vérité du design.
                          Chaque système a son fichier, avec le POURQUOI des choix.
docs/SETUP.md          ← installation détaillée de l'environnement
game/                  ← le projet Unity 6 (URP)
  Assets/Scripts/Core/     ← logique de domaine en C# PUR (zéro using UnityEngine).
                              Économie, Pactes, accidents, scoring. Testable hors Unity.
  Assets/Scripts/Gameplay/ ← les NetworkBehaviours (FishNet) : adaptateurs fins autour de Core.
  Assets/Scripts/Editor/   ← J0SceneBuilder : construit la scène de test par code.
  Assets/FishNet/          ← netcode vendoré. ⚠️ NE PAS MODIFIER (voir Règles).
graphify-out/          ← knowledge graph du projet (requêtes : /graphify query "...")
```

## 🧠 Architecture en 30 secondes

- **Autorité hôte stricte** : toute la logique (Pactes, accidents, économie, morts) tourne chez l'hôte. Les clients envoient des intentions (`ServerRpc`), reçoivent des états (`SyncVar`, `ObserversRpc`).
- **Core vs Gameplay** : une règle de jeu se code dans `Core/` (C# pur), puis s'expose au réseau dans `Gameplay/`. Si tu écris une règle de gameplay dans un `NetworkBehaviour`, c'est un code smell.
- **La règle d'or du design — le déni plausible** : tout événement déclenchable par un Pacte doit pouvoir arriver naturellement, avec le MÊME code/sons/timings ([docs/gdd/04-deni-plausible.md](docs/gdd/04-deni-plausible.md)). L'`AccidentDirector` fabrique les accidents naturels ; le `HazardExecutor` exécute les deux indistinctement.
- L'aléatoire de gameplay passe par `IGameRandom` (injectable, seedable) — jamais de `Random` direct dans Core.

## ✅ État actuel (briques livrées)

| Brique | Contenu | État |
|---|---|---|
| J0 | Réseau FishNet, FPS, physique des objets, HUD héberger/rejoindre | ✅ |
| 1 | La Montre + les Pactes + Directeur d'Accidents (déni plausible) | ✅ |
| 2 | Économie : loot valorisé, Terminal, dépôt public pot/poche, quota | ✅ |
| 3 | Manche complète : mort (règle cruelle), capsule N-2 sièges, bouton rouge, fin | ✅ |
| 4 | LE TRIBUNAL : votes, verdicts, prime de mauvaise foi | ✅ |
| 5 | Le Juge (revolver unique, 1 balle, barillet privé) | 🔜 |
| 6 | Multi-manches + lobby | 🔜 |
| 7 | Voice de proximité (Dissonance — achat Asset Store ~85 $) | 🔜 |

Objectif : **jalon J1 du GDD** — le playtest de validation à 6 joueurs sur la boîte grise ([docs/gdd/10-roadmap.md](docs/gdd/10-roadmap.md)).

## 📏 Règles d'équipe (à lire avant de commiter)

1. **`game/Assets/FishNet/` est intouchable** — sauf le patch de compatibilité Unity 6000.5 déjà appliqué (`Scene.handle` → `GetRawData()`, documenté dans [CLAUDE.md](CLAUDE.md)). Si tu as besoin d'un comportement FishNet différent, on en parle d'abord.
2. **La scène `J0_GreyBox` ne se modifie PAS à la main** : elle est générée par `J0SceneBuilder`. Tu veux changer l'arène ? Modifie le builder et reconstruis. (Ça élimine 100 % des conflits de merge de scène.)
3. **Une scène Unity = une personne à la fois.** Si tu crées une scène de travail perso, préfixe-la `WIP_TonNom`.
4. **Rien du backlog n'entre au MVP** ([docs/gdd/99-backlog.md](docs/gdd/99-backlog.md)) : 8 Pactes, pas un de plus. Toute idée nouvelle → backlog, on triera après les playtests.
5. Commits conventionnels en français : `feat:`, `fix:`, `docs:`… Branches : `feat/nom-court`, PR vers `main`.
6. Avant d'implémenter un système, **relis son fichier GDD** — le pourquoi y est toujours documenté.

## 🐛 Dépannage courant

- **Erreurs `ObjectId 65535 ... Reserialize`** au Play : reconstruis la scène (menu MAUVAISE FOI) — le builder régénère les SceneIds FishNet.
- **`SpawnablePrefabs is null`** : même remède.
- **Pas de son de montre** : le buzz est spatial (2,5 m) — rapproche-toi du joueur concerné, ou de toi-même (il est audible par le porteur).
- Le projet ne compile plus après un pull : `Assets → Reimport All` en dernier recours.
