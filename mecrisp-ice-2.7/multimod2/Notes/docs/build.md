# MultiMod II Production Build Instructions
These instructions assume a Linux build environment, specifically [Pop!_OS](https://pop.system76.com/), a Debian variant. Other Linux distributions are likely sufficient if they are supported by the toolchains used herein. The author has used WSL (Windows System for Linux) as well for early exploration.

## Setup
This version begins by building the design in the `multimod2` directory using
the [Fomu](https://www.crowdsupply.com/sutajio-kosagi/fomu) development platform's designer-provided toolset. The tar file containing the [icestorm tools](https://github.com/im-tomu/fomu-toolchain)
should be installed in /opt/fomu-toolchain-Linux. Place its bin subdirectory
FIRST in your PATH environment variable.

```export PATH=/opt/fomu-toolchain-Linux/bin:$PATH```

Use `sudo apt install gforth` to install an old version of gforth in order  to compile the [latest Gforth](https://gforth.org) source files. The older default version of gforth works fine, though I did build the latest version from
source just in case any significant changes or improvements to gforth occurred.

## Compilation and Synthesis
Within the `multimod2` directory use the `compile` command script to create an initial Forth dictionary image using `compilenucleus` and using the above icestorm toolset to synthesize the J1A CPU design. The resulting bitstream file, j1a.bin in the `build` directory, is copied over to the `mmj1boot` directory for inclusion in the multiboot bitstream build.

There is a correction made to the stock build scripts. An unrecognized option appears in the first command in the compile script that invokes yosys, the `-noabc9` option. The build completes successfully with this option removed.

The script needs a seed value when invoking the `nextpnr-ice40` place and route program. The value used (11) was determined by successively trying values, starting with 1, until a seed was found that allowed synthesis to complete with all clock constraints satisfied. At the end of the build, the bitstream file `j1a.dfu` will appear in the `multimod2` directory. Usable files for loading by the bootloader also appear as `j1a-prod.bin` and `j1a-prod.dat` in the `build` directory.


### Communication

Development and testing of the production bitstream is done within the bootloader environment with the MultiMod II connected to the development computer using a USB C cable. Instructions for using a specific Linux client to communicate with the bootloader is given here. The details can be used as a guideline if a different communications client (eg TeraTerm) is used.

Use ```minicom -b 115200 -D /dev/ttyACM0``` to connect to the Forth CPU. Note that you'll need to add yourself to the dialout group. An alternative terminal program is GTKTerm, which is more easily configurable.

Invoke minicom communication (control-A Z) then set lineWrap on/off (control-A Z W) followed by Add Carriage Ret (control-A Z U). Set the comm parameters to 8N2 (control-A Z P X). You'll need to add some delay to each character sent by minicom to the Forth console via the terminal settings (control-A Z T). Set the character send delay to 1 milliseconds and the line end delay to 50 milliseconds. **The same character delay will be needed by other terminal emulators, such as TeraTerm.**

***For the above settings to work properly, use the `dint` command at the start of your working session to disable interrupts.***


## Production Build
Use the `compile` command to build a production bitstream image for the MultiMod II board. The build will copy the resulting **.bin** file to the `mmj1boot` directory and also create an ASCII file that can be transferred to the bootloader.

After connecting the MultiMod II to your host via a USB-C cable, establish communication as outlined in the previous section. Then execute the following command in the Forth console

```
1 bitstream   \ Program bitstream image slot 1
```

With minicom, use control-A Z, then S for Send. Select ascii transfer then highlight the `j1a-prod.dat` in the `build` directory. Press return to start the transfer and a second return to initiate flash programming. Programming the new production bitstream takes around six to eight seconds before the Forth *ok* response appears.

Once the production bitstream is loaded, use the following command to load the bitstream into the FPGA

```
5 warmboot
```

***ASCII transfers may in the future be augmented a with Kermit option***