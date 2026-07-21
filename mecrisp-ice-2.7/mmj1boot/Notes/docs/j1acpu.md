# The j1a Forth CPU

The original j1a CPU was developed by James Bowman. The Verilog source can be found at the following [github repository](https://github.com/jamesbowman/j1).

The version of the j1a CPU used in the MultiMod II was used to port the [Mecrisp Forth](https://mecrisp-stellaris-folkdoc.sourceforge.io/index.html) implementation, originally developed for Stellaris ARM microprocessors, to the Lattice iCE40 family of FPGAs. Since development began on the [Fomu](https://tomu.im/fomu.html) iCE40UP device, the j1a Verilog for the MultiMod II is derived from there.

## MultiMod II CPU Origins
The j1a CPU that is part of the MultiMod II is the [Mecrisp-Ice for FPGA](https://mecrisp.sourceforge.net/) source found on Sourceforge. The Forth language implementation originated with James Bowman's *Swapforth*, found alongside his j1a CPU implementation. From the source description

> *Mecrisp-Ice is a 16 bit Forth running on a stack machine specifically developed for FPGAs, originally based on Swapforth and the J1a processor by James Bowman. Mecrisp-Ice requires initialised single-cycle dualport RAM blocks to run and is developed with excellent realtime capabilities and deterministic interrupt timing in mind. Due to instruction set design, the maximum (and recommended) amount of addressable executable memory is 16 kb, with an usable minimum of 8 kb.*
> 
> *The 16 bit implementation is stable and rock solid, whereas the 32 bit and 64 bit implementations with support for larger executable memories should be considered experimental.*

## MultiMod II CPU Modifications
The following modifications have been made to the stock Mecrisp-Ice j1a CPU implementation.

### CPU Warm Boot Support
The iCE40 UltraPlus, like all iCE40 FPGA's, supports warm boot. Only the HX/LX series supports cold boot. The warm boot feature allows a configured iCE40 FPGA to dynamically reconfigure itself from one of four bitstreams within an attached serial flash device. The default numeric limit itself can be easily extended to many more bitstream configurations. The reconfiguration process is mediated by the so-called *boot applet* stored at the beginning of flash memory.

Within the IceStorm toolset (available for both Linux and Windows WSL) the ```icemulti``` command is used to concatenate the boot applet and up to four bitstream images generated separately by the IceStorm toolset. Command options determine such things as which bitstream is loaded on cold boot and where images are stored in flash.

For a bitstream image to initiate a warm boot, the **SB_WARMBOOT** IP instance must be incorported into the Verilog design. This instance has two inputs that select which of four bitstream images to load, and one input that serves to trigger the warm boot process. In the MultiMod II bootloader, these three signals are controlled by the j1a Forth CPU and the process is under program control.

### SPI Flash Support
The Winbond 16MB flash device that is used to boot the MultiMod II FPGA is also used to store ROM and IRAM images. The Forth code used to bitbang the SPI device is derived from the port of mecrisp-ice, [UPduino-Mecrisp-Ice](https://github.com/igor-m/UPduino-Mecrisp-Ice-15kB.git) by igor-m under the BSD 3-Clause license.

Modifications were made to support SPI communication for Dual and Quad mode transfers in addition to the Standard mode support of the original code.


## MultiMod II CPU Address Space
The j1a CPU selected for the MultiMod II is a 16-bit word size, 16-bit address size processor. The memory address space for stack and dictionary is mapped to the 15KB of dual-port RAM blocks in the FPGA. Access to this memory is on a single cycle clock basis.

The processor also addresses a dual external address space arrangement; a 64K word RAM address space, and a 64K word I/O address space. The FPGA's 128KB of single port memory, organized as four 16KB by 16 bit RAM blocks, occupies the entirety of the RAM address space.

