# SETUP — environnement de développement

## État machine (audit du 2026-07-03)
- ✅ git — ❌ Unity Hub — ❌ Unity Editor — ❌ .NET SDK — ❌ Blender

## À installer (checklist, dans l'ordre)

### 1. Unity Hub + Unity 6 (obligatoire, ~30 min)
1. Télécharger Unity Hub : https://unity.com/download
2. Dans le Hub → Installs → Install Editor → **Unity 6 LTS (6000.0.x)**.
3. Modules à cocher : **Windows Build Support (IL2CPP)** + **Visual Studio Community** (si pas déjà d'IDE C#).
4. Licence : Personal (gratuite, CA < 100 k$).

### 2. Création du projet (fait ensemble avec Claude)
- Hub → New Project → template **Universal 3D (URP)** → nom `game` → emplacement `d:\PROJET\` (donne `d:\PROJET\game`).

### 3. Packages du projet
- **FishNet** (gratuit) : Package Manager → Add package from git URL → `https://github.com/FirstGearGames/FishNet.git` (ou via Asset Store "FishNet: Networking Evolved").
- **FishySteamworks** (transport Steam) : https://github.com/FirstGearGames/FishySteamworks (dépend de Steamworks.NET).
- **Dissonance Voice Chat** (Asset Store, payant ~85 $ — le seul achat obligatoire du MVP) + son intégration FishNet.
- Steamworks.NET : arrive avec FishySteamworks. AppID de test Steam : 480 (SpaceWar) tant qu'on n'a pas notre AppID.

### 4. Optionnel mais recommandé : Unity MCP (pour que Claude pilote l'éditeur)
- https://github.com/justinpbarnett/unity-mcp — permet à Claude Code de créer scènes/GameObjects/prefabs directement dans l'éditeur au lieu de te dicter des manipulations.
- Install : package Unity + `claude mcp add` (voir le README du repo). À faire APRÈS l'étape 2.

### 5. Plus tard (Phase 3, pas maintenant)
- Blender (retouche assets), ElevenLabs (voix de La Direction), compte Steamworks (100 $, à l'approche de la page Steam).

## Vérification finale
Ouvrir le projet `game`, presser Play : la scène vide tourne sans erreur console → prêt pour la Phase 0.
