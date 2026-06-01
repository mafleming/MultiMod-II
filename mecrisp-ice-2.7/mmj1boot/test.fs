
  \ Erase 16K sector then save instruction store
: saveramstub  ( ram# sector16k -- )
    dup 31 u> if \ Never overwrite bitstream !

        swap $2000 * swap    \ ( ram_addr sector16k -- )
        \ $AB >spi \ Release from Deep Power Down
        \ idle
        \ dup erase
        dup 3 and 14 lshift      \ beginning count
        begin   \ ( ram_addr sector16k spi_addr -- )
            \ spiwe

            $02            .x    \ Page program (256 bytes)
            over 2 rshift  .x     \ Sector number
            dup 8 rshift   .x      \ Address high
            $00            .x       \ Address low

            rot dup .x cr       \ ( sector16k spi_addr ram_addr -- )
            begin   \ Write 256 bytes, incrementing counter
                dup sram@  drop
                1+
                dup $7F and 0=
            until

            \ idle
            \ waitspi
            rot rot $0101 +     \ ( ram_addr sector16k spi_addr -- )
            dup $3F and $00 =    \ for 16kB ram
        until
        2drop

    else drop then \ Bitstream protection
;

: ram2rom ( ram# rom# -- )
    \ ram# 0~7, 16K block is SPRAM
    \ rom# 32~127, 16K block in flash ( 2 MB )

    dup 31 u> if \ Never overwrite bitstream !

        $AB >spi  \ Release from Deep Power Down Mode
        idle
        0 begin 1+ dup 500 =  until drop \ delay 100us
        
        swap $2000 * swap   \ ( ram_pointer sector16k -- )
        dup erase16
        6 lshift      \ page address
        begin          \ (ram_pointer page_address -- )
            spiwe

            $02          >spi \ Page program
            dup 8 rshift >spi  \ Sector number
            dup $FF and  >spi   \ Address high
            $00          >spi    \ Address low

            swap
            begin   \ ( page_address ram_pointer -- )
                dup sram@ dup $FF and >spi 8 rshift >spi
                1+   \ ( page_address ram_pointer -- )
                dup $7F and 0=
            until

            idle
            waitspi

            swap 1+    \ ( ram_pointer page_address -- )
            dup $3F and 0=    \ for 16kB ram
        until

    then
    2drop
;


: ram2stub ( ram# rom# -- )
    \ ram# 0~7, 16K block is SPRAM
    \ rom# 32~127, 16K block in flash ( 2 MB )

    dup 31 u> if \ Never overwrite bitstream !

        \ $AB >spi  \ Release from Deep Power Down Mode
        \ idle
        \ 0 begin 1+ dup 500 =  until drop \ delay 100us
        
        swap $2000 * swap   \ ( ram_pointer sector16k -- )
        \ dup erase16
        6 lshift      \ page address
        begin         \ (ram_pointer page_address -- )
            \ spiwe

            $02          .x \ Page program
            dup 8 rshift .x  \ Sector number
            dup $FF and  .x   \ Address high
            $00          .x    \ Address low

            swap dup .x cr
            begin   \ ( page_address ram_pointer -- )
                dup sram@ drop \ dup $FF and >spi 8 rshift >spi
                1+   \ ( page_address ram_pointer -- )
                dup $7F and 0=
            until

            \ idle
            \ waitspi

            swap 1+    \ ( ram_pointer page_address -- )
            dup $3F and 0=    \ for 16kB ram
        until

    then
    2drop
;

: hexstub ( ram# -- )
\ Load hex data stream to 16K RAM page. Data stream is any
\ length, multiple of 4 hex digits, terminated by two carriage returns

    $2000 *  \ ( ram_address -- )
    begin
        dup
        hex2bin        \ Get low byte of SRAM word
        \ ( ram_address ram_address low_byte -- )
        dup -1 = if
            .x .x cr drop exit
        then

        hex2bin        \ Get high byte of SRAM word
        \ ( ram_address ram_address low_byte high_byte -- )
        dup -1 = if
            drop .x .x cr drop exit
        then

        8 lshift or    \ Assemble word
        \ ( ram_address ram_address word -- )
        swap .x .x cr 1+
    again
    drop
;
