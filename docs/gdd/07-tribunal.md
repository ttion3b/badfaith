# Le Tribunal — la machine à clips

## Concept
Séquence post-game scriptée de ~3 minutes, présentée comme une émission par La Direction. Les joueurs (survivants ET morts) sont réunis dans une salle virtuelle face à un grand écran. Le jeu rejoue la manche incident par incident, et pour chacun : accusation → défense au micro → receipt vidéo → verdict. C'est le format de clip du jeu, l'équivalent du meeting d'Among Us mais avec des preuves vidéo.

## Le déroulé, incident par incident
1. La Direction annonce l'incident : "À 07:42, les lumières de l'aile Est ont été coupées. Était-ce un accident… ou un Pacte ?"
2. **Phase d'accusation (20 s)** : chaque joueur vote sur sa montre pour un suspect (ou "accident naturel"). Débat libre au micro pendant le vote.
3. **Phase de défense (15 s)** : le plus accusé a la parole (son portrait passe en plein écran — moment face-cam pour les streamers).
4. **LE RECEIPT** : l'écran diffuse l'extrait GoPro. Deux cas :
   - C'était un Pacte : on voit, du point de vue de la GoPro du coupable, sa montre vibrer, l'offre s'afficher, et SON POUCE APPUYER SUR ACCEPTER. Puis un plan de la conséquence. Musique de verdict.
   - C'était un accident naturel : "Accident. La Direction décline toute responsabilité." → tous les accusateurs à tort paient une amende.
5. Scoring immédiat affiché, puis incident suivant.

## Le scoring de la mauvaise foi (le mensonge est scoré)
- **Prime de mauvaise foi** : coupable NON désigné majoritairement → +50 % du gain du Pacte en bonus. Mentir avec succès rapporte.
- **Démasqué** : coupable désigné majoritairement → il rembourse le gain du Pacte au Pot Commun des perdants (réparti entre ceux qui ont voté juste).
- **Fausse accusation** : voter coupable sur un accident naturel → amende de 500 $ par erreur. Accuser a un coût, le "j'accuse tout le monde à chaque fois" n'est pas gratuit.
- **Flair** : voter juste (coupable OU accident) → +300 $ par verdict correct.
- Les morts votent aussi et gagnent leurs primes de Flair : perdre reste un jeu.

## Sélection des incidents
Le Tribunal ne rejoue pas tout : il choisit les 4-6 incidents les plus dramatiques (pondération : Pacte accepté > mort de joueur > accident naturel ayant causé des dégâts). Toujours inclure : le tir du Juge s'il a eu lieu, et le bouton rouge de la capsule s'il a été pressé.

## Pourquoi les GoPros (design des receipts)
- Chaque joueur porte une GoPro d'épaule, TOUJOURS active, buffer circulaire côté hôte (voir 09-tech.md).
- Le receipt est diégétique : ce n'est pas une "UI de replay", c'est LA caméra du joueur. Cohérence fictionnelle totale (La Direction surveille tout).
- Le plan type du receipt : point de vue épaule → poignet qui se lève → montre affichant "BLACKOUT AILE EST — ACCEPTER ?" → le pouce appuie → coupe sur les lumières qui meurent. Ce plan de 8 secondes EST le format TikTok du jeu.

## Après le Tribunal
- Classement de manche + classement de session.
- Export auto-clip : le jeu propose immédiatement les 3 meilleurs moments en 9:16 sous-titrés (voir 08-viralite.md).
