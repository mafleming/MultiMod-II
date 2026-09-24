# Forth HP-71B Memory Manager for the MultiMod II
The J1a soft Forth CPU was selected in order to implement the intelligent functionality of the MultiMod II as an HP-71B accessory. The use of Forth as the base language complemented the availability of Forth for the 71B itself.

The mecrisp-ice project combines the J1a Forth CPU with the Mecrisp ANSI Forth implementation that targets the J1a instruction set. Details about the J1a implementation can be found at James Bowman's [github repository](https://github.com/jamesbowman/swapforth). More information about Mecrisp Forth can be found at its [Sourceforge](https://mecrisp.sourceforge.net/) site. [Unofficial documentation](https://mecrisp-stellaris-folkdoc.sourceforge.io/) for Mecrisp Forth can be found on Sourceforge as well.

This FPGA configuration consists of two parts; the j1a processor, and the HP71B bus Memory Manager. The manager presents the content of the 128KB SPRAM memory in the FPGA as a set of 16KB ROM or RAM devices to the HP71B Saturn processor. Up to eight 16KB devices can be connected to the HP71B four-bit bus, and respond to bus commands to read or write data to the devices.

This version of the J1a is designed to support the 71B Memory Manager by configuring the manager and copying images between SPRAM memory and SPI flash memory. The Forth console is a serial device whose data and control registers are mapped to the HP71B address space so that a program can communicate commands and receive responses.


## Project Layout
Board design and documentation for the mecrisp Forth implementation can be found in separate directories beneath the main MultiMod-II directory. The ```mecrisp-ice-x.xx``` directory contains only the board support directories for the MultiMod II. The ```multimod2``` directory contains the necessary build scripts, the default Forth files that support MultiMod II operation, and the board definition files. See the Build section of this documentation for details.

## Forth Operation
The current mecrisp-ice configuration supports the default implementation that communicates using a serial device for the default user console. Details are provided in the appropriate sections of the Notes directory.