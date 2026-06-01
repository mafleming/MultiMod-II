
\ --------------------------------------------------------------------
\ Forth Dictionary Images
\ --------------------------------------------------------------------

: forthsave ( name -- )
    \ Save current Forth dictionary as `name` in Forth directory.
    \ If name already exists, replace image content.
    2dup forthdir @  \ ( name -- name name sector16k )
    dir_find          \ ( name name sector16k -- name entry# )
    dup 1024 = if      \ 1024 means not found
        drop            \ ( name 1024 -- name )
        ROM 1+           \ ( name type-size -- )
        forthdir @        \ ( name type-size sector16k -- )
        dir_insert         \ ( name type-size sector16k -- block# )
        forthdir @          \ ( block# sector16k --  )
        image_addr           \ ( block# sector16k -- sector16k )
        save                  \ ( -- )
    else                       \ ( name entry# -- )
        forthdir @ entry_image  \ ( name entry# sector16k -- name block# )
        forthdir @ image_addr    \ ( name block# sector16k -- name sector16k )
        save                      \ ( name sector16k -- name )
        2drop                      \ ( -- )
    then
;

: forthload ( name -- )
    \ Load specified Forth dictionary by name. Need to check for
    \ error return value (1024) if name is not found.
    forthdir @      \ ( name -- name sector16k )
    dir_find         \ ( name sector16k -- entry# )
    dup 1024 = if     \ ( entry# entry# -- entry# )
        drop           \ ( entry# -- )
        ." Name not found"
    else
	forthdir @       \ ( entry# -- entry# sector16k )
	entry_image       \ ( entry# sector16k -- block# )
	forthdir @         \ ( block# -- block# sector16k )
        image_addr          \ ( block# sector16k -- sector16k )
        load                 \ ( -- )
    then
;

: forthlist ( -- )
    \ List Forth directory entries.
    forthdir @      \ ( sector16k -- )
    cr dir_list      \ ( -- )
;

\ --------------------------------------------------------------------
\ Serial Transfer Protocol Support
\ These two commands can be modified to support whatever might be the
\ standard transfer protocol. For the moment, that would be the old
\ MultiMod hex file format.
\ --------------------------------------------------------------------

: send2host ( size -- )
    \ Send the data in SPRAM to the host using selected protocol
    case                 \ (size -- )
        1 of 0 hexdump endof
	2 of 0 hexdump32 endof
	3 of 0 hexdump 1 hexdump32 endof
        4 of 0 hexdump64 endof
        ." Unknown image size"
    endcase
;

: host2ram ( -- size )
    \ Accept data from host and save to SPRAM.
    \ Assumes that a full 16KB or multiple thereof is sent.
    \ If lower 13 bits are non-zero, then add 1 after rshift.
    0 hexload              \ Accept hex data, send to start of SPRAM
    dup 13 rshift           \ 16KB = 8K Words
    swap $1FFF and 0<> if    \ Lower 13 bits not zero?
        1+
    then
;


\ --------------------------------------------------------------------
\ HP-71B ROM/IRAM Images
\ --------------------------------------------------------------------

: writeflash ( name type -- )
    \ Download image to flash.
    \ Should check to see if file already exists in the directory.
    \ If so, update its image according to size in the entry.
    >r              \ ( name type -- name )
    2dup romdir @    \ ( name -- name name sector16k )
    dir_find          \ ( name name sector16k -- name entry# )
    dup 1024 = if      \ ( name entry#  -- name entry# )
	drop host2ram   \ ( name 1024 -- name size )
	dup r> +         \ ( name size -- name size type.size )
	swap >r           \ ( name size type.size -- name type.size )
	romdir @           \ ( name type.size -- name type.size sector16k )
	dir_insert          \ ( name type.size sector16k -- block )
	romdir @ image_addr  \ ( block sector16k -- sector16k )
	r>                    \ ( sector16k -- sector16k size )
	case
	    1 of 0 swap ram2rom endof
	    2 of 0 swap ram32k2rom endof
	    3 of 0 swap ram2rom 1 ram32k2rom endof
	    4 of 0 swap ram64k2rom endof
            drop ." Unknown image size"
	endcase
    else               \ Replace entry image
        -rot 2drop      \ ( name entry# -- entry# )
        r> drop          \ Don't need supplied type.size
        dup romdir @      \ ( entry# -- entry# entry# sector16k )
        entry_type         \ ( entry# entry# sector16k -- entry# type.size )
        $F and swap         \ ( entry# type.size -- size entry# )
        romdir @             \ ( size entry# -- size entry# sector16k )
        entry_image           \ ( size entry# sector16k -- size sector16k )
        image_addr swap        \ ( size sector16k -- sector16k size )
        case
            1 of 0 swap ram2rom endof
            2 of 0 swap ram32k2rom endof
            3 of 0 swap ram2rom 1 ram32k2rom endof
            4 of 0 swap ram64k2rom endof
            drop ." Unknown image size"
        endcase
    then
;

: readflash ( name -- )
    \ Upload image from flash.
    \ Error if name doesn't appear in the ROM/IRAM directory.
    romdir @        \ ( name -- name sector16k )
    dir_find         \ ( name sector16k -- entry# )
    dup 1024 = if     \ ( entry#  -- entry# )
        drop           \ ( entry# -- )
        ." Name not found"
    else
        dup romdir @     \ ( entry# -- entry# entry# sector16k )
        entry_image       \ ( entry# entry# sector16k -- entry# block# )
        swap romdir @      \ ( entry# block# -- block# entry# sector16k )
        entry_type          \ ( block# entry# sector16k -- block# type.size )
        $F and swap          \ ( block# type.size -- size block# )
        romdir @              \ ( size block# -- size block# sector16k )
        image_addr             \ ( size block# sector16k -- size sector16k )
        over case               \ (size sector16k -- size sector16k size )
            1 of 0 rom2ram endof
            2 of 0 rom32k2ram endof
            3 of dup 0 rom2ram 1+ 0 rom32k2ram endof
            4 of 0 rom64k2ram endof
            2drop ." Unknown image size"
            leave
        endcase                      \ (size -- )
        send2host                     \ ( size -- )
    then
;

: romlist ( -- )
    \ List HP-71B directory entries.
    romdir @      \ ( -- sector16k )
    cr dir_list    \ List directory entries
;


\ --------------------------------------------------------------------
\ FPGA Bitstream Images
\ --------------------------------------------------------------------

: bitstream ( slot -- )
    \ Write FPGA bitstream to flash. Each image is aligned on
    \ 0x020000 boundaries. Valid `slot` number is 1, 2, or 3.
    \ Bitstreams start in sector16k: 8, 16, 24

    dup 1 <        \ ( slot -- slot flag ) Less than 1?
    over 3 >        \ ( slot flag -- slot flag flag ) Greater than 3?
    or if            \
        cr ." Out of range, Use 1, 2, or 3"
        drop           \
    else                \
        bitfence *       \ slot * sector16k
        8 0 ?do           \
            i onesram      \ Set all of SPRAM to foxes
        loop                \
        host2ram             \ ( sector16k -- sector16k ncount ) Read bitstream
        drop dup              \ ( sector16k ncount -- sector16k sector16k )
        0 swap ram64k2rom      \ Half of SPRAM to flash
        4 swap ram32k2rom       \ Other half to flash
    then
;

