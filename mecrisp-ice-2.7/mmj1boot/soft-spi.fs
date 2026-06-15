\ #######   Flash   ###########################################

\ SPI Flash tools and loader
\ NOTE: Code based on that from UPduino-Mecrisp-Ice-15kB by Igor-m
\ BSD -3-Clause license
\ https://github.com/igor-m/UPduino-Mecrisp-Ice-15kB.git

\ Fomu seems to have used some of the first 7 64K sectors for
\ flash bitstream or data. Reserve the first 1MB (64 16K) sectors
\ for bitstreams and their data. 16KB sectors 0 to 15 are the
\ powerup/reset bitstream and should be protected.
\ Default load on power-up would be 16K sector number 0

8 constant bitfence  \ Write protect 16KB pages below this point
64 constant frtstart  \ Forth dictionary images start point
128 constant romstart  \ HP-71B ROM/IRAM image start

\ #############################################################
\ #######    SPI IO    ########################################

: idle  ( -- )
    \ Deselect flash to mark the end of a command
    1 $0100 io!   \ Deselect flash CS/ = 1
;

: spixbit ( x -- y )
    \ Output data in high byte, assemble input in low byte
    dup 0< 2 and        \ extract MS bit
    dup $0100 io!        \ lower SCK, update MOSI
    4 + $0100 io!         \ raise SCK
    2*                     \ next bit
    $0200 io@ +             \ read MISO, accumulate
;

: spix ( outdata -- indata )
    8 lshift
    spixbit spixbit spixbit spixbit
    spixbit spixbit spixbit spixbit
;

: >spi ( -- byte )
    spix drop
;

: spi> ( byte -- )
    0 spix
;

: waitspi  ( -- )
  begin
    $05 >spi \ Read Flag status register
    spi> $01 and 0= \ WIP: Write in Progress.
    idle
  until
;

: spiwe ( -- )
    $06 >spi \ Write enable
    idle
;


\ #############################################################
\ #######    SPI SUPPORT     ##################################

: sect2addr ( sector16k -- double_rom_addr )
    \ Convert a 10-bit `sector16k` value to a 24-bit flash address.
    \ Where TOS is high 8 bits and next is low 16 bits.
    dup 3 and 14 lshift      \ Low 16 bits of address
    swap 2 rshift             \ High 16 bits of address
;

: addr2sect ( double_rom_addr -- sector16k )
    \ Convert a 24-bit flash address to a 10-bit sector16k value.
    $FF and 2 lshift    \ High 8 bits masked and shifted right
    swap 14 rshift or    \ Divide by 16K, form lower 2 bits of sector16k
;

: addr2spi ( double_rom_addr -- )
    \ Output a 24-bit address to spi flash. The address is a double
    \ where the high 8 bits are in TOSand the low 16 bits in NOS.
    $FF and      >spi     \ Address high byte
    dup 8 rshift >spi      \ Address mid byte
    $FF and      >spi       \ Address low byte
;

: spiread ( double_rom_addr -- )
    \ Set up the read command and byte address
    \ Address is a double, high 16-bits in TOS
    $AB >spi  \ Release from Deep Power Down Mode, IgorM
    idle
    0  begin 1+ dup 500 =  until drop \ delay 100us

    03 >spi              \ Read command
    addr2spi              \ Output 24-bit address
;

: spiwrite ( double_rom_addr -- )
    \ Set up the write command and byte address
    \ Address is a double, high 16-bits in TOS
    $AB >spi  \ Release from Deep Power Down Mode, IgorM
    idle
    0  begin 1+ dup 500 =  until drop \ delay 100us
 
    spiwe       \ Setup write
    02 >spi      \ Write command
    addr2spi      \ Output 24-bit address
;

: spiread16k ( sector16k -- )
    \ Set up the read command and sector address
    sect2addr
    spiread
;


\ #############################################################
\ #######    SPI UTILITY     ##################################

: spiflush ( Nbytes -- )
    \ Flush N bytes from the spi flash being read
    0 ?do
        spi> drop
    loop
;

: spidump ( Nbytes -- )
    \ Print N bytes from the spi flash being read
    0 ?do
        spi> .
    loop
;

: numsectors ( -- #sector16k )
    \ Return the number of 16K sectors in this flash device
    \ For device independence, RDID command capacity is # of address bits
    \ i.e. 21 = 2M, 22 = 4M, etc.
    $AB >spi  \ Release from Deep Power Down Mode, IgorM
    idle
    0  begin 1+ dup 500 =  until drop \ delay 100us
    $9F >spi spi> drop spi> drop spi> 14 - 1 swap lshift
;


\ #######   DATA I/O   ########################################
\ Definition of load/save/erase words support 16K sectors
\ 2 MB flash: 128 sectors, 4 MB flash: 256 sectors, 16 MB flash: 1024 sectors

  \ There's only a 4K and 64K sector erase command
: erase4k ( sector4k -- )
    dup bitfence 2 lshift 1- u> if   \ Never overwrite bitstream !
        $AB >spi                      \ Release from Deep Power Down
        idle
        0  begin 1+ dup 500 =  until drop  \ delay

        spiwe
        $20              >spi    \ Sector erase, 4K
        dup 4 rshift     >spi     \ Sector number, bits 9 to 4
        $F and 4 lshift  >spi      \ Address high
        $00              >spi       \ Address low
        idle
        waitspi
    else drop then
;

  \ Erase 16K sectors using 4K sector erase command four times
: erase ( sector16k -- ) \ Erase 4 4K sectors given 16K sector number
  dup + dup +   \ 4K sector number is 4 times 16K sector number
  dup erase4k
  1+ dup erase4k
  1+ dup erase4k
  1+ erase4k
;

: load ( sector16k -- )
    \ Save FORTH instruction store (15K EBR) to 16K sector
    $AB >spi  \ Release from Deep Power Down Mode, IgorM
    idle
    0  begin 1+ dup 500 =  until drop \ delay 100us

    spiread16k
    spi> spi> 8 lshift or

    dup $FFFF <> \ Execution starts at address 0, there always will be a valid opcode.
    if             \ $FFFF denotes an empty sector that should not be loaded.
	0 !         \ Store first byte

        2             \ 2nd through 15K bytes
        begin
        spi> spi> 8 lshift or over !
        2 +
        dup $3C00 =       \ For 15kB ram
        until

    then

    drop
    idle
    init \ @i ?dup if execute then \ The freshly loaded image might have init set
    quit
;

  \ Erase 16K sector then save instruction store
: save ( sector16k -- )
    dup bitfence u> if \ Never overwrite bitstream !

        $AB >spi \ Release from Deep Power Down
        idle
        0  begin 1+ dup 500 =  until drop \ delay 100us

	dup erase
        sect2addr
	begin              \ addrL addrH --
	    spiwe           \ Write enable
            $02 >spi         \ Page program (256 bytes)
	    2dup addr2spi     \ Output 24-bit address
	    swap               \ addrL addrH -- addrH addrL
            begin               \ Write 256 bytes, incrementing counter
		dup $3FFF and    \ Address range 0~$3FFF
		c@ >spi           \ Read dictionary, write flash
                1+                 \ Increment addrL
                dup $FF and 0=      \ 256 bytes?
	    until
            idle                      \ Must disable select after last byte
	    waitspi                    \ Wait for write to finish
	    swap over                   \ addrH addrL -- addrL addrH addrL
            $3FFF and $3C00 =            \ for 15kB ram
        until
        2drop

    else drop then \ Bitstream protection
;

cornerstone hp71b    \ Everything below this is core Forth
