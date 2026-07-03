# MAUVAISE FOI (BAD FAITH) — instructions projet

## Ce qu'est ce projet
Jeu multijoueur 4-8 joueurs de trahison sociale à déni plausible (Unity 6, FishNet, Dissonance), conçu pour la viralité Twitch/TikTok. Équipe indé FR, code accéléré par IA.

## Sources de vérité (dans cet ordre)
1. `docs/gdd/` — le Game Design Document. Toute décision de design y est documentée avec son pourquoi. Ne jamais implémenter un système sans relire son fichier GDD.
2. `graphify-out/graph.json` — knowledge graph du projet. Pour toute question d'architecture ou de design, utiliser `/graphify query "..."` d'abord.
3. `docs/gdd/99-backlog.md` — les idées post-MVP. RIEN du backlog n'entre au MVP (risque R8, scope creep).

## Règles de design non négociables
- **Déni plausible** : tout événement déclenchable par un Pacte doit pouvoir arriver naturellement, avec le MÊME code/sons/timings (voir docs/gdd/04-deni-plausible.md).
- **8 Pactes au MVP**, pas un de plus.
- La viralité (GoPro/replay/auto-clips) est une feature, pas du polish de fin.

## Architecture code
- `game-src/Core/` — logique de domaine en C# PUR (aucun using UnityEngine) : testable hors Unity, copiée/liée dans le projet Unity sous `Assets/Scripts/Core/`. Toute règle de gameplay (économie, pactes, scoring, accidents) vit ici.
- `game/` — le projet Unity 6 URP (créé en Phase 0). Les MonoBehaviours/NetworkBehaviours sont des adaptateurs fins autour de Core.
- Réseau : autorité hôte stricte (FishNet). Les clients n'exécutent jamais de logique de domaine.
- L'aléatoire de gameplay passe TOUJOURS par une interface injectable (IGameRandom) — indispensable pour tester le Directeur d'Accidents.

## Conventions
- C# : conventions .NET standard, code et identifiants en anglais, commentaires en français OK.
- Commits : conventionnels (feat:, fix:, docs:), en français.
- Le GDD est en français (équipe FR-first).
