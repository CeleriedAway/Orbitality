# Orbitality

This task is done in a simple procedural style with a bit of reactive code for cleaner game model and views.
Physics is a simple n^2 iteration, its good enough so for simplicity I decided not to go with Unity phys or other approaches
If I need to go better with performance and gameplay extensibility I would consider:
* Component array separation for better cache locality
* entity component system
* 2d grid proximity accelerator
* unity 2d physics

Rockets can be configured in Game/Resources/config scriptable object

Orbits are more elliptic to better fit the screen.

There are Omnilibs folder, that is a collection of constantly evolving reliable tools I use for 5 years or so
In this project I used ZergRush - reactive and UI library, you can see more examples of it here https://github.com/CeleriedAway/ZergRushUnityDemo
I tried to comment well all its usages.

Also, I used a bit of CodeGen to generate JSON serialization functions for game model
Codegen is a very powerful tool - the core of my last project, it can generate: various serialization, hashing, replication, comparison, automate object pooling, hierarchy propagation and a lot more.

I have completed all task requirements in 3.5 hours
I spend some more to write more comments, make better effects, experiment with rockets, make some additional features.

Unity version used is 2019.2.0f1