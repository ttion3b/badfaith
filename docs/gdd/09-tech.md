# Stack technique et architecture

## Moteur : Unity 6 (URP)
- Écosystème du genre (Lethal Company, Content Warning sont Unity), pipeline léger pour du low-poly, énorme vivier d'assets et de plugins réseau/voice.
- URP + rendu PSX (shader de vertex snapping + dithering, assets disponibles sur l'Asset Store).
- C# — accélérable massivement par IA (génération de code fiable sur Unity).

## Réseau : client-hôte via Steam, PAS du vrai P2P mesh
- **FishNet** (open source, gratuit, performant) comme couche réseau + **FishySteamworks** (transport Steam Datagram Relay : NAT traversal gratuit, pas de serveurs à payer, pseudo-P2P via relais Valve).
- Alternative payante si besoin : Photon Fusion 2. Décision MVP : FishNet (0 $ et pas de vendor lock-in).
- L'hôte (un des joueurs) a l'autorité : physique des objets, Directeur d'Accidents, attribution des Pactes, buffer de replay. Le cheat est un non-problème au lancement (lobbies entre amis).
- Objets physiques : autorité hôte avec prédiction client légère sur l'objet porté par soi-même. Max ~40 objets synchronisés par map au MVP.

## Voice : Dissonance Voice Chat (asset Unity)
- Proximité 3D avec atténuation + OCCLUSION par les murs (comploter dans une pièce fermée est un gameplay).
- Canal des morts séparé. Effet radio/grésillement pour le Pacte Brouillage.
- Intégration au buffer de replay : Dissonance permet de taper le flux audio par joueur (pour les sous-titres des clips, opt-in, post-MVP).

## Systèmes serveur (côté hôte)
- **PacteService** : tirage, distribution, délais, règles d'unicité et de pitié (03-pactes.md).
- **AccidentDirector** : taux de base des événements naturels, lissage anti-cluster, génération d'ambiguïté (04-deni-plausible.md). Naturel et Pacte appellent le MÊME code d'événement — un seul enum HazardEvent, le flag d'origine n'existe que dans le log du Tribunal.
- **ReplayRecorder** : buffer circulaire d'événements (positions à 10 Hz, événements discrets), sérialisé pour le Tribunal et l'export de clips.
- **TribunalDirector** : sélection des incidents par score de drama, séquencement du reveal, scoring.
- **EconomyService** : pot commun, poches perso, règle de la mort, quota, capsule.

## Plateforme et distribution
- PC / Steam uniquement au MVP (Steamworks : lobbies, invites, voice fallback, rich presence).
- Prix cible : 9,99 $ (le prix "toute la bande achète", cf. Content Warning/Lethal Company).

## Outils IA dans le pipeline
- Code : Claude Code (ce projet) — systèmes, netcode, éditeur d'outils.
- Assets 3D : génération low-poly (Meshy/Tripo) + retouche Blender ; la DA PSX pardonne tout.
- Voix de La Direction : TTS (ElevenLabs) — c'est diégétique, une voix corporate synthétique est un choix artistique cohérent.

## Ce qu'on ne fait PAS au MVP
Pas de dédié serveur, pas d'anti-cheat, pas de progression/cosmétiques, pas de matchmaking public (code de lobby entre amis uniquement), pas de console, un seul monstre maximum (et zéro de préférence).
