# Le moteur de déni plausible — cœur du design

## Règle numéro un du projet
Tout événement déclenchable par un Pacte DOIT pouvoir se produire naturellement. Si un sabotage est identifiable à 100 %, la mauvaise foi meurt et le jeu devient un whodunit résolu en 30 secondes. Si rien n'est jamais attribuable, accuser devient gratuit et le Tribunal perd son enjeu. Tout l'équilibrage du jeu vit entre ces deux bornes.

## Implémentation : le Directeur d'Accidents (système serveur)
Un système côté hôte maintient un taux de base d'événements naturels :
- Lumières : flicker bref fréquent (ambiance), coupure de 30-60 s d'une aile ~1 fois par manche naturellement.
- Portes : ~5 % de chance de se gripper 20-60 s à chaque cycle d'ouverture.
- Gaz / électricité / effondrement : 1-2 événements naturels par manche, zones aléatoires pondérées.
- Micros : micro-coupures radio aléatoires brèves (grésillement d'ambiance permanent léger).

## Règles d'orchestration
1. **Indistinguabilité** : un événement naturel et un événement de Pacte utilisent EXACTEMENT le même code, les mêmes sons, les mêmes timings. Aucune différence détectable par datamining visuel/sonore.
2. **Lissage anti-cluster** : si un Pacte vient de déclencher un blackout, le Directeur d'Accidents supprime les blackouts naturels pendant 90 s (éviter les coïncidences absurdes qui cassent la crédibilité).
3. **Génération d'ambiguïté active** : quand un Pacte est accepté, le Directeur augmente légèrement la probabilité d'UN événement naturel d'un autre type dans la même fenêtre → il y a souvent deux explications possibles à la situation.
4. **Le brouillard statistique est public** : l'écran de chargement dit aux joueurs "des accidents arrivent naturellement" — les joueurs doivent SAVOIR que le déni est possible, sinon toute accusation devient une certitude.

## Variables d'équilibrage prioritaires (playtest avant tout contenu)
| Variable | Trop bas | Trop haut |
|---|---|---|
| Taux d'accidents naturels | Chaque incident = accusation certaine | Personne n'accuse jamais, apathie |
| Délai des Pactes (20-90 s) | L'auteur est celui qui vient de partir | Personne ne relie cause et effet |
| Vibration de montre audible (2 m) | Aucun contre-jeu possible | Les Pactes sont inutilisables près des autres |
| Fréquence des Pactes (2-3 min) | Pas assez de chaos | Le chaos noie la paranoïa |

## Test de validation (prototype gris)
Une pièce grise, des cubes à looter, 6 Pactes, le voice, le Tribunal. Critère de succès : une table de 6 potes rit et s'accuse pendant le Tribunal SANS contenu artistique. Si ça marche avec des cubes, le jeu est validé. Si ça ne marche pas, aucun asset ne le sauvera.
