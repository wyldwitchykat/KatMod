# Rain Collector

Turns the dew collector into something more like a rain collector.

* :x: Server side only (clients do not need to install the mod)
* :x: EAC safe (clients and servers do not need to disable Easy Anti-Cheat)

## Features

* The dew collector fills more rapidly when it is foggy.
* The dew collector fills much more rapidly when it is raining.
* The dew collector will not do anything if the outside temperature is below the freezing point of water.
* To make up for the fog and rain boost, the dew collector takes longer to fill up overall.
* Adds the _ability_ to increase the number of "slots" in the dew collector.
  The XML changes are in `windows.xml` but are commented out.
  See below for instructions.
* Contains sample XML changes to use if you want the dew collector to produce no heat for the heatmap.
  The XML changes are in `blocks.xml` but are commented out.
  See below for instructions.

This is all configurable in the XML for the dew collector.

## Technical Details

**This modlet contains custom C# code.**
It is *not* compatible with EAC, and will *not* run if EAC is on.
It must be installed on both clients and servers.

I recommend starting a new game after installing or uninstalling this mod.

I have installed it into an existing game without corrupting the game save, but this is not guaranteed.
_Uninstalling_ the mod is a different matter.
If you have changed the number of slots, you _cannot_ ununstall without starting a new game.
See below for details. 

### Customizing

This mod adds these properties to the dew collector block:

* `MinConvertTemperature`:
  The minimum temperature, in degrees Fahrenheit, at which the dew collector will work.
  The minimum temperature is set to 32, which is the freezing point of water at sea level.
  _This means the dew collector will often stall in the snow biome._
* `FogConvertMultiplier`:
  This a multiplier for the fog density.
  The mod takes the existing collection amount, multiplies it by the fog density,
  then by this multiplier, and adds the result to the existing collection amount.
  The fog density is usually in the range of `0.1 - 0.45` in the vanilla game.
  Clear days are usually around 0.1, foggy days are usually around 0.3,
  and the fog density may go up to 4.5 during heavy rains.
  The multiplier is set to 3, which means foggy days usually double the collection amount.
* `RainConvertMultiplier`:
  This a multiplier for the rainfall amount.
  The mod takes the existing collection amount, multiplies it by the rainfall amount,
  then by this multiplier, and adds the result to the existing collection amount.
  When raining, rainfall can be in the range of `0.1 - 1` in the vanilla game.
  The multiplier is set to 5, which means a light rain can double the collection amount,
  and a heavy rain can increase it five fold.

All of this is configurable by changing the values.
In addition, if you want to totally disable a new feature (like the minimum temperature),
you can remove the relevant `<property>` tag.

Because the collection amounts are significantly increased during fog or rain,
I also doubled the time that it takes for a dew collector to collect a jar of water.

This is done by doubling the vanilla `MinConvertTime` and `MaxConvertTime` values.

#### Changing the number of "slots"

> :warning: If you change the number of slots,
> you _can not_ remove the mod without starting a new game.
> Attempting to do so will cause unrecoverable errors when you open the dew collector,
> and you will need to kill the 7D2D process using Task Manager or similar.

The "slots" are the number of empty squares in the dew collector's output grid.
Each slot generates one bottle of water when filled, unless you have the tarp.
There are three slots in the vanilla game.

In `XUi/windows.xml`, set the columns and rows on the "windowDewCollector" grid:
```xml
<set xpath="//window[@name='windowDewCollector']/rect/grid/@cols">4</set>
<set xpath="//window[@name='windowDewCollector']/rect/grid/@rows">2</set>
```

This XML is already in `windows.xml` but is commented out.
This example uses two rows of four columns each,
but you can use any numbers you want (within reason).

#### Do not produce heat

By default, dew collectors generate some "heat" for the heatmap.
This happens every time water is created in the dew collector.

I have heard players complain about this.
If you want to change this, then remove the "HeatMap" properties from the block:

```xml
<remove xpath="//block[@name='cntDewCollector']/property[starts-with(@name, 'HeatMap')]" />
```

This XML is already in `windows.xml` but is commented out.

If you are unfamiliar with the heatmap, then I recommend reading
[the Heatmap page in the official Wiki](https://7daystodie.fandom.com/wiki/Heatmap).

### About the files in this mod

Not all of the files and folders in this mod are required.
Many of these files and folders are used for development.
If you do not want to use this mod for your own development,
then you do not need these files.

#### Required

These files and folders are **required** for the mod to work.

* `Config`:
  Contains XML files that use XPath to change the game's configuration.
  Also contains alternate localizations.
* `ModInfo.xml`:
  XML file that tells 7D2D about the mod. Required for all mods.
* `RainCollector.dll`:
  Library file containing the compiled C# code in binary format.

#### Optional

These files and folders are optional.

* `README.md`:
  This file. :waving_hand:
* `Harmony`:
  Contains C# source code that is related Harmony patching.
  The source code is compiled into binary in the `RainCollector.dll` file.
* `Scripts`:
  Contains C# source code that is *not* related to Harmony patching.
  The source code is compiled into binary in the `RainCollector.dll` file.
* `RainCollector.csproj`:
  Visual Studio project file.
  Used for opening the source code in Visual Studio.
* `RainCollector.pdb`:
  Microsoft portable database file.
  Usually used for debugging purposes,
  it also allows 7D2D to get the stack traces of errors thrown from the Rain Collector code.
* `RainCollector.sln`:
  Visual Studio solution file.
  Used for opening the source code in Visual Studio.
