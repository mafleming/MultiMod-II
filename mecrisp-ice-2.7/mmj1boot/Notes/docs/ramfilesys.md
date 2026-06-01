# SPRAM File System Support
These Forth words are used to access the SPRAM in a Lattice iCE40 FPGA to initialize, access or modify content used by an HP-71B computer.

These Forth words support access to SPRAM on a nibble basis. The SPRAM memory itself is organized as four 16K by 16-bit wide words, and accessed with a 14-bit address. The four SPRAM blocks are combined to form a 64K by 16-bit block of memory.

## Low Level Access
These HP-71B oriented Forth words use the same 20-bit address as the HP-71B itself for its address space.

- **nib@ ( ud_nibble_addr -- nibble ) -** Nibble read access.
<br>
Read a nibble from SPRAM memory using an unsigned double 20-bit address, like that used by the HP-71B.  
<br>
- **nib! ( nibble ud_nibble_addr -- ) -** Nibble write access.
<br>
Read a nibble from SPRAM memory using an unsigned double 20-bit address, like that used by the HP-71B.  
<br>
- **peek ( ud_nibble_addr count -- ud_nibbles ) -** Read 1 to 8 nibbles.
<br>
The **count** value is in the range of 1 to 8. The number of nibbles specified are returned as an unsigned double, first nibble in low end.  
<br>
- **poke ( ud_nibble_addr ud_nibble_value count -- ) -** Write 1 to 8 nibbles.
<br>
Write **count** nibbles in **ud_nibble_value** to address **ud_nibble_addr** where count is 1 to 8.  
<br>
- **mk16kiram ( ram# -- ) -** Create a 16K IRAM in SPRAM.
<br>
Given a **ram#** block number, zero out a 16K block of SPRAM and then write an IRAM eight nibble Module ID Field to the beginning of the block. Valid value is 0 to 7.  
<br>
- **mk32kiram ( ram# -- ) -** Create a 32K IRAM in SPRAM.
<br>
Given a **ram#** block number, zero out two 16K blocks of SPRAM and then write an IRAM eight nibble Module ID Field to the beginning of the first block. Valid value is 0 to 6.  
<br>
- **mk64kiram ( ram# -- ) -** Create a 16K IRAM in SPRAM.
<br>
Given a **ram#** block number, zero out four 16K blocks of SPRAM and then write an IRAM eight nibble Module ID Field to the beginning of the first block. Valid value is 0 to 4.  
<br>

## File Header Access

## File Access
