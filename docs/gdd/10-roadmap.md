# Roadmap de développement — MVP en 6 mois

## Principe : valider la boucle psychologique AVANT de produire du contenu
Le jeu vit ou meurt sur une question : est-ce que le Tribunal fait rire une table de 6 potes ? On le sait au mois 2, avec des cubes gris. Tout le budget artistique attend cette réponse.

## Phase 0 — Fondations (semaines 1-3)
- Projet Unity 6 URP + FishNet + FishySteamworks + Dissonance. Lobby par code, 6 joueurs connectés qui se voient bouger et se parlent en proximité.
- Contrôleur FPS, interaction physique de base : ramasser/porter/poser/jeter des objets (1 main / 2 mains).
- **Jalon J0 : 6 joueurs dans une boîte grise qui se lancent des cubes en vocal.**

## Phase 1 — Prototype gris de la boucle (semaines 4-9)
- Map grise : 6 salles + Terminal central + zone capsule.
- EconomyService (pot/poche, quota, règle de la mort), dépôt public au Terminal.
- Montre : réception de Pacte, vibration audible, consultation publique, acceptation.
- PacteService avec les 8 Pactes MVP. AccidentDirector avec les taux de base.
- ReplayRecorder (buffer d'événements) + Tribunal v1 : reveal séquencé, votes sur montre, scoring complet.
- Capsule : sièges N-2, poids, bouton rouge, porte physique.
- Le Juge : spawn, tir, barillet privé, annonce sonore.
- **Jalon J1 (fin mois 2) : LE playtest de validation. 6 joueurs, manche complète de 15 min + Tribunal, avec des cubes. Critère : rires + accusations spontanées au Tribunal. GO/NO-GO du projet.**

## Phase 2 — Équilibrage et rejouabilité (semaines 10-16)
- 15+ playtests hebdomadaires. Tuning des 4 variables prioritaires (04-deni-plausible.md).
- Variantes de map (agencement modulaire des salles pour la rejouabilité).
- Sessions multi-manches avec classement, assurance-vie, pièce maîtresse à deux porteurs.
- Spectateur mort en mode régie.
- **Jalon J2 : des playtesteurs externes redemandent une deuxième soirée sans qu'on leur demande.**

## Phase 3 — Production du contenu (semaines 17-22)
- DA PSX : modulaire kit industriel, 30-40 props lootables, la montre, la capsule, le Juge.
- Voix de La Direction (TTS + écriture des répliques citables).
- Audio : sons des hazards identiques naturel/Pacte, musique de verdict du Tribunal.
- Auto-clips 9:16 v1 (export MP4 local re-rendu depuis le replay).
- **Jalon J3 : vertical slice présentable — un trailer coupé uniquement dans des clips de vraies parties.**

## Phase 4 — Early Access (semaines 23-26)
- Steamworks complet (lobbies, invites, page Steam), onboarding 90 secondes, options d'accessibilité voice.
- Beta fermée avec 3-5 streamers FR moyens (le jeu est FR-first, le Terminal de Michou est la référence culturelle).
- Lancement Early Access à 9,99 $. La roadmap post-launch est écrite par les clips qui marchent.

## Post-launch (piloté par les données)
Extension Twitch, sous-titres auto des clips, nouveaux Pactes (délation, choix inversé), 2e map, 8-10 joueurs, un monstre simple si les playtests réclament une menace PvE.

## Équipe minimale
- 1 dev gameplay/réseau (accéléré IA) — le poste critique.
- 1 designer/intégrateur (tuning, map, audio) — peut être la même personne au début.
- Art externalisé/généré (DA PSX = tolérante), TTS pour la voix.

## Budget de risque
Le seul vrai risque est en Phase 1 : si J1 échoue, on itère sur les variables de déni 4 semaines max, et si ça ne prend toujours pas, on pivote (le socle technique — voice, physique, réseau — est réutilisable pour tout autre concept du genre).
