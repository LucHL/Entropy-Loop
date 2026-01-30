---
title:          Beta Test Plan
subtitle:       Entropy Loop
author:         Claire LUTHIN, Kevin LANDBECK, Luc HELMLINGER, Lucas SCHNEIDECKER, Nils LAMBERT, Tom WALTER
module:         G-EIP-700
version:        1.0
---

<!--# **BETA TEST PLAN – Entropy Loop**-->

## **1. Project context**
Entropy Loop is a tactical game combining autochess and deck-building, developed in Unity using C#. Players build and evolve multiple card decks that allow them to summon and position units on a board, where battles unfold automatically. Strategy is driven by unit placement, card synergies, and progression across successive runs.

The game takes place in a dark, symbolic universe inspired by themes of depression and resilience. The Void is portrayed as an omnipresent force influencing environments, enemies, and gameplay systems. Through procedurally generated levels, branching paths, and increasing difficulty, each run offers a unique and replayable experience.

The project features a low-poly art direction, designed to ensure strong visual identity while maintaining high performance. Its technical architecture is built to be scalable, with a focus on optimization, accessibility, and long-term replayability. Initially developed for PC, Entropy Loop is also designed with a future mobile adaptation in mind.

This BTP aims to demonstrate our ability to design a complete game project that is artistically coherent, technically robust, and gameplay-driven, while working as a team using professional tools and structured production methods.

## **2. User Roles**
The following roles will be involved in beta testing.

| **Role Name**  | **Description** |
|--------|----------------------|
| Player        | Player |

---

## **3. Feature table**
All of the listed features will be demonstrated during the beta presentation

| **Feature ID** | **User role** | **Feature name** | **Short description** |
|----------------|---------------|------------------|--------------------------------------|
| F1             | Player        | Game Menu        | The first screen of the game to present the game |
| F2             | Player        | Settings         | A screen for adjusting sounds, changing keys... |
| F3             | Player        | Deck selection   | Choose your first deck of cards |
| F4             | Player        | Level up         | Screen where you can level up your card with your virtual money |
| F5             | Player        | Level selection  | Choose your level (example : candy crush) |
| F6             | Player        | Map of the game  | Generation of chessboard to put the player |
| F7             | Player        | Entity selection | Choose your card to put on the chessboard |
| F8             | Player        | Combat           | Game start and play automatically | 
| F9             | Player        | Shop             | You can buy some more cards to add to your deck |

---

## **4. Success criteria**

| **Feature ID** | **Key success criteria** | **Indicator/metric** | **Result** |
|--------------|---------------------------------------|-----------------------|----------------|
| F1 | The player has access to the settings window, the game, and can close the application | You can move from one window to another without getting stuck | Achieved |
| F2 | Graphics, audio, and controls can be changed | All of the parameters mentioned can be modified | Partially achieved |
| F3 | The player can change their deck of cards | The player must be able to play the cards and entities from the selected deck | Partially achieved |
| F4 | Players can improve the stats of the cards in their deck | Entity statistics must change according to the improvements made | Partially achieved |
| F5 | The player must be able to play the game indefinitely, levels are created automatically | Each level increases the difficulty of the game | Partially achieved |
| F6 | With each new game, the map must be generated automatically | the map must be generated automatically | Partially achieved |
| F7 | The player must be able to select a card from their deck and place the entity on the board | The selected entity must be placed in the desired location | Partially achieved |
| F8 | The entities must fight each other until one team wins | One team must be the winner | Partially achieved |
| F9 | Players must be able to purchase cards during battles to expand their deck and increase their chances of dropping good cards | The number of cards in the deck must increase | Partially achieved |
