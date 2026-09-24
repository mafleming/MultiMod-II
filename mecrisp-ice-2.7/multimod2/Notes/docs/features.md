# Features and Requirements for the MultiMod II Memory Manager
As an accessory for the HP-71B, the MultiMod II is meant to provide both ROM and RAM plugin modules. All known ROMs should be available for access by a MultiMod II owner. The owner should also be able to plug in RAM modules, either in merged form or as independent RAM (IRAM). The content of IRAM modules should also be able to be saved to flash memory so its updated content can be accessed at a later time.

In addition to providing regular relocatable ROM and RAM modules, the MultiMod II will also support fixed memory at two locations; a fixed hard ROM at location $E0000 and a takeover ROM at location $00000. The former is used by the Forth/Assembler ROM among others. The latter requires a separate shorting module in Port 1 to disable the built-in System ROM.

## Warm Boot Support
A hardware modification to the mecrisp-ice J1a CPU is added to support warm boot. The modification allows a Forth program to initiate the warm boot process by supplying the address of a new bitstream pattern in flash and initiating the warm boot design confirguration replacement process.

Four bitstream images are supported. Bitstream 0 is the bootloader, bitstream 1 is this embedded production configuration, and bitstreams 2 and 3 can support alternative configurations for the HP-71B or for a connected USB Host.

## File Storage
The 16MB flash memory is treated as a structure of 1024 blocks of 16KB each, where 16KB is the size of a Forth dictionary image or a size multiple of HP-71B ROM and RAM images.

The first megabyte of flash memory is reserved for the bitstream images and any associated data they may require. The second megabyte of flash is reserved for Forth dictionary images. The remaining fourteen megabytes are used to store HP-71B ROM and IRAM images. Separate directories for Forth and HP-71B images store the name and location of images in flash. The directories allow the HP-71B owner to plug in ROM or IRAM modules by name.