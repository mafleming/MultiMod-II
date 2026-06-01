\ #######   Temporary Debug   #################################

: rompeek ( rom# byte_count -- )
    \ ram# 0~15, 16K block is SPRAM
    \ rom# 32~127, 16K block in flash (2 MB)
    \ Set SPI flash address to 16K block number

    $AB >spi  \ Release from Deep Power Down Mode
    idle
    0 begin 1+ dup 500 =  until drop \ delay 100us
    
    swap               \ ( byte_count rom# -- )
    03             >spi \ Read command
    dup 2 rshift   >spi  \ 64K Sector number, bits 7-2
    3 and 6 lshift >spi   \ Address high, bits 1-0 << 6
    $00            >spi    \ Address low

    begin   \ ( byte_count -- )
        spi> .x
        1-
        dup 0=
    until
    drop
;

: romdump ( rom_addr_double #lines -- )
    \ Dump #lines number of flash bytes starting st specified address
    >r spiread r>
    0 ?do
        spi> spi> 8 lshift or dup .x
        spi> spi> 8 lshift or dup .x
        swap space space
        dup $FF and emit 8 rshift emit
        dup $FF and emit 8 rshift emit cr
    loop
;

: rampeek ( ram_address byte_count -- )
    begin
        over sram@ .x
        swap 1+
        swap 1- dup 0=
    until
    2drop
;

: ramfill ( value ram_address byte_count -- )
    \ Fill ram with pattern value
    0 ?do           \ Save count
        2dup sram!   \ Write value
        1+            \ increment address
    loop
    2drop
;
