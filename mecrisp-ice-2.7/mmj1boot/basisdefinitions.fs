
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

: sram@ ( addr -- x ) $800 io! $80 io@ ;
: sram! ( x addr -- ) $800 io! $80 io! ;
