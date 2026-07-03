# MAUVAISE FOI — le jeu en 5 minutes

> **Pitch** : 4 à 8 amis coopèrent pour survivre à une expédition… pendant que le jeu propose secrètement à chacun de trahir les autres contre de l'argent. Personne ne sait qui a fait quoi — jusqu'au **Tribunal** de fin de partie, où les preuves vidéo tombent une par une, devant les face-cams.

**En une phrase : Lethal Company × Among Us × Le Terminal de Michou, où mentir est une mécanique de jeu scorée.**

---

## Comment se passe une partie (15 minutes)

### 1. L'Expédition (12 min)
On descend à 4-8 dans un complexe industriel, en chat vocal de proximité. Objectif d'équipe : looter assez d'argent pour remplir le **Pot Commun** — si le quota n'est pas atteint, la navette ne vient pas et **tout le monde meurt**. On est donc obligés de coopérer.

Mais chaque joueur a aussi une **Poche Perso**… et le vainqueur de la manche est le survivant le plus riche. À chaque dépôt, il faut choisir : l'équipe ou soi.

### 2. Les Pactes (tout du long)
Régulièrement, ta **montre** vibre discrètement. C'est "La Direction" (la voix du jeu) qui te fait une offre privée :

> *« +2 000 $ pour toi. En échange, les lumières de l'aile Est couperont dans 45 secondes. »*

Tu acceptes d'un appui du pouce. Personne ne sait que c'est toi. La conséquence tombe plus tard, quand tu es loin, avec un alibi.

**Le twist génial** : des accidents identiques arrivent aussi **naturellement**. Les lumières coupent parfois toutes seules. Les portes se grippent parfois toutes seules. Donc quand quelqu'un t'accuse, tu peux toujours répondre *« c'est pas moi, c'est un accident »* — et c'est peut-être vrai. C'est ça, le moteur de **mauvaise foi**.

Et attention : consulter sa montre est un geste **visible par les autres**. Regarder son poignet juste avant une panne, ça fait jaser…

### 3. Les autres outils de la trahison
- **Le Juge** : l'unique arme de la map, avec une seule balle. Elle sert surtout à braquer au micro (*« pose ton loot ou je tire »*). Vérifier si elle est chargée est une action privée — le bluff ne meurt jamais.
- **La règle cruelle** : quand un joueur meurt, sa Poche Perso est versée au Pot Commun. Laisser mourir un ami **aide objectivement tout le monde**. La non-assistance devient rationnelle… et défendable.
- **La capsule d'extraction** : moins de sièges que de joueurs, limite de poids, et un bouton rouge pour partir sans les autres. La fin de manche, c'est des chaises musicales au micro.

### 4. LE TRIBUNAL (3 min) — le moment que tout le monde attend
Après l'extraction, tout le monde (morts inclus) se retrouve face à un grand écran. Le jeu rejoue les incidents un par un :

1. *« À 07:42, les lumières de l'aile Est ont coupé. Accident… ou Pacte ? »*
2. Tout le monde **vote** un suspect (ou "accident") — débat libre au micro.
3. Le principal accusé se défend.
4. **Le receipt tombe** : chaque joueur porte une GoPro. L'écran diffuse l'extrait — on voit le poignet du coupable se lever, l'offre s'afficher sur sa montre, et **son pouce appuyer sur ACCEPTER**. Ou bien : *« Accident. La Direction décline toute responsabilité. »* — et tous ceux qui ont accusé à tort paient une amende.

Et voilà le pilier du jeu : **le mensonge est scoré**.
- Mentir sans être démasqué → **+50 % de bonus** (la "prime de mauvaise foi").
- Être démasqué → tu rembourses tout à ceux qui t'ont grillé.
- Accuser juste → bonus de flair. Accuser à tort → amende.

Perdre reste drôle : les morts votent, gagnent leurs bonus de flair, et assistent au spectacle.

---

## Pourquoi ça peut devenir viral (la thèse)

1. **Le clip est auto-suffisant** : le Tribunal, c'est 30-60 secondes compréhensibles sans contexte — accusation, mensonge éhonté au micro, preuve vidéo, réaction face-cam. C'est le moment d'éjection d'Among Us, en mieux, avec des preuves.
2. **Le jeu fabrique ses propres clips** : après chaque manche, il exporte les 3 meilleurs moments **en format vertical 9:16, prêts pour TikTok**, en un clic. Chaque joueur devient un canal de distribution.
3. **Le spectateur en sait plus que les joueurs** : extension Twitch prévue où le chat voit les Pactes secrets et vote même quel Pacte proposer au streamer. Le stream devient une émission.

Références assumées : Lethal Company (loot, quota, esthétique low-poly), Among Us (paranoïa, le meeting), Content Warning (la caméra dans le jeu), Le Terminal de Michou (les dilemmes secrets).

---

## Ce qu'on ne fait PAS (aussi important)
- ❌ Pas de bestiaire de monstres coûteux — la menace, c'est les autres joueurs. Des hazards environnementaux suffisent.
- ❌ Pas de grande map battle royale — la paranoïa a besoin de proximité.
- ❌ Pas de matchmaking public au lancement — le jeu est fait pour la trahison **entre amis** (lobbies par code).
- ❌ Pas de graphismes chers — low-poly PSX assumé, tout le budget va dans les interactions.

## La stack et le plan
- **Unity 6** (URP, rendu PSX), **FishNet** + relais Steam (réseau, 0 $ de serveurs), **Dissonance** (voice de proximité avec occlusion par les murs).
- **MVP en 6 mois**, avec UN rendez-vous décisif à la fin du mois 2 : le **prototype gris** — une manche complète avec des cubes, sans aucun asset. Si le Tribunal fait rire une table de 6 potes avec des cubes, le jeu est validé et on produit le contenu. Sinon, on itère sur l'équilibrage avant de dépenser un euro d'art.
- Prix cible : 9,99 $ en Early Access sur Steam (le prix "toute la bande achète").

## Où on en est
- ✅ Game Design Document complet : [docs/gdd/](gdd/) (commencer par [00-vision.md](gdd/00-vision.md))
- ✅ Cœur du gameplay codé en C# (économie, Pactes, accidents, scoring du Tribunal) : `game-src/Core/`
- ✅ Roadmap détaillée : [docs/gdd/10-roadmap.md](gdd/10-roadmap.md)
- 🔜 Phase 0 : projet Unity + réseau + voice → « 6 joueurs dans une boîte grise qui se lancent des cubes en vocal »

**La question à laquelle on répond dans 2 mois : est-ce que le Tribunal fait rire une table de 6 potes ? Tout le reste en découle.**
