# Procedural-Object-Placer
Procedural Object Placer and Level Generator is a Unity tool created with the intention of rapidly placing mesh models on various terrains to create random levels. The tool is split into two primary parts: an object placer and a random line generator.

The tool was built to create levels for a game where the user follows a path through a predefined and populated level. Example below.

![Sample](https://user-images.githubusercontent.com/43308388/62971441-e347c900-bddf-11e9-8ca0-e338bde87371.png) 

*This tool can also quickly place a large number of objects following patterns defined by unity colliders.



## The Inspector

![Inspector](https://user-images.githubusercontent.com/43308388/62983810-c79feb00-bdfe-11e9-8e19-103d8c07031d.png)

The tool has a built-in custom inspector that allows for easier usage. Many settings defining spawn parameters, line generation, and collider settings are defined in inspector variables


## Object Placer
![ezgif com-optimize](https://user-images.githubusercontent.com/43308388/62971601-3caff800-bde0-11e9-8339-fccfa6cabf9b.gif)

The object placer relies upon the unity built-in collision system as a means of placing the objects. The system follows the below steps for placement. 
1. A random triangle on "terrain"/base mesh is selected 
2. The spawned object is rotated to be flush and stand up on the terrain(to account for objects like trees needing to be standing up).
3. Then relying on the Unity collider system, if any of the objects overlap they go through step 1 and 2 until no longer colliding.

Objects are spawned in batches to allow all the spawn calls to occur together and for all the colliding to be run together. (This is opposed to the method used in reference 1, and is comparatively orders of magnitude faster.)

Objects can be spawned on a set of predefined meshes such as those shown in the first image or on a tileable/scalable terrain.

![Tileablility](https://user-images.githubusercontent.com/43308388/62984241-6d078e80-be00-11e9-8f76-c4698ba3611e.gif)



## Line Generator
![Line Generator](https://user-images.githubusercontent.com/43308388/62970058-99111880-bddc-11e9-97b6-15635b77ec4f.gif)


The line generator portion of the tool creates a random 2D or 3D line within a defined space. The system follows the below steps for placement. 
1. Random start and end positions are selected based on min and max distance for the random line are placed within predetermined bounds. (bounds are an indicator to use by yellow bounding box)
2. Random line segments are continually generated according to the defined user parameters until a complete line is formed.
3. Colliders are created along the line to make the generator work with the object placers collider driven approach.







## Reference / Tools used

1. http://pawelstupak.com/colliders-based-placement-tutorial/   
This served as the initial inspiration for the project. Was the basis for using a collider based approach to making a tool like this.
2. https://assetstore.unity.com/packages/3d/environments/low-poly-pack-94605 
Art used to demo the project.
3. https://www.screentogif.com/
Tool used to create the gifs within the readme.

