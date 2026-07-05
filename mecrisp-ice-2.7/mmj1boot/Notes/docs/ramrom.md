# Flash <--> SPRAM Transfer Support
These Forth words build upon those *soft-spi* low level words for HP-71B ROM/IRAM image interaction with flash storage. The transfers work at the `sector16k` 16KB sector level.

## ROM And RAM Addressing
The MultiMod II 16MB flash device is divided into 1024 16KB segments, represented by the numeric value `sector16k`. The HP-71B ROM and IRAM images begin at the `romstart` (2MB) address mark. Valid values for `sector16k` range from 0 to 895.

The 128KB internal FPGA single port RAM is divided into eight corresponding 16KB segments, referred to as `ram#`, whose value ranges from 0 to 7.


## Image Transfer From Flash To Internal RAM
The following words copy a ROM or IRAM image from flash to internal FPGA RAM.

- **rom2ram ( sector16k ram# -- ) -** Copy 16KB Image To RAM.  
Copies a 16KB ROM image from flash into a 16KB RAM segment at **ram#**. Valid values are 0 to 7.  

- **rom32k2ram ( sector16k ram# -- ) -** Copy 32KB Image To RAM.  
Copies a 32KB ROM image from flash into two 16KB RAM segments, starting with segment **ram#**. Valid values are 0 to 6.  

- **rom64k2ram ( sector16k ram# -- ) -** Copy 64KB Image To RAM.  
Copies a 64KB ROM image from flash into four 16KB RAM segments, starting with segment **ram#**. Valid values are 0 to 4.  


## Image Transfer From Internal RAM To Flash
These flash write commands use the bitstream location restriction feature of the underlying `soft-spi` set of Forth words.

- **ram2rom ( ram# sector16k -- ) -** Copy 16KB Internal RAM To Flash.  
A internal 16KB RAM block **ram#** (usually IRAM) is copied to flash. Flash destination must be erased. Valid values of ram# is 0 to 7.  

- **ram32k2rom ( ram# sector16k -- ) -** Copy 32KB Internal RAM To Flash.  
A internal 32KB RAM block **ram#** (usually IRAM) is copied to flash. Flash destination must be erased. Valid values of ram# is 0 to 6.  

- **ram64k2rom ( ram# sector16k -- ) -** Copy 64KB Internal RAM To Flash.  
A internal 64KB RAM block **ram#** (usually IRAM) is copied to flash. Flash destination must be erased. Valid values of ram# is 0 to 4.  


## Internal SPRAM Access

- **ram2ram ( ramfrom# ramto# -- ) -** Copy One 16KB Internal RAM Block To Another.  
This is used to move images stored in internal RAM from one address to another, one 16KB block at a time.  

- **ramromcmp ( size ram# sector16k -- mem_addr flag ) -** Compare SPRAM to flash.
Compares the image in SPRAM, starting at 16KB page **ram#** (0~7) to the same flash image starting at **sector16k** and is **size** sectors long. The last memory address examined is returned along with a Pass/Fail flag. If the images don't match the **ram_addr** indicates the point of first mismatch.  

- **zeroram ( ram# -- ) -** Clear RAM Block To Zero's.  
Clearing a 16K RAM block is done prior to creating an IRAM module. Valid values of **ram#** is 0 to 7.  

- **onesram ( ram# -- ) -** Set RAM Block To One's.  
Setting a 16K RAM block to all $FF is done prior to loading a bitstream and writing the content to flash. Valid values of **ram#** is 0 to 7.  
