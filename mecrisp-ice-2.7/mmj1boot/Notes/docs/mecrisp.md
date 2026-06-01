# Mecrisp Forth Implementation
The mecrisp-ice project combines the J1a Forth CPU with the Mecrisp ANSI Forth implementation that targets the J1a instruction set. Details about the J1a implementation can be found at James Bowman's [github repository](https://github.com/jamesbowman/swapforth). More information about Mecrisp Forth can be found at its [Sourceforge](https://mecrisp.sourceforge.net/) site. [Unofficial documentation](https://mecrisp-stellaris-folkdoc.sourceforge.io/) for Mecrisp Forth can be found on Sourceforge as well.

From the mecrisp Sourceforge page on **Mecrisp-ice**:

>Mecrisp-Ice is a 16 bit Forth running on a stack machine specifically developed for FPGAs, originally based on Swapforth and the J1a processor by James Bowman. Mecrisp-Ice requires initialised single-cycle dualport RAM blocks to run and is developed with excellent realtime capabilities and deterministic interrupt timing in mind. Due to instruction set design, the maximum (and recommended) amount of addressable executable memory is 16 kb, with an usable minimum of 8 kb.

>The 16 bit implementation is stable and rock solid, whereas the 32 bit and 64 bit implementations with support for larger executable memories should be considered experimental.

