# Flash File System Directory
A directory consists of two 16KB pages in flash - the first holding the directory itself and the second as a scratch block used when a pack operation occurs. Words relating to creating and maintaining an image directory are defined as follows.

## Constants and Variables
These variables are defined for the Forth directory and HP-71B ROM/IRAM directory. They are initialized to the constant values defined in the *ramrom* Forth file.

- **forthdir (variable) - ** Forth Directory Location.  
Initialized to the constant value `frtstart`. Use of the variable allows the directory to be relocated if desired.  

- **romdir (variable) - ** HP-71B Image Directory Location.  
Initialized to the constant value `romstart`. Use of the variable allows the directory to be relocated if desired.  

The value of the first byte in a 16-byte directory entry indicates the type of the entry.

- **Empty (constant) -** Unaltered Entry.  
All directory entries except the first are initially empty and their associated bytes are in the erased state.  

- **Valid (constant) -** Entry Referencing An Image.  
A directory entry the specifies the name, location, and type of image to which it points.  

- **Reclaim (constant) -** Entry Marked For Deletion.  
An entry marked Reclaim has been deleted and its entry space and image are ready to be reclaimed by the packing process.  

The second byte of a directory entry indicates both the type and size of the entry's image. The upper nibble of the byte indicates the image type, and the lower nibble indicates the number of 16KB blocks in the image (1~15). The image types are:

- **IRAM, ROM, HARD, TAKEOVER (constants) -** HP71B Image Types.  
ROM and IRAM images are relocatable. A HARD ROM has a starting address of $E0000 while a TAKEOVER ROM begins at address 0.  

- **DIRSIZE (constant) -** Internal Use Directory Entry.  
This is the first entry in a directory and indicates how many image blocks follow the two block directory itself. **Note that a future extension could define an entry that points to another directory (subdirectory) within the current directory.**  

The third and fourth bytes of a directory entry hold the 10-bit `sector16k` value indicating where the image is located relative to the directory itself. This value is a offset (0~DIRSIZE-1) from the first 16KB block following the directory.

The remaining 12 bytes of the directory entry give the name of the directory image in the form of the string length followed by 1 to 11 ASCII characters.

## Directory Support Words
These words support access to and creation/modification of directory entries. Commands that read or write entry values assume the read or write command pointer is positioned to the correct entry and byte offset within the entry.

These words accept the `sector16k` 16KB sector where a directory starts. The words are generalized in order to process either a Forth or HP-71B directory, but can also be used for any user defined directory.

- **writeloc ( sector16k -- ) -** Write Image Block Location.  
The 10-bit `sector16k` value is written to the third anf fourth bytes of a directory entry, low byte first. The value is from zero to the directory size minus one. The 16KB image blocks following the two block directory are numbered starting with zero. The flash Read command pointer must be positioned to the fifth byte of a directory entry.  

- **readloc ( -- sector16k ) -** Read Image Block Location.  
With the flash Read command pointer positioned to the third byte of an entry, this command will read two bytes and return the `sector16k` image location value of the entry.  

- **writestr ( string -- ) -** Write Name Of Entry Image.  
Similar to the **writeloc** words, this will write a string length byte followed by string characters. A string longer than 11 characters will be truncated. The flash Write command pointer must be positioned to the fifth byte of the directory entry.  

- **printstr ( -- ) -** Print Name Of Entry Image.  
This will write a string to the console when the flash read command pointer is positioned to the fifth byte of a directory entry.  

- **empty_entry ( sector16k -- entry# ) -** Return First Empty Entry Number.  
Scan the entries in a directory until the first empty entry is found. Return its entry number.  

- **entry_addr ( entry# sector16k -- double_rom_addr ) -**Flash Address Of Entry.  
Given the 16KB location of a directory and a desired entry number, this will return the 24-bit flash address of the directory entry.  

- **goto_entry ( entry# sector16k -- ) -** Set Read Pointer To Entry.  
Given the 16KB location of a directory and a desired entry number, this will issue a flash Read command to position the Read command pointer to the start of the specified directory entry.  

- **mark_reclaim ( entry# sector16k -- ) -** Mark Entry For Deletion.  
Given the 16KB location of a directory and a desired entry number, this will mark the entry as Reclaimable. The entry and its associated image are marked for deletion by the **pack** command.  


## Directory Commands
The following commands generalize the operations on a directory. Normally the Forth directory image directory and the HP-71B ROM/IRAM image directory are specified using the `fthstart` and `romstart` constants defined in the *soft-spi.fs* file. Other directories can be created and manipulated if so desired.

- **dir_init ( nblocks sector16k -- ) - ** Initialize Directory.  
Erase the two blocks making up a directory. The `sector16k` value is either `fthstart` or `romstart`. An initial first entry is added indicating the number of image blocks reserved following the directory itself.  

- **dir_size ( sector16k -- nblocks ) - ** Return Directory Size.  
Return the number of image blocks allocated to the specified directory. The first directory entry is of type DIRSIZE and holds the number of allocated blocks, stored in the name field.  

---

- **dir_find ( name sector16k -- entry# ) -** Find Named Directory Entry.  
Given a directory address `sector16k` and a directory image string name, return the `entry#` location in the directory. If the name is not found then an `entry#` value of -1 is returned.  

- **dir_insert ( name type.size sector16k -- block ) -** Insert Directory Entry.  
Given a directory address `sector16k`, a directory image string name, and a `type.size` value defining the image associated with the entry, insert an entry in the directory and return the block where the image should be stored. The caller is responsible for insuring the name does not already exist.  

- **dir_drop ( name sector16k -- ) -** Mark Directory Entry Reclaimable.  
Given a directory address `sector16k` and a directory image string name, mark the directory entry as invalid and reclaimable.  

---

- **dir_free ( sector16k -- number ) - ** Available Directory Image Blocks.  
Return the number of remaining unused image blocks in the `sector16k`  directory. A zero value indicates the directory needs to be packed.  

- **dir_list ( sector16k -- ) - ** List directory entries.  
List the valid entries in a directory. The `sector16k` value is normally either `fthstart` or `romstart`.  

- **dir_pack ( sector16k -- ) - ** Pack directory.  
The `sector16k` value is either `fthstart` or `romstart`. This command will pack both the directory and the image storage associated with the directory.  

