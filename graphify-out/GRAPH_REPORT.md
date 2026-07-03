# Graph Report - d:/PROJET  (2026-07-03)

## Corpus Check
- Corpus is ~5,509 words - fits in a single context window. You may not need a graph.

## Summary
- 62 nodes · 80 edges · 9 communities (8 shown, 1 thin omitted)
- Extraction: 94% EXTRACTED · 6% INFERRED · 0% AMBIGUOUS · INFERRED: 5 edges (avg confidence: 0.83)
- Token cost: 86,391 input · 9,800 output

## Community Hubs (Navigation)
- [[_COMMUNITY_Catalogue des Pactes|Catalogue des Pactes]]
- [[_COMMUNITY_Tribunal & Machine a Clips|Tribunal & Machine a Clips]]
- [[_COMMUNITY_Stack Technique Reseau|Stack Technique Reseau]]
- [[_COMMUNITY_Deni Plausible & Validation|Deni Plausible & Validation]]
- [[_COMMUNITY_Economie & Le Juge|Economie & Le Juge]]
- [[_COMMUNITY_Voice & Spectateur Twitch|Voice & Spectateur Twitch]]
- [[_COMMUNITY_Boucle & Extraction|Boucle & Extraction]]
- [[_COMMUNITY_Directeur d'Accidents|Directeur d'Accidents]]
- [[_COMMUNITY_Backlog & Scope|Backlog & Scope]]

## God Nodes (most connected - your core abstractions)
1. `Pacte` - 15 edges
2. `Le Tribunal` - 7 edges
3. `Regle de la mort (poche versee au pot)` - 5 edges
4. `Deni plausible` - 5 edges
5. `MAUVAISE FOI (BAD FAITH)` - 4 edges
6. `These de viralite Twitch/TikTok` - 4 edges
7. `Boucle de gameplay (manche)` - 4 edges
8. `Quota commun` - 4 edges
9. `La Montre` - 4 edges
10. `Le Juge (revolver unique)` - 4 edges

## Surprising Connections (you probably didn't know these)
- `MAUVAISE FOI (BAD FAITH)` --references--> `Dissonance Voice Chat`  [EXTRACTED]
  README.md → docs/gdd/09-tech.md
- `MAUVAISE FOI (BAD FAITH)` --references--> `Roadmap MVP 6 mois`  [EXTRACTED]
  README.md → docs/gdd/10-roadmap.md
- `MAUVAISE FOI (BAD FAITH)` --references--> `FishNet`  [EXTRACTED]
  README.md → docs/gdd/09-tech.md
- `MAUVAISE FOI (BAD FAITH)` --references--> `Unity 6 (URP)`  [EXTRACTED]
  README.md → docs/gdd/09-tech.md
- `Le Terminal de Michou` --rationale_for--> `Pacte`  [EXTRACTED]
  docs/gdd/00-vision.md → docs/gdd/03-pactes.md

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **Pipeline de la machine a clips (GoPro -> replay -> Tribunal -> export)** — docs_gdd_08_viralite_gopro, docs_gdd_09_tech_replayrecorder, docs_gdd_07_tribunal_tribunal, docs_gdd_07_tribunal_receipt, docs_gdd_08_viralite_auto_clips [EXTRACTED 1.00]
- **Systemes serveur cote hote** — docs_gdd_09_tech_pacteservice, docs_gdd_09_tech_accidentdirector, docs_gdd_09_tech_replayrecorder, docs_gdd_09_tech_tribunaldirector, docs_gdd_09_tech_economyservice [EXTRACTED 1.00]
- **Economie double en tension (pot vs poche)** — docs_gdd_02_economie_pot_commun, docs_gdd_02_economie_poche_perso, docs_gdd_02_economie_quota, docs_gdd_02_economie_regle_de_la_mort, docs_gdd_06_extraction_capsule [EXTRACTED 1.00]

## Communities (9 total, 1 thin omitted)

### Community 0 - "Catalogue des Pactes"
Cohesion: 0.17
Nodes (13): La Direction, Terminal central, Depot public, Piece maitresse (objet a deux porteurs), Pacte Blackout, Pacte Depot masque, Pacte Doigt de la Direction, Pacte Fuite de gaz (+5 more)

### Community 1 - "Tribunal & Machine a Clips"
Cohesion: 0.36
Nodes (8): Among Us, Content Warning, Le Receipt (preuve GoPro), Le Tribunal, Auto-clips verticaux 9:16, GoPro diegetique par joueur, ReplayRecorder, TribunalDirector

### Community 2 - "Stack Technique Reseau"
Cohesion: 0.25
Nodes (8): Esthetique low-poly PSX, Architecture client-hote, FishNet, FishySteamworks (Steam Datagram Relay), Unity 6 (URP), R4 — Physique en reseau, R5 — Host advantage / host quitte, MAUVAISE FOI (BAD FAITH)

### Community 3 - "Deni Plausible & Validation"
Cohesion: 0.29
Nodes (8): Les 4 piliers de design, Le Terminal de Michou, Deni plausible, Prototype gris (test de validation), Prime de mauvaise foi (scoring du mensonge), Jalon J1 — playtest GO/NO-GO, Roadmap MVP 6 mois, R1 — Equilibrage du deni plausible

### Community 4 - "Economie & Le Juge"
Cohesion: 0.32
Nodes (8): Assurance-vie (contrat lootable), Poche Perso, Pot Commun, Regle de la mort (poche versee au pot), Pacte Prime sur tete, Braquage au micro, Le Juge (revolver unique), EconomyService

### Community 5 - "Voice & Spectateur Twitch"
Cohesion: 0.40
Nodes (6): These de viralite Twitch/TikTok, Pacte Brouillage, Extension Twitch (La Direction c'est le chat), Spectateur mort (mode regie), Dissonance Voice Chat, R2 — Toxicite entre inconnus

### Community 6 - "Boucle & Extraction"
Cohesion: 0.60
Nodes (5): Lethal Company, Boucle de gameplay (manche), Quota commun, Le bouton rouge (depart anticipe), La Capsule d'extraction

### Community 7 - "Directeur d'Accidents"
Cohesion: 0.67
Nodes (4): Pacte Le Cadeau, Directeur d'Accidents, Indistinguabilite naturel/Pacte, AccidentDirector

## Knowledge Gaps
- **21 isolated node(s):** `La Direction`, `Among Us`, `Content Warning`, `Piece maitresse (objet a deux porteurs)`, `Assurance-vie (contrat lootable)` (+16 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **1 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `Pacte` connect `Catalogue des Pactes` to `Deni Plausible & Validation`, `Economie & Le Juge`, `Voice & Spectateur Twitch`, `Boucle & Extraction`, `Directeur d'Accidents`?**
  _High betweenness centrality (0.559) - this node is a cross-community bridge._
- **Why does `Deni plausible` connect `Deni Plausible & Validation` to `Catalogue des Pactes`, `Directeur d'Accidents`?**
  _High betweenness centrality (0.205) - this node is a cross-community bridge._
- **Why does `Boucle de gameplay (manche)` connect `Boucle & Extraction` to `Catalogue des Pactes`, `Tribunal & Machine a Clips`?**
  _High betweenness centrality (0.193) - this node is a cross-community bridge._
- **What connects `La Direction`, `Among Us`, `Content Warning` to the rest of the system?**
  _21 weakly-connected nodes found - possible documentation gaps or missing edges._