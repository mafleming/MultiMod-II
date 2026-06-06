\ SPI Flash Security
\
\ These words support flash security by providing hardware write protection
\ for the bootloader and other features.

: rdstatreg ( status-register-num -- register-value )
    \ Read Status Register.
    \ Register number is $05 (1), $35 (2), or $15 (3)
    >spi          \ Read status register command ($05, $35, $15)
    spi>           \ Read register
    idle            \ End of command
;

: wrstatreg ( register-value status-register-num -- )
    \ Write Status Register.
    \ Register number is $05 (1), $35 (2), or $15 (3)
    spiwe         \ Enable write to status registers
    >spi           \ Write status register 3
    >spi            \ Updated register value
    idle             \ End of command
;


: WPSset ( -- )
    \ Set write protect status bit in status register 3.
    \ This flag must be set in order to write protect flash memory on a
    \ block by block basis (4 KB blocks).
    $AB >spi   \ Release from Deep Power Down Mode, IgorM
    idle
    0  begin 1+ dup 500 =  until drop \ delay 100us
    $15 rdstatreg \ Read status register 3
    $4 or            \ Set WPS bit (2)
    $11 wrstatreg     \ Write status register 3
;

: WPSclr ( -- )
    \ Clear write protect status bit in status register 3.
    \ This flag needs to be cleared before using the CH431A programmer
    \ to write a new image to flash.
    $AB >spi   \ Release from Deep Power Down Mode, IgorM
    idle
    0  begin 1+ dup 500 =  until drop \ delay 100us
    $15 rdstatreg \ Read status register 3
    $FB and        \ Clear WPS bit (2)
    $11 wrstatreg   \ Write status register 3
;

: blklock (end start -- )
    \ Lock the 4KB sectors in range start..end-1
    \ The WPS flag must be set for this to have an effect. Note also
    \ that the lock bits are all set on power-up or reset and therefore
    \ must be initialized as unlocked for most of flash.

    0 0            \ Starting double_rom_addr
    2swap ?do       \ Loop through the start..end-1 4KB blocks
	2dup         \ Make index a double
	spiwe         \ Write enable status registers
        $36 >spi       \ Block/Sector Lock command
        addr2spi        \ Output 24-bit address
	idle             \ End of command
        $1000 0 d+        \ Increment addr to next 4KB block
    loop
    2drop
;

: blkunlock (end start -- )
    \ Unlock the 4KB sectors where the bootloader is stored.

    0 0            \ Starting double_rom_addr
    2swap ?do       \ Loop through the 32 4KB blocks
	2dup         \ Make index a double
	spiwe         \ Write enable status registers
        $39 >spi       \ Block/Sector Lock command
        addr2spi        \ Output 24-bit address
	idle             \ End of command
        $1000 0 d+        \ Increment addr to next 4KB block
    loop
    2drop
;

: blkstatus ( end start -- )
    \ Display write protection status of the bootloader blocks.
    \ The bootloader is in address range $000000~$01FFFF
    \ This is sector16k 0~7, sector4k 0~31
    $AB >spi   \ Release from Deep Power Down Mode, IgorM
    idle
    0  begin 1+ dup 500 =  until drop \ delay 100us

    0 0            \ Starting double_rom_addr
    2swap ?do       \ Loop through the 32 4KB blocks
        2dup         \ Make index a double
        $3D >spi      \ Read Block/Sector Lock command
        addr2spi       \ Output 24-bit address
        spi>            \ Status, low bit unlock/lock
	idle             \ End of command
        $01 and           \ Mask the lock status bit
        $30 + emit         \ Print 0 or 1
        $1000 0 d+          \ Increment addr to next 4KB block
    loop
    2drop
;


: bootlock ( -- )
    \ Lock the 4KB sectors where the bootloader is stored.
    \ The bootloader is in address range $000000~$01FFFF
    \ This is sector16k 0~7, sector4k 0~31
    \ The WPS flag must be set for this to have an effect. Note also
    \ that the lock bits are all set on power-up or reset and therefore
    \ must be initialized as unlocked for most of flash.

    32 0 blklock    \ Loop through the 32 4KB blocks
;

: bootunlock ( -- )
    \ Unlock the 4KB sectors where the bootloader is stored.
    \ The bootloader is in address range $000000~$01FFFF
    \ This is sector16k 0~7, sector4k 0~31
    \ The WPS flag must be set for this to have an effect. Note also
    \ that the lock bits are all set on power-up or reset and therefore
    \ must be initialized as unlocked for most of flash.

    32 0 blkunlock  \ Loop through the 32 4KB blocks
;
