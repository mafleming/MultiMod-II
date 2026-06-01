# MultiMod II Flash Memory
The MultiMod II board has a 16MB Winbond flash device. When the FPGA exits power-up or reset, it reads its initial configuration bitstream from flash starting at address 0. It can optionally read alternative configuration bitstreams from other locations in flash, a process known as **warm boot**.

Flash memory is a byte-serial device rather than random access memory. A flash memory location must first be erased (all one's) before it can be written. The smallest block of flash that can be erased is 4 kilobytes. A read or write operation is always preceded by a command byte followed by a three byte (24-bit) memory address. Data can then be streamed to or from the flash device on a bit serial basis. The design of the MultiMod II allows an optional quad serial transfer interface.

The MultiMod II treats its 16 MB flash memory as a array of 1024 blocks of 16 KB size, with each block on a 16 KB address boundary. The flash memory is divided into three sections; section one for FPGA bitstreams and their associated data, section two for Forth dictionary images and data files, and the remaining third section for 71B ROM and IRAM images. ROM and IRAM images are 1,  2, or 4 blocks in size (16/32/64 KB).

> *Need a picture here of flash memory and sector addressing*

## Boot Section
The first megabyte of flash memory is reserved for configuration bitstreams as well as any data associated with a given FPGA configuration. In the **foboot** bootloader, the starting address of a bitstream varies according to post-boot data requirements, but roughly $020000 (128KB) is reserved for each bitstream. For the MultiMod II, four bitstream areas will be reserved in the lower half of the boot section, with the upper half serving as optional data storage for a given bitstream device.

## Forth Section
The J1a Forth CPU has its dictionary in the 15KB dual port RAM of the Lattice FPGA. There are **load** and **save** commands which allow the dictionary to be written to flash or read from flash in its entirety. This gives a programmer the ability to develop applications and save them to flash for later use.

The second megabyte of flash memory is reserved for Forth dictionary images. Since an image occupies a 16KB block of flash, the Forth Section has room for 64 dictionary images. The first two blocks are reserved for a directory of images and a scratch block for image manipulation. The remaining 62 blocks store dictionary images.

## HP-71B Section
The remaining 14MB of flash is reserved for ROM images and IRAM images. These images are multiples of 16KB in size, hence like the Forth Section storage is divided into 768 blocks. The first two of these blocks are reserved for a directory of images and a scratch block for image manipulation.