# ROM And IRAM Module Management
These commands provide a basic means of inserting logical ROM or RAM modules into the HP-71B address space as though they were real physical modules. Module memory is taken from the FPGA SPRAM which is initialized, in the case of ROM or IRAM modules, from the 16MB flash memory. Commands also allow IRAM modules to be created and saved to flash.

## SPRAM Organization

Physically, the 128KB of SPRAM in the FPGA is arranged as four 16KB by 16-bit memories. The j1a Forth CPU addresses the memory as 64KB of 16-bit words, the default word size of the Forth CPU. The SPRAM is not addressed directly within the CPU memory address space. Rather, it is accessed via an address register and data register within the CPU's I/O address space.

## Bus Interface
There is a register per bank in the j1a I/O address space. The register stores both information used by Forth code to track modules accessible to the HP-71B and control/status bits that configure the HP-71B bus interface logic. The configuration of each register is as follows.

```
Register Write
+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
|15 14 13 12 11 10 09 08 07 06 05 04 03 02 01 00|
+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
| U| E| Type| xxx |            Image            |
+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+

Register Read
+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
|15 14 13 12 11 10 09 08 07 06 05 04 03 02 01 00|
+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
| U| E| Type| S|xx|            Image            |
+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+

U     - Flag indicating bank is in use
E     - Flag indicating bank continues/ends chain of banks
S     - Bank configuration status
Image - Directory entry number of ROM/IRAM image
```

A bank with a zero for the `U` in-use flag does not appear in the HP-71B I/O bus address space. When a bank is loaded with an image content, the `U` flag is set, the directory entry number of the image is stored in the `Image` bits, and the `Type` bits are the same as the directroy entry type bits. These are used to indicate the bank contains a ROM, IRAM, hard ROM or takeover ROM image. The `E` flag is used to endicate whether the bank is a continuation or the end of a chain of banks. A 16KB image will set the flag, while larger images will clear the flag for all occupied banks except the last bank in the chain.

## Constants And Variables

- **ibank (variable) -** Starting Bank For Image.  
- **isize (variable) -** Number Of Banks Image Occupies.  
- **itype (variable) -** Image Type.  
- **ientry (variable) -** Directory Entry Of Image.  
- **errno (variable) -**Identifier Of Function Error.  

## Support Functions
Low level support functions dealing with the interface between the j1a Forth CPU and the HP-71B bus support logic is described here.

- **bank@ ( bank -- value ) -** Read Bank Configuration.  
The `bank` value is between 0 and 7,  and the supplied value is masked to fit that range. The return value is described in the Bus Interface section.  

- **bank! ( value bank -- ) -** Write Bank Configuration.  
The `bank` value is between 0 and 7,  and the supplied value is masked to fit that range. The `value` parameter is an integer and is described in the Bus Interface section.  

- **willfit ( -- flag ) -** Image Will Fit In SPRAM.  
Based on variables `ibank` and `isize`, return true if the bank(s) needed are in the range of banks 0 through 7.  

- **isempty ( -- flag ) -** Image Banks Are Not In Use.  
Based on variables `ibank` and `isize`, return true if the bank(s) needed are not in use by another image.  

- **isname ( -- flag ) -** Valid Dictionary Name.  
Based on the variable `ientry` insure the name is in the dictionary.  

- **settypsz ( -- ) -** Set Type And Size Variables From Entry#.  
Given a valid dictionary entry# in `ientry` retrieve the type and size of the entry image in variables `itype` and `isize`.

- **isbanked ( -- flag ) -** Image Name Found In Banks.  
Return true if the value of variable `ientry` is found in one or more banks.  

- **setbanks ( -- ) -** Configure Banks Used By Image.  
Using the `ientry`, `itype`, `isize`, and `ibank` variables, configure the SPRAM bank registers used by a validated image name.  

## Storage Support Functions
The following functions are used by the set of user commands to carry out detailed processing. They use the variables defined at the start of the Forth file as parameters.

- **rom2banks ( -- ) -** Copy Flash Image To SPRAM.  
Use the `ientry`, `ibank`, and `isize` variable to copy an image in flash to the appropriate bank(s) in SPRAM.  

- **banks2rom ( -- ) -** Copy SPRAM To Flash.  
Use the `ientry`, `ibank`, and `isize` variable to copy an image in SPRAM bank(s) to the appropriate flash location.  

- **mkiram ( name -- ) -** Create IRAM And Save To Flash.  
This will convert a RAM in SPRAM defined by the `ibank` and `isize` variables to an IRAM and then save the new IRAM to the HP-71B image directory.  

## Commands
These are the commands that are used by an HP-71B owner to control the configuration of the MultiMod II. The commands allow the owner to plug or unplug ROM or IRAM modules by name, create new IRAM modules and give them a name, and plug in ordinary RAM modules. Named modules can be unplugged or saved at will.

- **listall ( -- ) -** List Plugged In Modules. 
A list of each occupied bank and the name of the module that occupies the bank is listed to the console.  

- **newram ( size bank -- flag ) -** Plug In An Empty RAM Module.  
The name is a ROM or IRAM image stored in the HP-71B directory. The `size` argument is the number of banks the image occupies, the `bank` specified plus any subsequent banks as needed. The return value `flag` indicates success with a true value and failure with a false value.  

- **newiram ( name size bank -- flag ) -** Plug In A New Empty IRAM.  
The new `name` IRAM image of `size` banks is created in the HP-71B directory and plugged into SPRAM starting at `bank` bank. The image occupies the `bank` specified plus any subsequent banks as needed according to the image size. The return value `flag` indicates success with a true value and failure with a false value. A false value would indicate the name already exists in the directory or the directory is full.  

- **save ( -- flag ) -** Save An IRAM Image To Flash.  
Using the variable `ientry` verify the dictionary entry# is in one or more banks. The image content itself is written back to flash. The return value `flag` indicates success with a true value and failure with a false value.  

- **savename ( name -- flag ) -** Save An IRAM Image To Flash.  
The name is a ROM or IRAM image stored in the HP-71B directory. A lookup of the name is done for the ROM directory and if found and if present in the banks, the image content is written back to flash. The return value `flag` indicates success with a true value and failure with a false value.  

- **plug ( bank name -- flag ) -** Plug In A Named Module Image.  
The name is a ROM or IRAM image stored in the HP-71B directory. The image occupies the `bank` specified plus any subsequent banks as needed according to the image size. The return value `flag` indicates success with a true value and failure with a false value.  

- **unplug ( name -- flag ) -** Unplug A Named Module Image.  
The name is a ROM or IRAM image stored in the HP-71B directory. The image occupying one or more banks has those banks marked as unoccupied. The return value `flag` indicates success with a true value and failure with a false value. *Should an IRAM image automatically be saved?*  

- **unplugall ( -- ) -** Unplug All Module Images.  
All of the eight SPRAM banks are marked as unoccupied.  

- **clrbank ( bank# -- ) -**Mark Specified Bank As Not In Use.  
Use with caution to clear a bank configuration.