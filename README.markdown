Encephal16 is an implementation of the DCPU-16 VM written in C# and designed to be easily embedded into other games and simulators.

* Functional 1.7 VM in a library
* Library of WPF controls for viewing VM RAM and registers
* Simple application to load a .dcpu16 image into the VM and step through execution
* Compatible with the dcputoolcha.in assembler and compiler output.
* LEM1802 hardware device and WPF control to view the screen.  Hook up as many as you want!

Currently it implements the 1.7 version of the specification, and now includes the hardware interface instructions.

So far it's been tested by compiling the test code from the end of the 1.1 spec using the dcputoolcha.in assembler and runs that code perfectly.  [Screenshots here] (https://github.com/btodoroff/Encephal16/wiki/Screenshots)

Current goal is to implement a simple dueling cannons game (think Scorched Earth) where the two cannons are controlled by independent DCPU-16 controllers.

Upcoming features:
* Hardware interface
* Display screen
* Keyboard
* Maybe a TCP debug interface... would be nice to have one dev environment to debug processors in a different process.

BHAG: Implement a dueling robot tanks game ala OGRE. (80's PC game, not Steve Jackson tabletop game)

<a rel="license" href="http://creativecommons.org/licenses/by-sa/3.0/deed.en_US"><img alt="Creative Commons License" style="border-width:0" src="http://i.creativecommons.org/l/by-sa/3.0/88x31.png" /></a><br /><span xmlns:dct="http://purl.org/dc/terms/" property="dct:title">Encephal16</span> by <a xmlns:cc="http://creativecommons.org/ns#" href="www.todoroff.com" property="cc:attributionName" rel="cc:attributionURL">Brian Todoroff</a> is licensed under a <a rel="license" href="http://creativecommons.org/licenses/by-sa/3.0/deed.en_US">Creative Commons Attribution-ShareAlike 3.0 Unported License</a>.<br />Based on a work at <a xmlns:dct="http://purl.org/dc/terms/" href="https://github.com/btodoroff/Encephal16" rel="dct:source">https://github.com/btodoroff/Encephal16</a>.