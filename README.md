# TotemDungeon
This repository contains code and files related to my Master Semester Project : Placing Sound in a Procedurally Generated Game. 
For more detailed information please check the Placing_Sound_In_PCG_Levels.pdf which contains the final report of this project.

Summary:
I designed a simple game in Unity in which a player needs to find and retrieve collectibles in a procedurally generated 3d level. This means that the level is unknown before runtime, as well as all the collectibles locations. The purpose of this project is to enhance the PCG experience by adding sound in automatic fashion to the game level.
I present two approaches: 1. Calculate all important paths, such as player-to-collectible and collectible-to-collectible then calculate intersections. These spots may be very close to one another so we refine these by interpolating 2 spots that are too close to each other. 2. Finding locations of equal distance between 2 collectibles. With this approach we may vary the distances to have sounds more scattered around the level.
The first approach gives more accurate results, the player listens to the sounds more often. If users are very far from collectibles there will be no sounds. The second approach is less accurate but players who wander around the level will still hear sounds that will adapt to how far they are from collectibles.
Please check the report for more details and statistical work.

