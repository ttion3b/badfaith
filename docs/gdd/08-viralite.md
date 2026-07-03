# Machinerie virale — features budgétées, pas du marketing

## 1. La GoPro par joueur (le système de receipts)
- Chaque joueur porte une caméra d'épaule diégétique, toujours active.
- Techniquement : pas d'enregistrement vidéo réel en continu — l'hôte enregistre un buffer d'ÉVÉNEMENTS de replay (positions, animations, états) et la "vidéo" est re-rendue à la volée au Tribunal depuis le point de vue de la caméra (comme un kill-cam). Coût mémoire faible, qualité de rendu native.
- Moments toujours capturés : consultation/acceptation de Pacte sur la montre, tir du Juge, bouton rouge, morts, dépôts au Terminal.

## 2. Auto-clips verticaux 9:16 (la feature la plus rentable du projet)
- Après le Tribunal, le jeu propose les 3 meilleurs moments de la manche, re-rendus en 9:16 avec sous-titres automatiques (transcription du voice local du joueur — opt-in) et habillage "MAUVAISE FOI".
- Export en un clic : fichier MP4 local + partage direct. Le joueur moyen devient un canal de distribution TikTok.
- Sélection des moments par score de drama : Pacte démasqué > tir du Juge > bouton rouge > braquage vocal long > mort en direct.
- MVP : export local MP4 sans transcription. Post-MVP : sous-titres auto, templates d'habillage.

## 3. Extension Twitch "La Direction, c'est le chat"
- Le chat du streamer vote quel Pacte proposer au streamer (parmi 3 options tirées par le jeu).
- L'extension montre aux viewers les Pactes secrets des AUTRES joueurs (supériorité du spectateur) avec un délai anti-snipe de 2-3 min et uniquement pour les lobbies qui l'activent.
- Overlay "suspicion-mètre" : les viewers votent en continu sur qui est le plus suspect ; le streamer ne le voit pas, le VOD oui.
- Post-MVP (phase early access), mais l'API interne (événements de partie exposés) est prévue dès l'architecture MVP.

## 4. Le spectateur mort (rétention en partie)
- Un joueur mort bascule en "régie" : il voit les caméras GoPro de tous les joueurs vivants et LES PACTES en cours. Il sait tout, ne peut rien dire (son micro passe en canal des morts).
- Frustration délicieuse : "JE SAIS QUI C'EST ET JE PEUX RIEN DIRE" — c'est aussi exactement l'expérience du viewer Twitch, donc les morts génèrent du contenu de réaction.

## 5. Nommage et identité
- Titre FR : MAUVAISE FOI. Titre international : BAD FAITH.
- La Direction : voix TTS corporate froide et polie (façon annonces d'aéroport) — contraste comique avec le chaos. Toutes ses répliques sont écrites pour être citables ("La Direction décline toute responsabilité.").
- Direction artistique : low-poly PSX, palette industrielle + signalétique jaune/noir, visages simples mais expressifs (les emotes faciales déclenchables sont un outil de mauvaise foi).
