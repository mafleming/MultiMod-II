# Features and Requirements for the MultiMod II Bootloader
As an accessory for the HP-71B, the MultiMod II is meant to provide both ROM and RAM plugin modules. All known ROMs should be available for access by a MultiMod II owner. The owner should also be able to plug in RAM modules, either in merged form or as independent RAM (IRAM). The content of IRAM modules should also be able to be saved to flash memory so its content can be accessed at a later time.

In addition to providing regular relocatable ROM and RAM modules, the MultiMod II will also support fixed memory at two locations; a fixed hard ROM at location $E0000 and a takeover ROM at location $00000. The former is used by the Forth/Assembler ROM among others. The latter requires a separate shorting module in Port 1.

## Bootloader
The Forth bootloader serves three purposes; to act as a bootloader that updates the MultiMod II production configuration under user control, to transfer files between the MultiMod II and a user host PC, and to develop Forth applications for use by the production Forth CPU in the HP-71B.

First, a hardware modification to the mecrisp-ice J1a CPU is needed to support warm boot. The modification allows a Forth program to initiate the warm boot process by supplying the address of a new bitstream pattern in flash and initiating the warm boot design confirguration replacement process.

Second, a user interface is needed for updating the FPGA production configuration and transferring files between the MultiMod II and the user host. Given the USB-based Forth console for communication, a terminal application like TeraTerm with a file transfer protocol like Kermit should be a sufficient client for the owner.

## File Storage
Transferring files implies a file system, and that means some form of a file identification structure is needed. There are a number of open source flash file systems, but these assume small sector sizes (256/512 bytes) and plenty of host RAM. Neither condition is true for the MultiMod II where flash sector size is 4KB and only a fraction of the 15KB Forth dictionary is available for scratch space.

The 16MB flash memory will be treated as a structure of 1024 blocks of 16KB each, where 16KB is the size of a Forth dictionary image or a size multiple of HP-71B ROM and RAM images.

Transfer of ROM image data in the MultiMod was by sending ASCII Hex representation of ROM data via a serial port. While this mode is still supported by the MultiMod II, the Kermit protocol will be used to transfer raw binary image files between the MultiMod II flash and a host PC. This protocol is also suitable for transferring updates to the production FPGA bitstream image.

## Forth Development
There is little reason to develop Forth applications on the HP-71B itself when one can interact with the USB-based Forth console from a host using a terminal client. Furthermore, adding words to the Forth dictionary is a simple matter of sending a text file containing Forth definitions via the client. Thus, text development can be performed on the host, and a target dictionary can be saved to flash for later use in the HP-71B production environment.

## Optional: HP-71B Emulation
If an owner wishes to develop a Forth application that can interact with the HP-71B in actual production operation, then some means is needed to simulate the 71B behaviour. One example would be to develop words that can interact with and modify an IRAM filesystem. A Forth application could then read and write data to local files in their FPGA SPRAM environment.