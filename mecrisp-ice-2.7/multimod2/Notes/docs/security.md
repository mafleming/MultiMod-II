# SPI Flash Security Related Functions
The functionality within this set of Forth words is primarily targeted towards protecting certain areas of flash, i.e. the bootloader, from being overwritten.

- **rdstatreg ( reg# -- value ) -** Read Status Register.  
Read the content of Status Register 1, 2, or 3. The top of stack is the JEDEC command to read the given status register, either $05 (SR1), $35 (SR2), or $15 (SR3). The return value is the byte value of the given register.  

- **wrstatreg ( value reg# -- ) -** Write Status Register.  
Write content to Status Register 1, 2, or 3. The top of stack is the JEDEC command to read the given status register, either $01 (SR1), $31 (SR2), or $11 (SR3).  

- **WPSset ( -- ) -** Set Write Protect Status (WPS) Flag.  
The Write Protect Status flag (bit 2 of SR3), when set to 1, allows each 4KB block in flash to be write  protected on an individual basis. When the flag is set to 0 then other status registers bits determine write protect protocol. If the flag is set, then all blocks are write protected following power-up or reset, and must be individually write unlocked prior to being programmed.  

- **WPSclr ( -- ) -** Clear Write Protect Status (WPS) Flag.  
Clearing the WPS flag is necessary if a previously locked block is to be written.  

- **blklock ( end start -- ) -** Lock Blocks In Flash.  
Lock all 4KB blocks in flash from the starting block to the end-1 block. The block range is 0..4095.  

- **blkunlock ( end start -- ) -** Unlock Blocks In Flash.  
Unlock all 4KB blocks in flash from the starting block to the end-1 block. The block range is 0..4095.  

- **blkstatus ( end start -- ) -** Display Lock Status Of Blocks In Flash.  
Display a series of 1's or 0's for each block in the range start..end-1.  
