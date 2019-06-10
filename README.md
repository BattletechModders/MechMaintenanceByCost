# MechMaintenanceByCost
BattleTech mod (using BTML) that changes your maintenance cost of your mechs from a flat sum for all mechs, to a cost, specific to the cost of the mech chassis.

## Requirements
* Install [Modtek](https://github.com/BattletechModders/ModTek/releases) using the [instructions here](https://github.com/BattletechModders/ModTek)

## Features
- Your monthly mech cost now depend on the chassis.
- This makes Light mechs more attractive and Assault mechs more hard to sustain.

## Download

Downloads can be found on [github](https://github.com/BattletechModders/MechMaintenanceByCost/releases).

## Settings
Setting | Type | Default | Description
--- | --- | --- | ---
PercentageOfMechCost | float | default 0.003 | The percentage of the chassis cost you have to pay monthly. 1 = 100% / 0 = 0%
CostByTons | bool | default false | set this to true if you want tonnage to be the factor to determine the drop costs.
cbillsPerTon | int | default 500 | if CostByTons is true, this sets the cost per ton.
TonsAdditive | bool | default false | If both tons flags are true adds the tons value to the normal calculated value.
    
## Install
- After installing Modtek, put  everything into \BATTLETECH\Mods\ folder.
- If you want a different percentage set it in the settings.json.
- Start the game.
