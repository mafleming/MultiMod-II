# MultiMod II for the Hewlett Packard HP-71B Pocket Computer
The MultiMod II is a successor to the [MultiMod ROM](https://github.com/mafleming/HP71MultiMod) emulator board for the HP71B. The original board fit into the card reader slot of the HP-71B and could emulate up to 120 KB of ROMs.

The MultiMod II can emulate up to 128 KB of mixed ROM, RAM, and Independent RAM (IRAM). ROM and IRAM images are stored in a 16 MB flash memory device. Images can be loaded, unloaded, or saved by user command. The HP-71B owner can configure ROM and RAM on the fly to match current needs, and backup IRAM content to flash so that no work is lost if batteries fail.

## Design Decision Issues
There are several design decisions that affect the functionality and usability of the MultiMod II. First and formost is the choice of a bootloader. The are a number of suitable bootloaders for the Lattice FPGA platform, but they are limited to making production updates, with little or no support for other operations.

The choice of how to implement a file system in flash for Forth dictionary images and for HP-71B ROM/IRAM images has a number of constraints that must be managed. Another issue is whether to have separate directories for Forth images and HP-71B images, or just a single combined directory.

The user interface for the MultiMod II when plugged into a USB host has important implications for ease of use by owners of differing technical sophistication. The interface should make software updates simple, while also supporting files transfers with a minimal host-side client requirement.

## Future Enhancements

The MultiMod II is designed to easily update the FPGA configuration and Forth support software via its USB connection. Plans also include support for the embedded Forth machine to access IRAM file systems when installed in an HP-71B and to act as a coprocessor for the 71B itself.
