# Custom Ion Cubes

A mod for Subnautica which allows both users and mod makers alike to easily create their own ion cubes with custom colouring.


## Installation

- Install [BepInEx](https://www.nexusmods.com/subnautica/mods/1108).
- Install [Nautilus](https://www.nexusmods.com/subnautica/mods/1262).
- Unzip this mod into your `Subnautica/BepInEx` directory.
- Enjoy!


## Usage

Any custom cubes created by this mod unlock as soon as you pickup and/or scan your first vanilla ion cube. After that, 
you can freely craft one cube into the other at the fabricator.

The mod comes pre-installed with purple ion cubes. To create your own variants you have three options:


### For Users: Option 1 - JSON files

Nested in the mod directory is the `Assets/Colors` directory. This mod will attempt to load every .json file in this folder 
and try to parse it into a new cube. Every file is a separate cube. To begin, copy the existing `purple.json` and rename it
to the new colour you are trying to create.

Open the new .json file you just created. Change the ID to something unique, ideally matching the file name. *You cannot have
multiple cubes with the same id.* This will cause an error. This ID later determines the item id of your custom cube, which is
added to the mod prefix. For example, using the ID `purple` results in an in-game item id of `cic_purple`. Orange would lead
to `cic_orange`, etc.

After adjusting the id you'll find the data for a few different colours in your new cube. These are represented in RGBA colours;
it's easier to play around with those in an online editor like for example https://rgbcolorpicker.com/0-1, finding a colour
you like over there, and then copying it to the file. Each of these colours does a slightly different thing:

- MainColor - Determines the main color of the texture. Changing this value will have the largest impact on the overall look.
- Details - This color mostly impacts the glowy highlights like the "energy lines" running through the cube.
- AnimatedSquares - Determines the look of the many animated squares that flash over the surface of the cube. You will rarely
  see the actual colour you input here because the game takes this color as a baseline for the random colors it *actually* displays.
- Glow - Determines how much the cube glows, i.e. how visible it is at night. Different colors have little impact here,
  the alpha channel makes all the difference.
- Illumination - Determines the color of the light used to illuminate the surroundings of the cube. Rarely visible unless
  the cube is resting on a surface.
- IconColor - The color used to change the inventory icon. This is done by replacing the hue in HSV colour space, so
  the icon will look more or less the same with the colour "swapped out".


### For Mod Makers: Option 2 - Calling the API

The method used internally by this mod to register new cubes is publically accessible in [CustomIonCubeHandler](CustomIonCubes/CustomIonCubeHandler.cs). 
You can call it from your own mod either directly by adding this mod as a dependency or via reflection. Either way, make sure 
that you only call this method after giving CustomIonCubes a chance to start up! You can do so by adding a 
`[BepInDependency("com.github.tinyhoot.CustomIonCubes")]`
attribute to your mod or delay calling the method until your plugin's `Start()` method.

### For Mod Makers: Option 3 - Mod Messaging

CustomIonCubes supports the Nautilus Mod Messaging System. Every time a new cube is registered this mod will send out a
global message using the subject `CustomIonCube_Registered` to notify any other mods of the TechType of a newly created cube.
You can also register new cubes via the messaging system by sending a mod message with the correct subject matching the 
signature of any of the public cube registering method overloads as described above in Option 2.

```cs
ModMessageSystem.SendMessage(
    "com.github.tinyhoot.CustomIonCubes",  // The inbox you are sending the message to, in this case this mod's GUID.
    "CustomIonCube_Registered",            // The subject of your message so that this mod knows what this message is about.
    "white",                               // Start of the registering method signature; id of the new colour.
    Color.white, Color.white, Color.white, Color.white, Color.white, Color.white);
```
