
\ #######   Image Transfer   ##########################################
\ Words to transfer ROM images to/from SPRAM via the console.
\ ROM/IRAM .bin files are converted to a hex ASCII stream of
\ 32 bytes (64 characters) per line, sent to the MultiMod II,
\ then converted back to binary in SPRAM. The process can also
\ be performed in reverse, sending binary data in SPRAM as
\ a stream of hex characters.

: bin2hex ( nibble -- char )
  \ Convert 4 bit number to hex character
    $30 + dup $39 > if $7 + then
;

: hexdump ( ram# -- )
    \ Dump the content of a 16KB RAM block in hex, 32 bytes per line.
    \ Bytes in a 16-bit dictionary word are output low byte first.
    \ Bytes themselves are output high nibble first, then low nibble.

    $2000 * 0 swap   \ ( counter ram_address -- )
    begin
        dup sram@
                     \ Low byte
        dup $FF and
        dup 4 rshift bin2hex emit
        $F and bin2hex emit
                     \ High byte
        8 rshift
        dup 4 rshift bin2hex emit
        $F and bin2hex emit
                     \ Increment address and counter
        1+ swap 1+ swap
                     \ Terminate line after 32 bytes, 16 words
        dup $F and 0= if cr then
        over $2000 =
    until
    2drop
;

: hexdump32 ( ram# -- )
    \ Dump two contiguous 16K RAM blocks as 32K image
    dup 6 u> if      \ 32K isn't last 16K block
        dup hexdump   \ First 16K RAM block
        1+  hexdump    \ Second block
    else
        drop             \ Fail
    then
;

: hexdump64 ( ram# -- )
    \ Dump four contiguous 16K RAM blocks as 64K image
    dup 4 u> if          \ 64K isn't last 3 16K blocks
        dup    hexdump    \ First 16K RAM block
        1+ dup hexdump     \ Second
        1+ dup hexdump      \ Third
        1+     hexdump       \ Fourth
    else
        drop
    then
;



: hex2bin ( -- byte|-1 )
  \ Read two hex characters, return byte equivalent
  \ Return -1 if two carriage returns read in a row
    key             \ High nibble
    dup $0D = if     \ EOL?
        drop key      \ Second EOL?
        dup $0D = if   \ Two CR's terminate
            drop -1
            exit
        then
    then
    $30 - dup 9 > if $7 - then
    key                      \ Low nibble
    $30 - dup 9 > if $7 - then
    swap 4 lshift or
;

: hexload ( ram# -- nwords )
\ Load hex data stream to 16K RAM page. Data stream is any
\ length, multiple of 4 hex digits, terminated by two carriage returns

    dup $2000 *     \ ( ram# -- ram# ram_address )
    begin
        dup           \ (ram# ram_address -- ram# ram_address ram_address )
        hex2bin        \ Get low byte of SRAM word
                        \ ( -- ram# ram_address low_byte )
        dup -1 = if      \ Encountered two CR's? (EOF)
	    2drop         \ ram# ram_address ram_address low_byte --
	                   \ ram# ram_address )
            swap $2000 *    \ ( ram# ram_address -- ram_address1 ram_address2)
            -                \ ( ram_address1 ram_address2 -- #words )
	    exit
        then

        hex2bin       \ Get high byte of SRAM word
                       \ ( ram# ram_address ram_address low_byte high_byte )
	dup -1 = if     \ ( ram# ram_address ram_address low_byte high_byte --
	    drop swap    \ ( -- ram# ram_address low_byte ram_address )
	    sram!         \ Write ( ram# ram_address low_byte ram_address -- )
	                   \ low byte ( --  ram# ram_address )
            swap $2000 *    \ ( ram# ram_address1 -- ram_address1 ram_address2)
            -                \ ( ram_address1 ram_address2 -- #words )
	    exit
        then

        8 lshift or    \ Assemble word
                        \ ( -- ram# ram_address ram_address word )
        swap sram! 1+    \ ( -- ram# ram_address )
    again
;
