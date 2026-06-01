
\ Definitions in high-level Forth that can be compiled by the small
\ nucleus itself. They are included into the bitstream for default.

\ #######   CORE   ############################################

: [']
    '
; immediate 0 foldable

: [char]
    char
; immediate 0 foldable

: (
    [char] ) parse 2drop
; immediate 0 foldable

: u>= ( u1 u2 -- ? ) u< invert ; 2 foldable
: u<= ( u1 u2 -- ? ) u> invert ; 2 foldable
: >=  ( n1 n2 -- ? )  < invert ; 2 foldable
: <=  ( n1 n2 -- ? )  > invert ; 2 foldable

: else
    postpone ahead
    swap
    postpone then
; immediate

: while
    postpone if
    swap
; immediate

: repeat
     postpone again
     postpone then
; immediate

: create ( "<name>" -- ; -- addr )
    :
    here 2 cells + postpone literal
    postpone ;
;

: buffer: ( u "<name>" -- ; -- addr )
   create allot 0 foldable
;

: >body ( addr -- addr' )
    @ -1 1 rshift and \ Remove the literal opcode MSB
;

: m* ( n1 n2 -- d )
    2dup xor >r
    abs swap abs um*
    r> 0< if dnegate then
; 2 foldable

: variable ( x "name" -- ; -- addr )
    create ,
    0 foldable
;

: constant ( x "name" -- ; -- x ) : postpone literal postpone ; 0 foldable ;

: sgn ( u1 n1 -- n2 ) \ n2 is u1 with the sign of n1
    0< if negate then
; 2 foldable

\ Divide d1 by n1, giving the symmetric quotient n3 and the remainder
\ n2.
: sm/rem ( d1 n1 -- n2 n3 )
    2dup xor >r     \ combined sign, for quotient
    over >r         \ sign of dividend, for remainder
    abs >r dabs r>
    um/mod          ( remainder quotient )
    swap r> sgn     \ apply to remainder
    swap r> sgn     \ apply to quotient
; 3 foldable

\ Divide d1 by n1, giving the floored quotient n3 and the remainder n2.
\ Adapted from hForth
: fm/mod ( d1 n1 -- n2 n3 )
    dup >r 2dup xor >r
    >r dabs r@ abs
    um/mod
    r> 0< if
        swap negate swap
    then
    r> 0< if
        negate         \ negative quotient
        over if
            r@ rot - swap 1-
        then
    then
    r> drop
; 3 foldable

: */mod ( n1 n2 n3 -- n4 n5 ) >r m* r> sm/rem ; 3 foldable
: */    ( n1 n2 n3 -- n4 )    */mod nip ; 3 foldable

: spaces ( n -- )
    begin
        dup 0>
    while
        space 1-
    repeat
    drop
;

( Pictured numeric output                    JCB 08:06 07/18/14)
\ Adapted from hForth

\ "The size of the pictured numeric output string buffer shall
\ be at least (2*n) + 2 characters, where n is the number of
\ bits in a cell."

create BUF0
16 cells 2 + 128 max
allot here constant BUF

0 variable hld

: <# ( -- )
    BUF hld !
;

: hold ( c -- )
    hld @ 1- dup hld ! c!
;

: sign ( n -- )
    0< if
        [char] - hold
    then
;

: .digit ( u -- c )
  9 over <
  [char] A [char] 9 1 + -
  and +
  [char] 0 +
;

: # ( ud -- ud* )
    0 base @ um/mod >r base @ um/mod swap
    .digit hold r>
;

: #s ( ud -- 0 0 )
    begin
        #
        2dup d0=
    until
;

: #> ( ud -- addr len )
    2drop hld @ BUF over -
;

: (d.) ( d -- addr len )
    dup >r dabs <# #s r> sign #>
;

: ud. ( ud -- )
    <# #s #> type space
;

: d. ( d -- )
    (d.) type space
;

: . ( n -- )
    s>d d.
;

: u. ( u -- )
    0 d.
;

: rtype ( caddr u1 u2 -- ) \ display character string specified by caddr u1
                           \ in a field u2 characters wide.
  2dup u< if over - spaces else drop then
  type
;

: d.r ( d length -- )
    >r (d.)
    r> rtype
;

: .r ( n length -- )
    >r s>d r> d.r
;

: u.r ( u length -- )
    0 swap d.r
;

( Memory operations                          JCB 18:02 05/31/15)

: move ( addr1 addr2 u -- )
    >r 2dup u< if
        r> cmove>
    else
        r> cmove
    then
;

: /mod ( n1 n2 -- n3 n4 ) >r s>d r> sm/rem ; 2 foldable
: /    ( n1 n2 -- n3 )    /mod nip ; 2 foldable
: mod  ( n1 n2 -- n3 )    /mod drop ; 2 foldable

: ."
    [char] " parse
    state @ if
        postpone sliteral
        postpone type
    else
        type
    then
; immediate 0 foldable

\ #######   CORE EXT   ########################################

: pad ( -- addr )
    here aligned
;

: within ( n1|u1 n2|u2 n3|u3 -- flag ) over - >r - r> u< ; 3 foldable

: s"
    [char] " parse
    state @ if
        postpone sliteral
    then
; immediate

( CASE                                       JCB 09:15 07/18/14)
\ From ANS specification A.3.2.3.2

: case ( -- 0 ) 0 ; immediate  ( init count of ofs )

: of  ( #of -- orig #of+1 / x -- )
    1+    ( count ofs )
    >r    ( move off the stack in case the control-flow )
          ( stack is the data stack. )
    postpone over  postpone = ( copy and test case value)
    postpone if    ( add orig to control flow stack )
    postpone drop  ( discards case value if = )
    r>             ( we can bring count back now )
; immediate

: endof ( orig1 #of -- orig2 #of )
    >r   ( move off the stack in case the control-flow )
         ( stack is the data stack. )
    postpone else
    r>   ( we can bring count back now )
; immediate

: endcase  ( orig1..orign #of -- )
    postpone drop  ( discard case value )
    0 ?do
      postpone then
    loop
; immediate

\ #######   DICTIONARY   ######################################

: cornerstone ( "name" -- )
  create
    forth 2@        \ preserve FORTH and DP after this
    , 2 cells + ,
  does>
    2@ forth 2! \ restore FORTH and DP
;

: unused ( -- u ) $3C00 here - ;

: ticks ( -- u ) $40 io@ ;

: nextirq ( cycles -- ) \ Trigger the next interrupt u cycles after the last one.
  $4000 io@  \ Read current tick
  -           \ Subtract the cycles already elapsed
  8 -          \ Correction for the cycles neccessary to do this
  invert        \ Timer counts up to zero to trigger the interrupt
  $4000 io!      \ Prepare timer for the next irq
;

: ms    ( u -- ) 0 do 1200 0 do loop loop ; \ 10 cycles per loop run. 1 ms * 12 MHz / 10
: leds  ( x -- ) 8 io! ;

: randombit ( -- 0 | 1 ) $2000 io@ 2 rshift 1 and ;
: random ( -- x ) 0  16 0 do 2* randombit or 100 0 do loop loop ;

: sram@ ( addr -- x ) $21 io! $20 io@ ;
: sram! ( x addr -- ) $21 io! $20 io! ;

: h71@ ( daddr -- x ) swap $51 io! $52 io! $50 io@ ;
: h71! ( x daddr -- ) swap $51 io! $52 io! $50 io! ;

\ -------------------------------------------------------------
\  Double tools
\ -------------------------------------------------------------

: 2or  ( d1 d2 -- d ) >r swap >r or  r> r> or  ; 4 foldable
: 2and ( d1 d2 -- d ) >r swap >r and r> r> and ; 4 foldable
: 2xor ( d1 d2 -- d ) >r swap >r xor r> r> xor ; 4 foldable

: d0<   ( d -- ? ) nip 0< ; 2 foldable

: d= ( x0 x1 y0 y1 -- ? )

  swap ( x0 x1 y1 y0 )
  >r   ( x0 x1 y1 R: y0 )
  =    ( x0 x1=y1 R: y0 )
  swap ( x1=y1 x0 R: y0 )
  r>   ( x1=y1 x0 y0 )
  =    ( x1=y1 x0=y0 )
  and
; 4 foldable

: d<> d= not ; 4 foldable

: d2/  ( x1 x2 -- x1' x2' ) >r 1 rshift r@ 8 cells 1- lshift or r> 2/       ; 2 foldable
: dshr ( x1 x2 -- x1' x2' ) >r 1 rshift r@ 8 cells 1- lshift or r> 1 rshift ; 2 foldable

\ : 2lshift  ( ud u -- ud* ) begin dup while >r d2*  r> 1- repeat drop ; 3 foldable
\ : 2arshift (  d u --  d* ) begin dup while >r d2/  r> 1- repeat drop ; 3 foldable
\ : 2rshift  ( ud u -- ud* ) begin dup while >r dshr r> 1- repeat drop ; 3 foldable

: 2lshift ( low high u -- )
  dup >r ( low high u R: u )
  lshift ( low high* )
  over 8 cells r@ - rshift or
  over r@ 8 cells - lshift or
  swap r> lshift swap
; 3 foldable

: 2rshift ( low high u -- )
  >r swap ( high low R: u )
  r@ rshift
  over 8 cells r@ - lshift or
  over r@ 8 cells - rshift or
  swap
  r> rshift
; 3 foldable

: 2arshift ( low high u -- )
  dup >r 8 cells u< ( low high R: u )
  if
    swap ( high low R: u )
    r@ rshift
    over 8 cells r@ - lshift or
  else
    nip dup r@ 8 cells - arshift
  then
  swap r> arshift
; 3 foldable

: 2nip ( d1 d2 -- d2 )
  >r nip nip r>
; 4 foldable

: 2rot ( d1 d2 d3 -- d2 d3 d1 )
  >r >r ( d1 d2 R: d3 )
  2swap ( d2 d1 R: d3 )
  r> r> ( d2 d1 d3 )
  2swap ( d2 d3 d1 )
; 6 foldable

: d<            \ ( al ah bl bh -- flag )
    rot         \ al bl bh ah
    2dup =
    if
        2drop u<
    else
        > nip nip
    then
; 4 foldable

: d>  ( d1 d2 -- ? ) 2swap d< ; 4 foldable
: d>= ( d1 d2 -- ? ) d< not   ; 4 foldable
: d<= ( d1 d2 -- ? ) d> not   ; 4 foldable

: dmin ( d1 d2 -- d ) 2over 2over d< if 2drop else 2nip then ; 4 foldable
: dmax ( d1 d2 -- d ) 2over 2over d< if 2nip else 2drop then ; 4 foldable

: du<           \ ( al ah bl bh -- flag )
    rot         \ al bl bh ah
    2dup =
    if
        2drop u<
    else
        u> nip nip
    then
; 4 foldable

: du>  ( d1 d2 -- ? ) 2swap du< ; 4 foldable
: du>= ( d1 d2 -- ? ) du< not   ; 4 foldable
: du<= ( d1 d2 -- ? ) du> not   ; 4 foldable

\ -------------------------------------------------------------
\  Fixpoint output
\ -------------------------------------------------------------

: hold< ( c -- ) \ Add a character at the end of the number string
  hld @   dup 1- dup hld !    BUF hld @ -  move
  BUF 1- c!
;

: f# ( u -- u ) base @ um* .digit hold< ;

: f.n ( f n -- ) ( f-Low f-High n -- ) \ Prints a s15.16 number

  >r ( Low High R: n )

  dup 0< if [char] - emit then
  dabs
  ( uLow uHigh )
  0 <# #s   ( uLow 0 0 )
  drop swap ( 0 uLow )

  [char] , hold<
  r> 0 ?do f# loop

  #> type space
;

: f. ( f -- ) 8 cells f.n ;

\ -------------------------------------------------------------
\  Fixpoint calculations
\ -------------------------------------------------------------

: 2variable ( d -- ) create , , 0 foldable ;
\ : 2constant ( d -- ) create , , 0 foldable does> 2@ ;
: 2constant ( d -- ) swap : postpone literal postpone literal postpone ; 0 foldable ;

: s>f ( n -- f ) 0 swap ; 1 foldable  \ Signed integer --> Fixpoint s15.16
\ : f>s ( f -- n ) nip    ; 2 foldable  \ Fixpoint s15.16 --> Signed integer

: f* ( f1 f2 -- f )

        dup >r dabs
  2swap dup >r dabs

            ( d c b a )
  swap >r   ( d c a R: b )
  2dup *    ( d c a ac R: b )
  >r        ( d c a R: b ac )
  >r        ( d c R: b ac a )
  over      ( d c d R: b ac a )
  r> um*    ( d c L H R: b ac )
  r> +      ( d c L H' R: b )
  rot       ( d L H' c R: b )
  r@        ( d L H' c b R: b )
  um* d+    ( d L' H'' R: b )
  rot       ( L' H'' d R: b )
  r>        ( L' H'' d b )
  um* nip 0 ( L' H'' db 0 )
  d+        ( L'' H''' )

  r> r> xor 0< if dnegate then

; 4 foldable

0. 2variable dividend
0. 2variable shift
0. 2variable divisor

: (ud/mod) ( -- )

  16 cells
  begin

    \ Shift the long chain of four cells.

       dividend cell+ @ dup 8 cells 1- rshift >r 2*    dividend cell+ !
    r> dividend       @ dup 8 cells 1- rshift >r 2* or dividend       !
    r>    shift cell+ @ dup 8 cells 1- rshift >r 2* or    shift cell+ !
    r>    shift       @                          2* or    shift       !

    \ Subtract divisor when shifted out value is large enough

    shift 2@ divisor 2@  du>=

    if \ Greater or Equal: Subtract !
      shift 2@ divisor 2@ d- shift 2!
      dividend cell+ @ 1+ dividend cell+ !
    then

    1- dup 0=
  until
  drop
;

: ud/mod ( ud1 ud2 -- ud-rem ud-div )

     divisor 2!
  0. shift 2!
     dividend 2!

  (ud/mod)

  shift 2@
  dividend 2@

; 4 foldable

: f/ ( f1 f2 -- f )

  dup >r dabs  divisor 2!
  dup >r dabs  0 Shift 2! 0 swap dividend 2!

  (ud/mod)

  dividend 2@
  r> r> xor 0< if dnegate then

; 4 foldable

\ #######   DUMP   ############################################

: dump
    ?dup
    if
        1- 4 rshift 1+
        0 do
            cr dup dup .x space space
            16 0 do
                dup c@ .x2 1+
            loop
            space swap
            16 0 do
                dup c@ dup bl 127 within invert if
                    drop [char] .
                then
                emit 1+
            loop
            drop
        loop
    then
    drop
;

\ #######   INSIGHT   #########################################


( Deep insight into stack, dictionary and code )
( Matthias Koch )

: .s ( -- )
  \ Save initial depth
  depth dup >r

  \ Flush stack contents to temporary storage
  begin
    dup
  while
    1-
    swap
    over cells pad + !
  repeat
  drop

  \ Print original depth
  ." [ "
  r@ .x2
  ." ] "

  \ Print all elements in reverse order
  r@
  begin
    dup
  while
    r@ over - cells pad + @ .x
    1-
  repeat
  drop

  \ Restore original stack
  0
  begin
    dup r@ u<
  while
    dup cells pad + @ swap
    1+
  repeat
  rdrop
  drop
;

: insight ( -- )  ( Long listing of everything inside of the dictionary structure )
    base @ hex cr
    forth @
    begin
        dup
    while
         ." Addr: "     dup .x
        ."  Link: "     dup link@ .x
        ."  Flags: "    dup cell+ c@ 128 and if ." I " else ." - " then
                        dup @ 7 and ?dup if 1- u. else ." - " then
        ."  Code: "     dup cell+ count 127 and + aligned .x
        space           dup cell+ count 127 and type
        link@ cr
    repeat
    drop
    base !
;

0 variable disasm-$    ( Current position for disassembling )
0 variable disasm-cont ( Continue up to this position )

: name. ( Address -- )  ( If the address is Code-Start of a dictionary word, it gets named. )

  dup ['] s, 24 + = \ Is this a string literal ?
  if
    ."   --> s" [char] " emit space
    disasm-$ @ count type
    [char] " emit

    disasm-$ @ c@ 1+ aligned disasm-$ +!
    drop exit
  then

  >r
  forth @
  begin
    dup
  while
    dup cell+ count 127 and + aligned ( Dictionary Codestart )
      r@ = if ."   --> " dup cell+ count 127 and type then
    link@
  repeat
  drop r>

  $000E =                                  \ A call to execute
  disasm-$ @ 2 cells - @ $C000 and $C000 =  \ after a literal which has bit $4000 set means:
  and                                        \ Memory fetch.
  if
    ."   --> " disasm-$ @ 2 cells - @ $3FFF and .x ." @"
  then
;

: alu. ( Opcode -- ) ( If this opcode is from an one-opcode definition, it gets named. This way inlined ALUs get a proper description. )

  dup $6127 = if ." >r"    drop exit then
  dup $6B11 = if ." r@"    drop exit then
  dup $6B1D = if ." r>"    drop exit then
  dup $600C = if ." rdrop" drop exit then

  $FF73 and
  >r
  forth @
  begin
    dup
  while
    dup cell+ count 127 and + aligned @ ( Dictionary First-Opcode )
        dup $E080 and $6080 =
        if
          $FF73 and r@ = if rdrop cell+ count 127 and type space exit then
        else
          drop
        then

    link@
  repeat
  drop r> drop
;


: memstamp ( Addr -- ) dup .x ." : " @ .x ."   " ; ( Shows a memory location nicely )

: disasm-step ( -- )
  disasm-$ @ memstamp
  disasm-$ @ @        ( Fetch next opcode )
  1 cells disasm-$ +! ( Increment position )

  dup $8000 and         if ." Imm  " $7FFF and       dup .x 6 spaces                      .x       exit then ( Immediate )
  dup $E000 and $0000 = if ." Jmp  " $1FFF and cells dup                                  .x name. exit then ( Branch )
  dup $E000 and $2000 = if ." JZ   " $1FFF and cells disasm-cont @ over max disasm-cont ! .x       exit then ( 0-Branch )
  dup $E000 and $4000 = if ." Call " $1FFF and cells dup                                  .x name. exit then ( Call )
                           ." Alu"   13 spaces dup alu. $80 and if ." exit" then                             ( ALU )
;

: seec ( -- ) ( Continues to see )
  base @ hex cr
  0 disasm-cont !
  begin
    disasm-$ @ @
    dup  $E080 and $6080 =           ( Loop terminates with ret )
    swap $E000 and 0= or             ( or when an unconditional jump is reached. )
    disasm-$ @ disasm-cont @ u>= and ( Do not stop when there has been a conditional jump further )

    disasm-step cr
  until

  base !
;

: see ( -- ) ( Takes name of definition and shows its contents from beginning to first ret )
  ' disasm-$ !
  seec
;

cornerstone new

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
    1 $28 io!   \ Deselect flash CS/ = 1
;

: spixbit ( x -- y )
    \ Output data in high byte, assemble input in low byte
    dup 0< 2 and        \ extract MS bit
    dup $28 io!          \ lower SCK, update MOSI
    4 + $28 io!           \ raise SCK
    2*                     \ next bit
    $30 io@ +               \ read MISO, accumulate
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
\ #######   Warm Boot   ###########################################

\ Support For FPGA Warm Boot


: warmboot ( num -- )
    \ num is 0 .. 7
    \ bits [1:0] select bitstream image
    \ bit [2] = 1 triggers warm boot
    $a0 io!     \ Write to BOOTCTL register
;

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

    dup 6 u> if     \ 32K won't fit last 16K block
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

    dup 4 u> if     \ 64K won't fit last 16K block
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
    \ ram# 0~7, 16K block is SPRAM
    \ sector16k 32~127, 16K block in flash ( 2 MB )

    dup bitfence 1- u> if    \ Never overwrite bitstream !

        swap $2000 * swap      \ ( ram_addr sector16k -- )
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

    over 6 u> if     \ 32K isn't last 16K block
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

    over 4 u> if    \ 64K won't fit in last 3 16K blocks
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

    $2000 *         \ Starting address in SPRAM
    $2000 0 ?do      \ Fill 8K of 16-bit words
        $FF over sram!  \ With ones
        1+             \ Next address
    loop
    drop
;

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

