\ #######   HP-71B  ###########################################
\ Words to support management of the HP-71B I/O bus control


\ Implementation Note:
\ There is a register per bank in the j1a I/O address space. The register
\ stores both information used by Forth code to track modules accessible
\ to the HP-71B and control/status bits that configure the HP-71B bus
\ interface logic. The configuration of each register is as follows.

\ Register Write
\ +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
\ |15 14 13 12 11 10 09 08 07 06 05 04 03 02 01 00|
\ +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
\ | U| E| Type| xxx |            Image            |
\ +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
\
\ Register Read
\ +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
\ |15 14 13 12 11 10 09 08 07 06 05 04 03 02 01 00|
\ +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
\ | U| E| Type| S|xx|            Image            |
\ +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
\
\ where
\
\ U     - Flag indicating bank is in use
\ E     - Flag indicating bank continues/ends chain of banks
\ S     - Bank configuration status
\ Image - Directory entry number of ROM/IRAM image

\ ---------------------------------------------------------------------
\ ######   Define variables and constants

\ These are used as substitute for actual hardware registers when
\ developing code
\ here 16 allot variable bank   \ Allocate an eight word array
\ Using separate variables instead of an array
0 variable bank0     \ Bank 0
0 variable bank1     \ Bank 1
0 variable bank2     \ Bank 2
0 variable bank3     \ Bank 3
0 variable bank4     \ Bank 4
0 variable bank5     \ Bank 5
0 variable bank6     \ Bank 6
0 variable bank7     \ Bank 7

\ Variables storing image information
0 variable ibank     \ Range 0~7
0 variable isize     \ Range $1~F
0 variable itype     \ Range $00~F0
0 variable ientry    \ Range $000~3FF

\ Variable and constants for error management
0 variable errno

0 constant isok
1 constant nofit
2 constant nofree
3 constant noname
4 constant nobank
5 constant nouniq

\ ---------------------------------------------------------------------
\ ######   Useful support words

: bank@ ( banknum -- bankval )
    \ Read the value stored in bank register banknum. Note the value is
    \ 0 to 7, and the value is masked to insure it is in the correct range.
    7 and case               \ Mask banknum, the select bank variable
        0 of bank0 @ endof
        1 of bank1 @ endof
        2 of bank2 @ endof
        3 of bank3 @ endof
        4 of bank4 @ endof
        5 of bank5 @ endof
        6 of bank6 @ endof
        7 of bank7 @ endof
    endcase
;

: bankreg! ( bankval banknum -- )
    \ Write settings to a hardware bank control register
    \ The four bit value is: InUse, ChainEnd, Type[1:0]
    7 and                \ Confine to range 0~7
    swap 12 rshift swap   \ Shift bits 15:12 to 3:0
    $0400 or io!           \ Form IO address 040x and write
;

: bank! ( bankval banknum -- )
    \ Write the supplied value of bankval to bank register banknum. Note
    \ the correct value id 0 to 7 and the value is masked to insure the
    \ bank array is indexed within bounds.
    2dup bankreg!
    7 and case               \ Mask
        0 of bank0 ! endof
        1 of bank1 ! endof
        2 of bank2 ! endof
        3 of bank3 ! endof
        4 of bank4 ! endof
        5 of bank5 ! endof
        6 of bank6 ! endof
        7 of bank7 ! endof
    endcase
;

: willfit ( -- flag )
    \ Use variables ibank and isize to determine if a image will fit
    \ in SPRAM memory
    ibank @ isize @ + 1- 8 <  \ Can't go from 'bank' past bank 7
;

: isempty ( -- flag )
    \ Use variables ibank and isize to ensure target SPRAM banks
    \ are not already occupied
    0                 \ Accumulator
    isize @ ibank @ +  \  0 -- bank+size
    ibank @             \ 0 bank+size -- 0 bank+size bank
    ?do                  \ 0 bank+size bank -- 0
	i bank@ or        \ acc -- acc or bank_i
    loop                   \ Accumulate InUse flags
    $8000 and 0=            \ No bank is InUse?
;

: isname ( -- flag )
    \ Use variable ientry to determine if a name is valid
    ientry @  0 <>         \ Zero is not a valid entry #
    ientry @ 1024 <>        \ dir_find returns 1024 for not found
    and                      \ Valid entry is neither 0 or 1024
;

: settypsz ( -- )
    \ Set the itype and isize variables for an entry
    ientry @ romdir @   \ -- entry# sector16k
    entry_type dup       \ entry# sector16k -- type.size type.size
    $F0 and itype !       \ type.size type.size -- type.size
    $F and isize !         \ type.size --
;

: isbanked ( -- flag )
    \ A valid name's entry number is in banks
    false 8 0 ?do      \ 2.  -- flag
        i bank@         \ flag -- flag bank-config
        $03FF and        \ Mask off configuration name entry#
        ientry @          \ entry#
        = or               \ flag bank-entry# entry# -- flag
    loop                    \ flag -- flag
;


: setbanks ( -- )
    \ Use ibank isize, itype and ientry to set bank registers
    isize @ ibank @ +    \  -- bank+size
    ibank @               \ bank+size -- bank+size bank
    ?do                    \ Loop through each image bank
        $8000               \ InUse flag
        itype @ 8 lshift or  \ Image type
        ientry @ $3FF and or  \ Image entry, E flag clear
        i bank!                \ Set bank configuration
    loop
    ibank @ isize @ + 1-         \ Last bank in chain
    dup bank@ $4000 or            \ lastbank -- lastbank bankval
    swap bank!                     \ Set bank 'E' end flag
;


\ ---------------------------------------------------------------------
\ ######   Storage Support Functions

: rom2banks ( -- )
    \ Copy the flash content of a ROM entry to SPRAM
    ientry @ romdir @    \ -- entry# sector16k
    entry_image           \ entry# sector16k -- block#
    romdir @ image_addr    \ block# -- sector16k
    ibank @ isize @         \ sector16k -- sector16k bank size
    case                     \ bank sector16k size -- sector16k bank
        1 of rom2ram          \ sector16k bank --
        endof
        2 of rom32k2ram         \ sector16k bank --
        endof
        3 of 2dup rom2ram         \ sector16k bank -- sector16k bank
            1+ swap 1+ swap        \ sector16k bank -- sector16k+1 bank+1
            rom32k2ram              \ sector16k+1 bank+1 --
        endof
        4 of rom64k2ram               \ sector16k bank --
        endof
    endcase
;

: banks2rom ( sector16k -- )
    \ Copy content of bank(s) back to flash
    ibank @ swap isize @    \ sector16k -- bank sector16k size
    case                     \ bank sector16k size -- bank sector16k
        1 of ram2rom endof    \ bank sector16k --
        2 of ram32k2rom endof  \ bank sector16k --
        3 of 2dup ram2rom       \ bank sector16k -- bank sector16k
            1+ swap 1+ swap      \ bank sector16k -- bank+1 sector16k+1
            ram32k2rom            \ bank+1 sector16k+1 --
        endof
        4 of ram64k2rom endof       \ bank sector16k --
    endcase
;

: mkiram ( name -- )
    \ Convert a RAM into IRAM using the ibank isize variables
    \ Note: IRAM denoted by $B3DD $DDDE in first four
    \ nibbles. $DD $B3 $DE $DD
    ibank @ isize @ +     \ name -- name bank+size
    ibank @ ?do            \ name bank+size -- name bank+size bank
            i zeroram       \ Clear memory
    loop
    ibank @ $2000 *           \ name -- name ramaddr
    $B3DD over sram!           \ name ramaddr data ramaddr -- name ramaddr
    1+ $DDDE swap sram!         \ name data ramaddr+1 -- name
    2dup IRAM isize @ or         \ name -- name name type.size
    romdir @ dir_insert           \ name name type.size sector16k -- name block#
    romdir @ image_addr            \ name block# sector16k -- name sector16k
    banks2rom                       \ name sector16k -- name
    romdir @ dir_find ientry !       \ name --
    setbanks                          \ Update entry# in banks
;



\ ---------------------------------------------------------------------
\ ######   Commands

: listall ( -- )
    \ For each bank, do
    \ Print name, type
    cr ." Bank InUse End Type  Name" cr
    8 0 ?do
        i . ."     "       \ Bank number
        i bank@
        dup $8000 and
        0= if
            ." 0     "
        else
            ." 1     "
        then
        dup $4000 and
        0= if
            ." 0  "
        else
            ." 1  "
        then
	dup 6 rshift $F0 and
	over $8000 and 0= if 1 or then
            case
                IRAM of ." IRAM " endof
                ROM of ." ROM   " endof
                HARD of ." HARD " endof
                TAKEOVER of ." TAKE " endof
                ." ---- "
	    endcase
        $3FF and
        dup 0 <> if
             romdir @ goto_entry
	    4 spiflush printstr idle
        else
            drop
        then
        cr
    loop
;

: newram ( size bank -- flag )
    \ 1. Check that RAM will fit, return false if not
    \ 2. Insure bank(s) are empty, return false if not
    \ 3. Setup bank(s) configuration
    \ 4. Return true
    ibank ! isize !    \ Save arguments
    0 ientry !          \ Not a valid entry#, regular RAM
    IRAM itype !         \ Not correct but won't be backed to flash
    willfit if            \ 1. Will the new RAM fit memory?
	isempty if         \ 2. Insure banks are empty
	    setbanks        \ 3. Setup bank(s) configuration
            isok errno !
            true
        else
            nofree errno !
            false          \ No, banks not empty, return Fail
        then
    else
        nofit errno !
        false             \ No, won't fit, return Fail
    then
;

: newiram ( name size bank -- flag )
    \ 1. Insure name does not already exist
    \ 2. Check that image size will fit, return false if not
    \ 3. Insure bank(s) are empty, return false if not
    \ 4. Initialize bank(s) as an IRAM
    \ 5. Save bank(s) to flash as directory entry 'name'
    \ 6. Return true
    ibank ! isize !         \ name size bank -- name
    2dup romdir @ dir_find   \ name -- name entry#
    ientry !                  \ name entry# -- name
    isname not if              \ 1.  name -- name
        isize @ ibank @         \ name -- name size bank
        newram if                \ 1~4. name size bank -- name
	    mkiram                \ name --
	    isok errno !
            true
        else
            2drop                    \ name --
            nouniq errno !
            false
        then
    else
        2drop                  \ name --
        noname errno !
        false
    then
;

: save ( -- flag )
    \ 1. Check that name exists in bank(s), return false if not
    \ 2. Write bank(s) content back to flash
    \ 3. Return true
    
    isbanked if           \ 1.  --
        settypsz           \ Size value needed when writing back
        ientry @            \ -- entry#
        8 0 ?do              \ Find first bank
            dup               \ entry# -- entry# entry#
            i bank@ $3FF and   \ entry# entry# -- entry# entry# bank-entry#
            = if                \ entry# entry# bank-entry -- entry#
                i ibank !        \ Save first bank#
                leave             \ entry# -- entry#
            then
        loop
	romdir @ entry_image         \ entry# -- block#
	romdir @ image_addr           \ block# -- sector16k
        banks2rom                      \ sector16k --
        isok errno !
        true
    else
        nobank errno !
        false
    then
;

: savename ( name -- flag )
    \ Save an image by name.
    \ 1. Check that name exists, return false if not
    \ 2. Save
    romdir @     \ name -- name sector16k
    dir_find      \ name sector16k -- entry#
    ientry !       \ entry# --
    isname if       \ 1.
        save         \ 2.  -- flag
        isok errno !
    else
        noname errno !
        false
    then
;


: plug ( bank name -- flag )
    \ 1. Check that name exists, return false if not
    \ 2. Check that image will fit
    \ 3. Insure bank(s) are empty
    \ 4. Copy image to bank(s)
    \ 5. Setup bank(s) configuration
    \ 6. Return true.
    romdir @ dir_find    \ 1. bank name -- bank entry#
    ientry ! ibank !      \ Save entry# and bank
    isname if
        settypsz            \ Set itype and isize
	willfit if           \ 2.
            isempty if        \ 3.
		rom2banks      \ 4.
		setbanks        \ 5.
                isok errno !
                true
            else
                nofree errno !
                false
            then
        else
            nofit errno !
            false
        then
    else
        noname errno !
        false
    then
;

: unplug ( name -- flag )
    \ 1. Check that name exists, return false if not
    \ 2. Check that name exists in bank(s), return false if not
    \ 3. Check if name is IRAM and save if true
    \ 4. Clear bank(s) configuration that match name
    \ 5. Return true
    romdir @ dir_find       \ 1. name -- entry#
    ientry ! isname if       \ entry# --
        isbanked if           \ 2.
            ientry @ romdir @  \ -- entry# sector16k
            entry_type          \ entry# sector16k -- type.size
            $F0 and IRAM = if    \ 3.
                save              \ --
            then
            8 0 ?do
                ientry @           \ -- entry#
                i bank@ $03FF and   \ entry# -- entry# bank-entry#
                = if
                    0 i bank!         \ 4. Clear configuration
                then
            loop
            isok errno !
            true
        else
            nobank errno !
            false
        then
    else
        noname errno !
        false
    then
;

: unplugall ( -- )
    \ Set all banks to zero, clearing InUse flag
    8 0 ?do
        0 i bank!
    loop
;

: clrbank ( bank -- )
    \ Mark a bank as not InUse
    \ Use with caution!
    0 swap bank!
;

