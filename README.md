# Procedural-Object-Placer
Procedural Object Placer and Level Generator is a Unity tool created with the intention of rapidly placing mesh models on various terrains to create random levels. The tool is split into two primary parts: an object placer and a random line generator.

The tool was built to create levels for a game where the user follows a path through a predefinend and populated level. Example below.

![Sample](https://user-images.githubusercontent.com/43308388/62971441-e347c900-bddf-11e9-8ca0-e338bde87371.png) 

*This tool can also quickly place a large amount of objects following patterns defined by unity colliders.



## The Inspector




## Object Placer
![ezgif com-optimize](https://user-images.githubusercontent.com/43308388/62971601-3caff800-bde0-11e9-8339-fccfa6cabf9b.gif)

The object placer relies upon the unity built in collision system as a means of placing the objects. The system follows the below steps for placement. 
1. A random triangle on "terrain"/base mesh is selected 
2. The spawned object is rotated to be flush and stand up on the terrain(to account for objects like trees needing to be standing up).
3. Then relying on the Unity collider system, if any of the objects overlap they go through step 1 and 2 until no longer colliding.

Objects are spawned in batches to allow all the spawn calls to occur together and for all the colliding to be run together. (This is opposed to the method used in reference 1, and is comparitively orders of magnitude faster.)


## Line Generator
![Line Generator](https://user-images.githubusercontent.com/43308388/62970058-99111880-bddc-11e9-97b6-15635b77ec4f.gif)


The line generator portion of the tool creates a random 2D or 3D line within a defined space. 

OBJECT PLACER
Optimized
Tool have a great UI
References
Random line
object placer
















