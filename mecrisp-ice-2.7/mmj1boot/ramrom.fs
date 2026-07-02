\ #######   HP-71B  ###########################################
\ Words to support HP-71B ROM/RAM modules in flash

: rom2ram ( sector16k ram# -- )
    \ Copy 16K ROM image to SPRAM
    \ ram# 0~7, 16K block in SPRAM
    \ sector16k frthstart~1023, 16K block in flash ( 14 MB )

    $AB >spi  \ Release from Deep Power Down Mode, IgorM
    idle
    0 begin 1+ dup 500 =  until drop \ delay
    
    \ Set SPI flash address to 16K block number
    swap               \ ( ram# sector16k -- )
    03             >spi \ Read command
    dup 2 rshift   >spi  \ Sector number, bits 7-2
    3 and 6 lshift >spi   \ Address high, bits 1-0 << 6
    $00            >spi    \ Address low

    $2000 *   \ ( Ram_pointer -- )
    0 swap     \ ( Ram_counter Ram_pointer -- )
    begin
        spi> spi> 8 lshift or    \ ( Ram_counter Ram_pointer Word -- )
        over            \ ( Ram_counter Ram_pointer Word Ram_pointer -- )
        sram!            \ ( -- Ram_counter Ram_pointer )
        1+ swap 1+ swap   \ ( Ram_counter Ram_pointer -- )
        over $2000 =
    until
    idle 2drop
;

: rom32k2ram ( sector16k ram# -- )
    \ Copy 32K ROM image to SPRAM
    \ ram# 0~7, 16K block in SPRAM
    \ sector16k frthstart~1023, 16K block in flash ( 14 MB )

    dup 7 u< if     \ 32K won't fit last 16K block
        2dup rom2ram
        1+ swap 1+ swap
        rom2ram
    else
        2drop
    then
;

: rom64k2ram ( sector16k ram# -- )
    \ Copy 64K ROM image to SPRAM
    \ ram# 0~7, 16K block in SPRAM
    \ sector16k frthstart~1023, 16K block in flash ( 14 MB )

    dup 5 u< if     \ 64K won't fit last 16K block
        2dup rom2ram
        1+ swap 1+ swap 2dup
        rom2ram
        1+ swap 1+ swap 2dup
        rom2ram
        1+ swap 1+ swap
        rom2ram
    else 2drop then
;



: ram2rom  ( ram# sector16k -- )
    \ ram# 0~7, 16K block in SPRAM
    \ sector16k 32~127, 16K block in flash ( 2 MB )

    dup bitfence 1- u> if    \ Never overwrite bitstream !

        swap $2000 * swap      \ ( ram# sector16k -- ram_addr sector16k )
        $AB >spi                \ Release from Deep Power Down
        idle
        dup erase
        dup 3 and 14 lshift    \ beginning count
        begin   \ ( ram_addr sector16k spi_addr -- )
            spiwe

            $02            >spi  \ Page program (256 bytes)
            over 2 rshift  >spi   \ Sector number
            dup 8 rshift   >spi    \ Address high
            $00            >spi     \ Address low

            rot                  \ ( sector16k spi_addr ram_addr -- )
            begin                 \ Write 256 bytes, incrementing counter
                dup sram@ 
                dup $FF and >spi
                8 rshift    >spi
                1+
                dup $7F and 0=
            until

            idle
            waitspi
            rot rot $0101 +     \ ( ram_addr sector16k spi_addr -- )
            dup $3F and $00 =    \ for 16kB ram
        until
        2drop drop

    else 2drop then \ Bitstream protection
;

: ram32k2rom ( ram# sector16k -- )
    \ Copy 32K SRAM image to ROM
    \ ram# 0~7, 16K block in SPRAM
    \ sector16k 32~127, 16K block in flash ( 2 MB )

    over 7 u< if     \ 32K isn't last 16K block
        2dup ram2rom
        1+ swap 1+ swap
        ram2rom
    else
        2drop
    then
;

: ram64k2rom ( ram# sector16k -- )
    \ Copy 64K RAM image to ROM
    \ ram# 0~7, 16K block in SPRAM
    \ sector16k 32~127, 16K block in flash (2 MB)

    over 5 u< if    \ 64K won't fit in last 3 16K blocks
        2dup ram2rom
        1+ swap 1+ swap 2dup
        ram2rom
        1+ swap 1+ swap 2dup
        ram2rom
        1+ swap 1+ swap
        ram2rom
    else
        2drop
    then
;

: ram2ram ( ramfrom# ramto# -- )
    \ Copy content of one 16K ram block to another
    \ ramfrom# 0~7, 16K block is SPRAM
    \ ramto# 0~7, 16K block is SPRAM
    
    $2000 * swap $2000 * swap
    begin             \ ( from_addr to_addr -- )
        over sram@     \ ( from_addr to_addr -- from_addr to_addr word )
        over sram!      \ ( from_addr to_addr word -- from_addr to_addr )
        1+ swap 1+ swap  \ ( from_addr to_addr -- )
        dup $1FFF and 0=
    until
    2drop
;

: pagecmp ( ram_addr -- ram_addr flag )
    8192 0 ?do      \ Number of words in SPRAM page
        dup sram@    \ ( ram_addr -- ram_addr ram_word )
        spi> spi>     \ ( ... -- ram_addr ram_word lowbyte highbyte )
        8 lshift or    \ ( ... -- ram_addr ram_word flash_word )
        <>              \ ( ram_addr ram_word flash_word -- ram_addr flag )
        if               \ ( ram_addr flag -- ram_addr )
            leave         \ (ram_addr flag -- ram_addr fail )
        then               \ Increment ram_addr
        1+                  \ ( ram_addr -- ram_addr+1 )
    loop
    dup $1FFF and 0=          \ (ram_addr -- ram_addr flag )
;

: ramromcmp ( size ram# sector16k -- mem_addr flag )
    \ Given the sector16k location of an image in flash, the ram# page
    \ of the same image and the size of the image in number of blocks,
    \ return a flag indicating the images match and the last address
    \ of the word in SPRAM of a mismatch if there was a mismatch.
    spiread16k      \ ( size ram# sector16k -- size ram# )
    $2000 *          \ ( size ram# -- size ram_addr )
    swap 0 swap ?do   \ ( size ram_addr -- ram_addr )
        pagecmp        \ ( ram_addr -- ram_addr flag )
        not if          \ ( ram_addr flag -- ram_addr )
            leave        \ ( ram_addr -- ram_addr )
        then
    loop
    idle
    dup $1FFF and 0=          \ (ram_addr -- ram_addr flag )
;

: zeroram ( ram# -- )
    \ Fill indicated SPRAM block with zeros, used to make IRAMs

    $2000 *         \ Starting address in SPRAM
    $2000 0 ?do      \ Fill 8K of 16-bit words
        0 over sram!  \ With zeros
        1+             \ Next address
    loop
    drop
;

: onesram ( ram# -- )
    \ Fill indicated SPRAM block with ones, used to fill out data
    \ written to flash.

    $2000 *             \ Starting address in SPRAM
    $2000 0 ?do          \ Fill 8K of 16-bit words
        $FFFF over sram!  \ With ones
        1+                 \ Next address
    loop
    drop
;
