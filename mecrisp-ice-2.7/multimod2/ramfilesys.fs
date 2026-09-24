\ #######   HP-71B  ###########################################
\ Words to support HP-71B ROM/RAM modules in SPRAM
\ Implementation of File System support

\ #######   Low Level Access   ################################

: nib@ ( ud_nibble_addr -- nibble )
    \ Return the nibble in 128KB SPRAM addressed by unsigned double
                    
    1 and           \ Limit address to 17 bits
    2dup 2 2rshift   \ 16-bit word address
    drop              \ d>s
    sram@ >r           \ Read word and save
    3 s>d 2and          \ Nibble number, double
    drop 4 *             \ d>s, Shift count, 0, 4, 8, 12
    r> swap rshift        \ Shift selected nibble to low 4 bits
    $F and                 \ Mask nibble and return
;

: nib!
 ( nibble ud_nibble_addr -- )
    \ Write nibble to memory at unsigned double nibble address

    1 and           \ Limit address to 17 bits
    2dup 2 2rshift   \ 16-bit word address
    drop dup          \ d>s, keep copy
    >r sram@ >r        \ Save memory address, read memory word and save
    drop 3 and 4 *      \ d>s, Shift count, 0, 4, 8, 12
    dup $F swap lshift   \ Mask
    not r> and >r         \ Mask off target nibble, save
    lshift                 \ Shift nibble to position
    r> or                   \ Or nibble into place
    r> sram!                 \ Write updated memory word
;

: peek ( ud_nibble_addr count -- ud_nibbles )
    \ Read count nibbles, count<=8, return double word

    $1F and         \ Trim count
    dup 8 u> if      \ to valid max
        drop 8        \ value
    then
                        \ ( ud_nibble_addr count -- )
    >r 0 s>d r>          \ ( ud_nibble_addr ud_value count -- )
    0 ?do                 \ ( ud_nibble_addr ud_value -- )
        2over nib@ >r      \ Read nibble and save
        2swap               \ ( ud_value ud_nibble_addr -- )
        1 s>d d+             \ Increment nibble address
        2swap                 \ ( ud_nibble_addr ud_value -- )
        r> s>d                 \ Retrieve nibble, s>d
        i 4 * 2lshift           \ Shift nibble to position
        2or                      \ Or into result
    loop
                                   \ ( ud_nibble_addr ud_value -- )
    2swap 2drop                     \ Drop nibble address
;

: poke ( ud_nibble_addr ud_value count -- )
    \ Poke count number of nibbles to SPRAM address, count <= 8

    $1F and         \ Trim count
    dup 8 u> if      \ to valid max
        drop 8        \ value
    then
    0 ?do
        2dup >r >r       \ Save nibble value
        $F s>d and        \ Mask off low nibble of double value
        2over nib!         \ Write nibble to SPRAM
        1 s>d d+            \ Increment nibble address
        r> r> 4 2rshift      \ Restore value, shift to next nibble
    loop
    2drop 2drop                \ Drop address and value arguments
;

\ : zeroram ( ram# -- )
\    \ Fill indicated SPRAM block with zeros, used to make IRAMs
\ 
\     $2000 *         \ Starting address in SPRAM
\     $2000 0 ?do      \ Fill 8K of 16-bit words
\         0 over sram!  \ With zeros
\         1+             \ Next address
\     loop
\     drop
\ ;

: mk16kiram ( ram# -- )
    \ Initialize a 16K SPRAM block as an IRAM

    dup zeroram      \ First clear memory
    $2000 *           \ IRAM address in SPRAM
    $B3DD over sram!   \ Eight nibble IRAM Module ID Field value
    1+ $DDDE swap sram!
;

: mk32kiram ( ram# -- )
    \ Initialize a 32K SPRAM block as an IRAM

    dup 6 u> if       \ 32K isn't last 16K block
        dup zeroram    \ First clear memory
        dup 1+ zeroram  \ First clear memory
        $2000 *          \ IRAM address in SPRAM
        $B3DD over sram!  \ Eight nibble IRAM Module ID Field value
        1+ $DDDE swap sram!
    then
;

: mk64kiram ( ram# -- )
    \ Initialize a 64K SPRAM block as IRAM

    dup 4 u> if       \ 64K isn't last 48K blocks
        dup zeroram    \ First clear memory
        dup 1+ zeroram  \ First clear memory
        dup 2 + zeroram  \ First clear memory
        dup 3 + zeroram   \ First clear memory
        $2000 *            \ IRAM address in SPRAM
        $B3DD over sram!    \ Eight nibble IRAM Module ID Field value
        1+ $DDDE swap sram!
    then
;


\ #######   File Header   #####################################

: fhdrname ( ud_nibble_addr --  )
    \ Output header name
    
    8 0 ?do
        2dup 2 peek drop \ Read two nibbles, drop double top
        $FF and emitchar  \ Output character
        2 0 d+             \ Increment to next character address
    loop
    2drop
;

: fhdrtype ( ud_nibble_addr -- u_nibbles )
    \ Return the 4 nibble file type given the header address
    
    16 s>d d+       \ Skip 16 nibble file name
    4 peek drop      \ Extract File Type, d>s
;

: fhdrflag ( ud_nibble_addr -- u_nibbles )
    \ Return the 1 nibble file flags given the header address
    
    20 s>d d+       \ Skip 16 nibble file name, 4 nibble file type
    1 peek drop      \ Extract File Flags, d>s
;

: fhdrcode ( ud_nibble_addr -- u_nibbles )
    \ Return the 1 nibble file copy codes given the header address
    
    21 s>d d+       \ Skip to the Copy Code field
    1 peek drop      \ Extract File Copy Code, d>s
;

: fhdrlen ( ud_nibble_addr -- ud_nibbles )
    \ Return the 5 nibble file type as a double given the header address
    
    32 s>d d+       \ Skip to File Chain Length
    5 peek           \ Extract File Type
;

: fhdrnext ( ud_nibble_addr -- ud_nibbleaddr )
    \ Return the next header address given the current header address
    
    2dup fhdrlen d+ \ Add offset to next header
    29 s>d d+        \ Add header length
;


\ #######   File Access   #####################################
