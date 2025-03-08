This is a fairly straightforward airlock controller for the game Space Engineers, allowing the setup and control of secure airlock systems that enforce a proper cycling sequence - only one side of the airlock can be open at a time, unless the external environment is deemed safe.

Setup requires that you rename the critical blocks that make up each airlock according to the following naming scheme:
- Doors: '<Airlock Name> Inner Door' for internal-facing doors, and '<Airlock Name> Outer Door' for external-facing doors.
- Vents: '<Airlock Name> Vent' for the airlock vents, and include 'Intake' in any vents you wish to use to check the external environment.
- Button panels: So long as the airlock's name and 'Panel' are included in the name, they'll be assigned to their respective airlocks.

As an example, say you have an airlock on the port side of your ship. Let's call it 'Port'. You'd have the following blocks at a minimum:
- Doors: 'Port Inner Door', 'Port Outer Door'
- Vent: 'Port Vent'
- Button panels: 'Port Inner Panel', 'Port Panel', 'Port Outer Panel'

Once these naming requirements are met, you'll need to assign actions to the button panels; simply assign the 'Run with argument' action with the argument 'cycle <Airlock Name>' (example 'cycle Port') - note that the airlock's name is **not** case-sensitive ('cycle port' and 'cycle pOrT' will both work just the same).

Recompiling the script will refresh the list of airlocks.

This controller handles two error states at present:
- If the airlock can't decompress - for example if no oxygen tank is available or has space.
- If the airlock can't fully pressurise - for example if not enough oxygen is available. Note that this may also occur if the airlock is filling too slowly.
In both cases, any of the airlock's button panels with screens will begin to flash with the word 'ERROR'. Hitting the button again in this state will force the airlock to cycle the doors as normal, possibly venting any air inside.
