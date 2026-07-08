# Changelog

All notable changes to this project will be documented in this file.

## [0.1.5-alpha] - 2026-
### Added
- Player can now choose his deck
- save player data and remove then in settings window
- number of card remaining in the deck
- add color for the next level
- if the player hasn't selected a deck when starting a level, a window will force the player to choose one.
- update README

### Fixed
- Each click on the deck remove a card even if the handslot is full
- Game freeze when you start a fight without any player units
- attackRange now depend on the chess tile size
- tutorial is now the level 1


## [0.1.4-alpha] - 2026-06-07
### Added
- story system with json file
- log system with UI button
- HP bar with different color for the player and enemy
- new level & level is now handle with a json file
- spawn algo for enemy, with differents trategy
- use of json for the level generation
- The level finish are in green

### Fixed
- settings menu: sound now can be modified
- you can not Drag and Drop units when the game is running
- deathzone error message
- gameManager is now a DontDestroyOnLoad & handle the whole game, menu & story
- gameLoop now handle only the game
- position of entity when you Drag and Drop
- change 3D model Cavalier to humainModel
- entity dispawn when you restart a game


## [0.1.3-alpha] - 2026-05-27
### Added
- Drag and Drop units
- Chess board hover color
- New decks with new 3D models
- Win and Loose condition

### Fixed
- Units navigation (without NavMesh)
- Entity body block
- Entity push each other into the void


## [0.1.2-alpha] - 2026-03-16
### Added
- Basic combat system
- HP bars
- Basic entity model
- Menu with settings
- Free camera
- Level selector with one level playable
- right click to remove entity from the board
