
# Acknowledgements
Select Forth code in this file is derived from UPduino-Mecrisp-Ice-15kB by Igor-m
BSD 3-Clause license

See: https://github.com/igor-m/UPduino-Mecrisp-Ice-15kB.git

# Flash Memory Structure
Flash memory can't be written on an individual memory cell basis; a sector must be erased and then the modified contents of the sector written back. Data can be written in smaller amounts to a sector but the sector must first be erased. In the Winbond standard SPI flash device, sectors can be erased in 4 KB, 32 KB or 64 KB sized blocks.

The MultiMod II treats its 16 MB flash memory as a array of 1024 blocks of 16 KB size, with each block on a 16 KB address boundary. The flash memory is divided into three sections; section one for FPGA bitstreams and their associated data, section two for Forth dictionary images and data files, and the third section for 71B ROM and IRAM images. ROM and IRAM images are 1,  2, or 4 blocks in size (16/32/64 KB).

# SPI Flash Memory Support
The words in this file provide support for SPI flash memory devices using a "bit-bang" approach to communication. This admittedly simple approach to implementing an SPI interface is nonetheless fast enough for purposes of the MultiMod II project.

The Low Level Support section describes the software implementation of the SPI hardware protocol. Should the built-in hard SPI fabric be used (or soft SPI driver), its code would replace the words found in the Low Level Support section.

The API Functions section are words used by the remainder of the MultiMod II support code. These functions treat the SPI flash memory as a sequence of 16 KB pages - in the case of a 16 MB flash device, pages 0 to 1023.

## Constants and Variables
The following definitions define boundaries within flash for bitstream, Forth dictionaries, and ROM/IRAM images. Low level words use these values to partition flash and protect critical sections.

- **core4th -** Mark The End Of Mecrisp Forth Words.  
This word marks the default dictionary words provided by Mecrisp Forth. It cn be used to purge all dictionary entries that follow if the entirety of the MultiMod II words are to be completely replaced with a new version, starting here at the SPI interface found in the *soft-spi.fs* file.  
<br>

- **bitfence (constant 8) - ** Bitstream Partition Point.  
The 16KB page below which no writes can occur. Flash address $20000 or 128KB.  

- **frtstart (constant 64) - ** Start Of Forth Dictionary Images.  
Starting 16KB page where Forth dictionary image storage begins. The first two pages are reserved for the image directory. Flash address $100000 or 1MB.  

- **romstart (constant 128) - ** Start Of HP-71B ROM/IRAM Images.  
Starting 16KB page where HP-71B image storage begins. The first two pages are reserved for the image directory. Flash address $200000 or 2MB.  
<br>

## Low Level Support
The Forth words in this section implement the software-based SPI interface. These functions are not used by ordinary user functions.

- **spimode_std ( -- ) -** Set SPI I/O to Standard mode.
Commands are sent in Standard transfer mode wherein **MISO** is an input to the FPGA and **MOSI**, **IO2**, and **IO3** are outputs.

- **idle ( -- ) -** Disable Device.  
Deasserting the Chip Select pin will put the flash memory chip in a quiescent power mode. But it is also needed for other commands, such as the sector erase commands where the chip must be deselected immediately after sending the command. The SPI transfer mode is also set to Standard.  

- **spixbit ( x -- y ) -** Read/Write Data Bit.  
The high order bit of the 16-bit word **x** is shifted out to the SPI data out pin and then the SPI data in pin is shifted into the low bit of **x** returning **y**.  

- **spix ( outdata -- indata ) -** Read/Write Data Byte.  
Used follow a Read or Write command. Data on the stack is written to flash following a Write command but is ignored following a Read command. Data returned by this function is valid following a Read command but is an undefined value following a Write command.   

- **\>spi ( -- byte ) -** Read Data Byte.  
Use `spix` to return a read data byte (zero if used with a write command).  

- **spi\> ( byte -- ) -** Write Data Byte.  
A zero byte is shifted out while the read data is shifted in and the data byte is returned on the stack.  

- **waitspi ( -- ) -** Wait For Device Ready.  
Checking for the Write In Progress flag to go low is needed immediately after sending an Erase Sector command.  

- **spiwe ( -- ) -** Enable Writing To Device.  
On application of power, the flash device will not allow erase or write commands until it is unlocked. There is a corresponding disable command. though it is currently not needed. In addition, there are sector write disable commands that could be added to protect the bitstream section of the chip from accidental overwrite.  

- **spi_powerdn ( -- ) -** Power Down The Flash Device.
The standby current can drop from 10uA to 1uA in power down mode. The drawback is the delay needed to power up the device before it will respond to commands. Best used when the HP-71B CPU issues the Shutdown command.

- **spi_powerup ( -- ) -** Restore From Power Down Sleep.
Brings the flash device back to the operational state, ready to accept commands.  


## API Support Functions
The words in this section are used only by the API function words. These functions do not implement any address range restrictions.

- **sect2addr ( sector16k -- double_rom_addr ) -** Sector Number to 24-bit Address.  
Convert a 10-bit sector address word ( 0~1023 ) into a 24-bit double-word address suitable for an SPI Read or Write command.  

- **addr2sect ( double_rom_addr -- sector16k ) -** 24-bit Address to Sector Number.  
Convert a double-word containing a 24-bit flash address to a sector number. The lower 14 bits are truncated.  

- **addr2spi ( double_rom_addr -- ) -** Output 24-bit Address to SPI.  
The 24-bit address in a double-word is written as three bytes to the SPI interface, high byte first. Bytes are written high bit first.  

- **spiread ( double_rom_addr -- ) -** Address A General Memory Location.  
This function can be used to read from flash memory starting at any location. Subsequent >spi commands will return each sequential data byte after a first dead read cycle.  

- **spiread16k ( sector16k -- ) -** Address Start Of Memory Sector.  
This function is passed a 16 KB sector number in the range of 0 to 1023 for the 16 MB SPI flash memory device. Sets up a read operation like the `spiread` command.  


## SPI Utility Functions

- **spiflush ( Nbytes -- ) -** Read And Discard Memory Bytes.  
This function is provided as a means of advancing the read memory address without having to reissue an spiread/spiread16k command.  

- **spidump ( Nbytes -- ) -** Read And Display Memory Bytes.  
This function is provided as a means of examining the read memory address content.  

- **numsectors ( -- sector16k_count ) -** Return Number Of Memory Sectors.  
This function uses the second byte from a Manufacturer ID, returned by the Read JEDEC ID command, to determine flash memory capacity. Useful in place of a hard coded size value.



## API Functions
These words, developed for the MultiMod II, are the formal interface to flash memory. User programs can use them to access or update image or data files. These words prevent write access to bitstream data in section one of flash memory. They **do not** limit Forth dictionary images to section two of flash memory.

- **erase4k ( sector4k -- ) -** Erase A 4 KB Sector.  
This command uses a standard JEDEC command to erase the smallest size sector in flash memory. Rather than pass a full 24 bit memory address, the function is given a sector number from 0 to 4095. This insures the address sent in the command is at the beginning of a given sector.  

- **erase ( sector16k -- ) -** Erase A 16 KB Page.  
Since the MultiMod II treats flash memory as a sequence of 16 KB blocks, this command is used to erase one such block in preparation for writing. The function takes a block number from 0 to 1023.  

- **load ( sector16k -- ) -** Load Forth Dictionary.  
The Forth CPU dictionary is in the 15 KB Dual-Port Memory of the FPGA. This command will overwrite that internal memory with the content of a specified 16 KB block in flash memory. Input is the range 0 to 1023.  

- **save ( sector16k -- ) -** Save Forth Dictionary.  
The content of the Forth CPU dictionary is written to a specified 16 KB block location in flash memory. The state of the Forth CPU can thus be restored using this command, allowing different Forth configurations for different tasks.  

- **hp71b -** Mark Top of Fixed Dictionary Words.  
The words defined in this and the constants file are the basis of all other code accessing the SPI flash memory. This marks the root of all other versions of the Forth dictionary.  


