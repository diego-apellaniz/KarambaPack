# Karamba Package by Diego Apellániz
This repository contains the extension plugin „KarambaPack“ for the Grasshopper plugin Karamba3d, which offers additional functionalities to this structural analysis tool (see examples). It also contains example files and the source code. Click on „Releases“ to download the last version.

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
