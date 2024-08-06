# Snowberry Love

Snowberries were removed from the game in A20.
This mod restores them to the game, and adds new uses for them.

* :x: Server side only (clients do not need to install the mod)
* :heavy_check_mark: EAC safe (clients and servers do not need to disable Easy Anti-Cheat)

## Features
In the real world, snowberries were used mostly for medicinal purposes by Native American tribes.
They also taste like wintergreen (reportedly - I have not eaten them).
I based the in-game uses on these facts.*

* Snowberries can be grown from seeds and harvested.
  The seed recipe is unlocked from the same Living Off The Land perk level as blueberry seeds.
* Snowberry tea can be crafted in a campfire with a cooking pot.
  The tea gives 24 water and cures dysentery 10%.
* Snowberry paste can be used the same as medical aloe cream,
  and can be used instead of aloe cream when crafting first aid bandages.
  The paste takes five snowberries to craft.
* Snowberries may be used instead of blueberries in herbal antibiotics recipes.
* Grandma's Gin: "How intelligent people get drunk and belligerent."
  It provides the buffs of beer, combined with 10% faster crafting time,
  an additional 1 point of intelligence,
  and 10% more experience gained.
  (The latter three used to be granted by nerdy glasses, which were removed in 1.0.)
  The gin recipe is unlocked at the same crafting level as Grandpa's Moonshine.
* To preserve balance, snowberries spawn less often.

\* References:
* [Wikipedia](https://en.wikipedia.org/wiki/Symphoricarpos#Cultivation_&_Medicinal_Uses)
* USDA [Plant of the Week: Creeping Snowberry](https://www.fs.usda.gov/wildflowers/plant-of-the-week/gaultheria_hispidula.shtml)
* USDA [Snowberry Plant Fact Sheet](https://plants.usda.gov/DocumentLibrary/factsheet/pdf/fs_syal.pdf) (PDF)

## Technical Details

This modlet uses XPath to modify XML files, and does not require SDX or DMT.
It should be compatible with EAC.

However, the modlet also includes new non-XML resources
(new icons).
These resources are _not_ pushed from server to client.
For this reason, the modlet should be installed on both servers and clients.

### Re-use of new assets

Most of the icon images are derivative works of The Fun Pimps original images.
I do *not* claim any rights over these images.

You may only re-use the images under the same terms and conditions that you would
use the original images from The Fun Pimps.

If you are using those derivative works under TFP's terms and conditions,
then you do _not_ need to ask my permission.

_However,_ the "Grandma's Gin" icon also incorporates this Gin Bottle image from Wikimedia Commons:

> [![Bottle, gin (AM 616677-5).jpg](https://upload.wikimedia.org/wikipedia/commons/thumb/3/3c/Bottle%2C_gin_%28AM_616677-5%29.jpg/180px-Bottle%2C_gin_%28AM_616677-5%29.jpg)](https://commons.wikimedia.org/wiki/File:Bottle,_gin_(AM_616677-5).jpg)
> 
> **Image by Udolpho Wolfe, released as CC-BY by the Auckland Museum.**

Re-using that image will require the above credit to be placed in your mod.

If you want to re-use this mod without that credit,
an older icon is still in the `UIAtlases/ItemIconAtlas` directory.
It only uses assets from The Fun Pimps.

To use it:
* Delete or rename the `drinkJarGrandmasGin.png` file
* Rename the `drinkJarGrandmasGin-old.png` file to `drinkJarGrandmasGin.png`

The game should pick up the icon automatically.
