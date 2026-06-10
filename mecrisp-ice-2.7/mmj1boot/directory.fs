\ #######   HP-71B  ###########################################
\ Words to support directories in flash

\ A directory consists of two 16KB sectors in flash - the first holding
\ the directory itself and the second as a scratch block used when a
\ pack operation occurs. The 16KB directory sector contains 1024 entries
\ of 16 bytes each. Following the two 16KB sectors are the 16KB sectors
\ holding Forth dictionary images or HP-71B ROM/IRAM images.

\ Implementation Note:
\ Several routines involving access to the name string stored in an
\ directory entry require the string access functions to flush the
\ training bytes from the string field in order to keep the read
\ command address pointer on a directory entry boundary. This
\ requirement could be discarded if the goto_entry in conjunction
\ with the loop index to set the read pointer to each entry starting
\ point.

\ ---------------------------------------------------------------------
\ ######   Define variables and constants

\ The starting point for the Forth dictionary images and for the HP-71B
\ ROM/IRAM images are both defined as variables so they can be modified
\ by the owner.

\ Forth dictionary images begin at the 1MB address, hex $100000, which
\ would be 16K block #64. The first two 16K blocks are dedicated to the
\ directory for the images.
frtstart variable forthdir    \ Initial value taken from stack

\ HP-71B dictionary images begin at the 2MB address, hex $200000, which
\ would be block #128. The first two 16K blocks are dedicated to the
\ directory for the images.
romstart variable romdir    \ Initial value taken from stack

\ variable fth_dtop     \ Next entry number in Forth directory (0~1023)
\ variable rom_dtop     \ Next entry number in HP-71B directory (0~1023)


\ Types of entry in the first entry byte
$FF constant Empty
$F0 constant Valid
$00 constant Reclaim

\ Types of HP-71B images in upper nibble of the second entry byte
\ Lower nibble contains image length in 16KB sectors (1~15, 0 reserved)
$00 constant IRAM
$10 constant ROM
$20 constant HARD
$30 constant TAKEOVER
$80 constant DIRSIZE

\ Third and forth bytes of entry contain offset from dictionary end
\ to image (0~1023)



\ ---------------------------------------------------------------------
\ ######   Useful support words

: writeloc ( sector16k -- )
    \ Write a 10-bit `sector16k` value as two bytes to flash write
    \ pointer location (Assumes write command active)
    dup $FF and >spi     \ Low byte sector16k location
    8 rshift 3 and >spi   \ High byte sector16k location
;

: readloc ( -- sector16k )
    \ Read a 10-bit sector16k value as two bytes from flash read
    \ pointer location (Assumes read command active)
    spi>         \ Low byte sector16k location
    spi>          \ High byte sector16k location
    8 lshift or    \ Assemble bytes to 10-bit sector16k value
;

: writestr ( string -- )
    \ Take a `string` and write it to flash, truncating it as
    \ necessary. A string consists of a string length integer (TOS)
    \ and the address of the string (next on stack).
    \ Assumes write command active.
    \ Needs to truncate strings longer than 11 characters!
    dup 11 > if
        drop     \ Discard string length > 11
        11        \ Truncate name to fit space in directory entry
    then
    dup >spi        \ Write string length
    0 do
	dup c@ >spi   \ Write string character
	1+             \ Advance pointer
    loop
    drop                 \ Lose the string addr
;

: printstr ( -- )
    \ Read string from flash, starting with the string length byte,
    \ and emit each character to the console.
    \ Assumes read command active.
    spi>          \ String length
    dup            \ Keep copy
    0 do
	spi> emit    \ Read and print string character
    loop
    12 swap - 1-       \ Remaining bytes in directory entry
    spiflush            \ Move to next entry
;

: empty_entry ( sector16k -- entry# )
    \ Return the first empty entry associated with the `sector16k`
    \ directory. This involves scanning each 16 byte directory entry
    \ starting at the beginning until an erased entry is found,
    \ or the end of the directory is found.
    \ A return value of 1024 indicates a full directory.
    spiread16k      \ Set read pointer to beginning of directory
    spi> drop        \ First directory entry is DIRSIZE
    0                 \ Loop count
    begin
	15 spiflush     \ Advance to next directory entry
	1+               \ Increment counter
	dup 1024 =        \ End of directory?
	spi> Empty =       \ Found an empty directory entry?
	or
    until
;

: entry_addr ( entry# sector16k -- double_rom_addr )
    \ Calculate the SPI flash address of a directory entry given the
    \ `sector16k` address of the directory and the entry number,
    \ range 0~1023. Note entries are 16 bytes in length.
    \ This function can be used as a more efficient way of setting
    \ the SPI flash write address in place of goto_entry.
    sect2addr     \ Convert sector16k to double_rom_addr
    rot 4 lshift   \ Retrieve entry#, multiply by 16
    0 d+            \ Convert 16*entry# to double, add to directory addr
;

: goto_entry ( entry# sector16k -- )
    \ Set up a read operation at the start of a specified directory `entry#`
    \ For the directory at sector number `sector16k`
    entry_addr            \ Compute 3-byte flash address
    spiread                \ Issue read command
;

: mark_reclaim ( entry# sector16k -- )
    \ Given the base address `sector16k` of a dictionary and an entry
    \ number, mark the entry as Reclaim and no longer Valid.
    entry_addr         \ Compute address of directory entry
    spiwrite            \ Switch to write mode
    Reclaim >spi         \ Clear `Valid` bit
    idle                  \ Must disable select after last byte
    waitspi                \ Wait for write to finish
;

: free_image ( sector16k -- sector16k )
    \ For a `sector16k` dictionary, find the next free image block
    \ within the dictionary. Use the last non empty dictionary
    \ location plus its image length to find next free block.
    dup empty_entry      \ Last used dictionary entry
    1- swap               \ ( entry# sector16k -- )
    goto_entry             \ Flash address of directory entry
    spi> drop                \ Skip entry type
    spi> $F and               \ Image Type.Size, mask off Size
    readloc +                  \ Read pointer to last allocated block
;

: entry_type ( entry# sector16k -- type.size )
    goto_entry     \ Set read command pointer to directory entry
    spi> drop       \ Discard entry type
    spi>             \ Return type.size byte
;

: entry_image ( entry# sector16k -- block# )
    \ Given a directory `sector16k` and a directory entry `entry#`,
    \ Return the image `block#` where the entry image starting
    \ address is. The actual `sector16k` address of the image would
    \ be the directory address + block# + 2.
    goto_entry     \ Set read command pointer to directory entry
    2 spiflush      \ Skip entry type and image type.size bytes
    readloc          \ Read two byte block number
;

: image_addr ( block# sector16k -- sector16k )
    \ Given directory location `sector16k` and the block number
    \ where an image is stored in the directory, return the
    \ absolute sector address number of the image.
    + 2 +
;



\ ---------------------------------------------------------------------
\ ######   Commands associated with directory initialization

: dir_init ( nblocks sector16k -- )
    \ Initialize the two 16K blocks of a directory. The argument is
    \ the starting block number of the Forth or HP-71B image area,
    \ either fthstart or romstart, and the number of 16K blocks in
    \ the directory collection.
    dup erase dup 1 + erase  \ First two blocks in collection
    sect2addr spiwrite        \ Address the first directory entry
    Valid >spi DIRSIZE >spi    \ Valid entry, type SIZE
    0                           \ First image block offset
    writeloc                     \ Output 0 image block number
    writeloc                      \ Output nblocks size to name field
    idle                           \ Must disable select after last byte
    waitspi                         \ Wait for write to finish
;

: dir_size ( sector16k -- nblocks )
    \ Return the number of 16k image sectors allocated to a directory.
    \ Placed alongside dir_init due to where the size value is stored.
    spiread16k        \ Read command to start of directory
    spi> drop          \ Entry type (Valid, Reclaim, ...)
    spi> drop           \ Image Type/Size
    readloc drop         \ First image block
    readloc idle          \ Stored directory size in name field
;



\ ---------------------------------------------------------------------
\ ######   Commands associated with directory entries and images

: strcmp ( string -- flag )
    \ Compare a string to a string in flash. The read command pointer
    \ addresses the first character of the string in flash.
    \ Return True (1) or False (0) for string match.
    1 rot rot       \ Assume success ( returnval string -- )
    0 ?do            \ loop string length times
        dup c@        \ Fetch string character
        swap 1+ swap   \ Increment string pointer
	spi> =          \ String match? ( flag addr flag -- )
	rot and swap     \ AND result with return value ( flag addr -- )
    loop
    drop                   \ Drop pointer
;

: cmpname ( name -- flag )
    \ Compares the name on the stack to the name in a directory entry
    \ that the read command pointer is pointing to.
    \ Returns true/false flag
    \ 2drop
    \ 15 spiflush     \ Advance to next directory entry
    \ 0
    dup spi> <> if       \ Are string lengths different?
        2drop             \ Drop string
	11 spiflush        \ Skip to end of entry
	0                   \ Return fail
    else
	dup rot rot           \ Save string length on stack
	strcmp swap            \ String match? ( flag length -- )
	12 swap - 1-            \ Remaining string characters
	spiflush                 \ Skip, return string match result
    then
;

: dir_find ( name sector16k -- entry# )
    \ Given a dictionary address `sector16k` and a dictionary image
    \ string name, return the `sector16k` location in flash. The return
    \ value is used by the `load` command for Forth dictionaries or one
    \ of the `ram2rom` commands for HP-71B images.
    \ Failure to find name will return an invalid value of 1024.
    spiread16k      \ Set read pointer to beginning of directory
    16 spiflush      \ Skip DIRSIZE entry
    1024 0            \ Loop count, iterates 0..1023
    ?do  
	spi> Empty =    \ Empty directory entry?
	if
            2drop idle    \ String
            1024 leave     \ Fail
	then                \ Exit loop
	3 spiflush           \ Discard Type.Size, Blocks bytes
	2dup cmpname if       \ True if name found
	    2drop idle         \ String
            i leave             \ Found match, return entry#
        then
    loop
;

: dir_insert ( string type.size sector16k -- block )
    \ Given the directory sector number `sector16k` read
    \ the next empty directory entry number and the next
    \ free memory location for an image, then write the
    \ directory entry value to the directory.
    \ Return value: Where to store image
    dup free_image    \ Find next flash block for image
    swap               \ string type-size block sector16k
    dup empty_entry     \ Next empty entry
    swap                 \ string type-size block entry# sector16k
    entry_addr spiwrite   \ Issue write command at start of entry
    swap                   \ string block type-size
    Valid >spi              \ Mark entry as `Valid`
    >spi                     \ Type/Size byte
    dup >r                    \ Save block number
    writeloc                   \ Two byte image address
    writestr                    \ Entry name
    idle waitspi                 \ End of write sequence
    r>                            \ Return image block number
;





\ ---------------------------------------------------------------------
\ ######   Commands associated with directory status

: dir_free ( sector16k -- number )
    \ Return the number of free image blocks in the given Forth or HP-71B
    \ directory. A zero value indicates the directory needs to be packed.
    dup dir_size     \ Number of image blocks
    free_image        \ Next empty image block
    -                  \ Should be >= 0
;

: prtype ( Type.Size -- )
    \ Print out the image type and size
    dup $F0 and case
        IRAM of ."     IRAM" endof
        ROM of ."      ROM" endof
        HARD of ."     HARD" endof
	TAKEOVER of ." TAKEOVER" endof
	."  UNKNOWN"
    endcase
    $F and 16 * 6 .r ." K "
;

: prblock ( block -- )
    \ Print out block size
    6 .r $20 emit $20 emit   \ Print block number in an 8-wide field
;


: dir_list ( sector16k -- )
    \ List the valid entries in a directory. The `sector16k` value is
    \ either `forthdir` or `hp71dir`.
    
    ."   Type    Size    Block    Name" cr
    
    spiread16k        \ Set read pointer to beginning of directory
    16 spiflush        \ First directory entry is DIRSIZE
    1024 0              \ Loop count, iterate 0..1023
    ?do
        spi>             \ Entry type
        dup Valid = if     \ If Valid entry, print it out
	    spi> prtype     \ Print object type
	    readloc prblock  \ Print block location
	    printstr cr       \ Print name
	else
	    15 spiflush         \ Skip entry, get next entry type
	then                     \ ( count entry-type -- )
	Empty = if                \ Found an empty directory entry?
            idle leave
        then
    loop
    idle
;

: dir_copy ( from16k to16k -- )
    \ Copy all of the directory entries from one directory block
    \ to another.
    \ cr ." to " . ."   from " .
;

: dir_resize ( nblocks sector16k -- new-blocks )
    \ Resize a directory to a larger or smaller number of image
    \ blocks. If reducing the size, the size can't be smaller than
    \ the current number of existing image blocks.
    \ The return value will be the same as the requested value if
    \ successful, or equal to the lowest valid value.
    \ The scratch block is erased when a directory is created, so
    \ no need to erase it again.
    ." Not Yet Implemented"
    \ 2dup            \ ( nblocks sector16k nblocks sector16k -- )
    \ free_image       \ ( nblocks sector16k nblocks free# -- )
    \  < if             \ True if nblocks < free#
    \     swap drop      \ ( sector16k -- )
    \     dup free_image  \ ( sector16k free# -- )
    \     swap             \ ( free# sector16k -- )
    \  then
    \ 2dup 1+ dir_init      \ Scratch is new directory
    \ swap drop dup 1+       \ ( sector16k sector16k+1 -- )
    \ 2dup dir_copy           \ Copy entries from sector16k to sector16k+1
    \ swap dup erase16k        \ Erase old directory block
    \ 2dup swap dir_copy        \ Copy new directory back to old directory
    \ swap drop                  \ ( sector16k sector16k -- )
    \ dup 1+ erase16k             \ Erase scratch block
    \ swap drop free_image         \ New directory size
;

: dir_pack ( sector16k -- )
    \ The `sector16k` value is either `fthstart` or `romstart`.
    \ This command will pack both the directory and the image
    \ storage associated with the directory.
    ." Not Yet Implemented"
;
