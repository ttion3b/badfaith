# Registre des risques

## R1 — L'équilibrage du déni plausible (risque produit n°1)
Trop identifiable → whodunit résolu en 30 s. Trop anonyme → accuser est gratuit, apathie.
Mitigation : prototype gris dédié (Jalon J1), les 4 variables de tuning isolées et exposées dans un menu de playtest, amende de fausse accusation pour donner un coût au spam d'accusations.

## R2 — La toxicité entre inconnus
Le jeu est conçu pour la trahison ENTRE AMIS. Avec des inconnus, la trahison + voice = toxicité.
Mitigation MVP : pas de matchmaking public, lobbies par code uniquement. Le public cible (groupes d'amis) est aussi le canal viral.

## R3 — Le meta-gaming hors jeu (Discord parallèle, streams snipés)
Les receipts du Tribunal perdent leur effet si tout le monde spoile sur un Discord à côté.
Mitigation : les Pactes n'affichent leur contenu qu'à l'écran de la montre (pas de log consultable), délai anti-snipe sur l'extension Twitch, et surtout : l'expérience est plus drôle en jouant le jeu — on ne combat pas le hors-jeu, on rend le dans-jeu plus savoureux.

## R4 — La physique en réseau (risque technique n°1)
Objets portés + ragdolls + client-hôte = désync.
Mitigation : autorité hôte stricte, peu d'objets simultanés (~40), interpolation généreuse (la DA PSX pardonne la latence visuelle), portage = attachement cinématique (pas de joints physiques réseau).

## R5 — Le host quitte / host advantage
L'hôte voit tout côté serveur (Pactes des autres, origine des événements).
Mitigation MVP : accepté entre amis (comme Lethal Company). Migration d'hôte en post-launch si besoin. Les données sensibles (auteur des Pactes) chiffrées en mémoire au minimum symbolique — on documente honnêtement que l'hôte "de confiance" est un prérequis.

## R6 — Le Tribunal qui traîne en longueur
3 min de post-game peuvent lasser si mal rythmées.
Mitigation : 4-6 incidents max, timers stricts (20 s + 15 s), le receipt dure 8-10 s, possibilité de skip collectif à l'unanimité. Le Tribunal doit finir sur une faim, pas sur un soulagement.

## R7 — Dépendance à la culture FR (Terminal de Michou)
La référence est française, le marché est mondial.
Mitigation : le concept (dilemmes secrets/mauvaise foi) est universel — Among Us l'a prouvé. Le titre international BAD FAITH et la Direction en VO anglaise sont prévus dès la Phase 3.

## R8 — Scope creep (le risque de l'équipe motivée)
Le concept génère 1 000 idées de Pactes et de systèmes.
Mitigation : la règle des 8 Pactes MVP est ferme. Toute idée nouvelle va dans docs/gdd/99-backlog.md, rien n'entre en Phase 1-2 sans qu'un playtest ait montré un manque.
