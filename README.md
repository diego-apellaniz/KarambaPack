# Karamba Package by Diego Apellániz
This repository contains the extension plugin „KarambaPack“ for the Grasshopper plugin [Karamba3d](https://www.karamba3d.com/), which offers additional functionalities to this structural analysis tool (see examples). It also contains example files and the source code. Click on „Releases“ to download the last version.

⚠️⚠️ ### Shutdown
After the official release of Karamba3d v2.2.0, I've decided to stop the development of this project. Karamba will implement Load Combinations very soon and the new Karamba Component “Model Query” already replaces my component for LCA. I hope it doesn’t cause too much trouble to your workflows. I just can’t find time to update the plugin after each Karamba release. Thank you very much for your support all this time!
- Diego Apellániz

## How to install
Download the plugin and save both the .gha and the common .dll file in the same folder where the plugin Karamba is installed (C:\Program Files\Rhino xx\Plugins\Karamba). Also check that the both files are not being blocked.

## Examples
### Load Combinations
With the component „Load Combinations“ it is possible to combine in a linear way the result of different load cases from a Karamba model. The format for the list of load combinations can be seen in the example and it can be automatically generated with commercial programs such as RFEM.
![alt text](https://github.com/diego-apellaniz/KarambaPack/blob/main/Pictures/Load%20Combinations.png?raw=true)

### Mass Source
Karamba3d offers the possibility of dynamic analyis. However, the mass source is always automatically taken from the load case Self-Weight. With this component, the mass source can be defined in the same way as a load combination (see previous example).
![alt text](https://github.com/diego-apellaniz/KarambaPack/blob/main/Pictures/Mass%20Source.png?raw=true)

### Life Cycle Analysis - Carbon Foodprint
This component generates a bill of quantites out of a Karamba modell, so the masses and geometries of each element are grouped by material. This results can be then fed into a LCA tool for calculating the carbon foodprint of the structure. In this examples the plugin of One Click LCA was used as such tool.
![alt text](https://github.com/diego-apellaniz/KarambaPack/blob/main/Pictures/OneClick_Karamba.png?raw=true)
