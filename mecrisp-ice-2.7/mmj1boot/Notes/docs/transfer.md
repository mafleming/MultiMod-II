# File Transfer Support
These Forth words support the transfer of ROM/IRAM images to and from the MultiMod II as well as transferring FPGA bitstream images to the MultiMod II flash storage. The latter capability is used to update the production configuration of the MultiMod II as well as to install optional configurations.

File transfer is done using the Kermit protocol, which is supported by most terminal emulators.

## Forth Dictionary Images
Forth dictionary images can be loaded or saved via the Forth dictionary using the following commands. Note that Forth words in a text file can be simply sent via a terminal emulator to the Forth console rather than typing in definitions by hand.

- **forthsave ( name -- ) -** Save Forth Dictionary Image.  
If `name` doesn't exist in the dictionary it is created, otherwise the current image updates the image stored in flash.  

- **forthload ( name -- ) -** Load Forth Dictionary By Name.  
The current Forth dictionary is replaced by the `name` image in flash. If that name doesn't exist in the dictionary, then no change occurs.  

- **forthlist ( -- ) -** List Forth Dictionary Entries.  
This uses the `dir_list` command from the `directory.fs` set of words to list entries in the Forth directory.  


## HP-71B ROM/IROM Images
ROM image `.bin` files can be sent using the Kermit protocol. ROM/IRAM images in flash can be downloaded and automatically given the `.bin` extension.

- **romverify ( ram# name -- ram_addr flag ) -** Verify Flash Image By Name.  
Given a name in the ROM directory and the starting point of an image in SPRAM, verify the content in SPRAM matches that of the image in SPRAM. The size of the image in the directory entry is used to determine the length of the image in 16KB sectors. The last memory address examined is returned along with a Pass/Fail flag. If the images don't match the **ram_addr** indicates the point of first mismatch.  

- **writeflash ( name type -- ) -** Upload ROM/IROM Image To Flash.  
Uploads a file and stores the file in flash, adding *name* to the ROM/IROM directory.  

- **readflash ( name -- ) -** Download Flash Image By Name.  
Downloads the *name* ROM/IROM image using the Kermit receive protocol.  

- **romlist ( -- ) -** List HP-71B Directory Entries.  
This uses the `dir_list` command from the `directory.fs` set of words to list ROM/IRAM image entries in the HP-71B directory.


## FPGA Bitstream Images
There are four locations, slots 0 through 3, in the first megabyte of flash memory that store bitstream images. The first, slot 0, is the bootloader and cannot be erased or altered. The second, slot 1, is the MultiMod II production configuration that is loaded while the MultiMod II is in the HP-71B card reader cavity. Slots 2 and 3 are for alternate configurations that can be loaded via a warm boot.

- **bitstream ( slot -- ) -** Transfer FPGA Bitstream.
<br>
Uploads a bitstream file to flash and loads it as bitstream number `slot`. The slot number does not include 0, the bootloader bitstream image itself.
<br>