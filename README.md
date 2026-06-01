HP-71B MultiMod II Repository

# MultiMod II, ROM and RAM Storage for the HP-71B

The MultiMod II is a follow-on development of the MultiMod ROM Emulator for the HP-71B handheld computer. The MultiMod was based on a Microchip PIC processor that stored ROM images in its flash memory and made those ROMs accessible to the HP-71B operator. However, the MultiMod could not provide RAM memory.

The MultiMod II is based on a Lattice iCE40UP5K FPGA with 128KB of single port RAM (SPRAM) and 15KB of dual port RAM. The FPGA fabric provides the logic needed to present the content of the SPRAM as a series of both ROM or RAM devices. The Winbond 16KB serial flash memory serves to hold up to four FPGA configuration bitstreams, and is also used to store ROM images that the HP-71B operator can dynamically plug and unplug into the HP-71B as needed.

# MultiMod II Board Design
The MultiMod II PCB is a four layer, two sided component design. The design was done in Kicad 9.0 and the design files can be found in the **board** directory.

# MultiMod II FPGA Configuration
To configure and provide content for ROM and RAM memories, a soft CPU is instantiated in fabric using the 15KB of dual port RAM for its programs. The J1a Forth CPU was selected in order to implement the intelligent functionality of the MultiMod II HP-71B accessory. The use of Forth as the base language complemented the availability of Forth for the HP-71B itself. The basis files can be found in the **mecrisp-ice-2.7** directory.

The mecrisp-ice project combines the J1a Forth CPU with the Mecrisp ANSI Forth implementation that targets the J1a instruction set. Details about the J1a implementation can be found at James Bowman's [github repository](https://github.com/jamesbowman/swapforth). More information about Mecrisp Forth can be found at its [Sourceforge](https://mecrisp.sourceforge.net/) site. [Unofficial documentation](https://mecrisp-stellaris-folkdoc.sourceforge.io/) for Mecrisp Forth can be found on Sourceforge as well.

This configuration was customized for two purposes; one configuration serves as a programmable bootloader when connected to a host, while the second is the production configuration when the MultiMod II is operating in the HP-71B.

## Bootloader Configuration
The bootloader Verilog and Forth files can be found in the **mmj1boot** subdirectory within the **mecrisp-ice** containing directory.

The bootloader configuration uses the standard mecrisp-ice j1a processor and USB device to communicate with a connected host. Going beyond the standard bootloader ability to update the production configuration, the interactive Forth console provides the means to transfer files and even extend its own capabilities.

## Production Configuration
The bootloader Verilog and Forth files can be found in the **multimod** subdirectory within the **mecrisp-ice** containing directory.

The production bitstream configuration replaces the mecrisp-ice USB device interface with a serial interface. The serial device data and control registers are mapped into the HP-71B address space so that the HP-71B can interact with the Forth console. This gives the HP-71B operator the ability to configure the MultiMod II, plugging and unplugging ROMs at will. Independent RAM (IRAM) device content can be saved to flash, allowing the operator to compose collections of programs that can be saved to flash and later loaded when needed. All the functional capability of an external storage device are provided by the MultiMod II.