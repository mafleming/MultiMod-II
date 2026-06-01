
\ #######   MEMORY   ##########################################

: unused
    $4000 here -
;

\ #######   IO   ##############################################

\  ------------------------------------------------------------
\    Useful Low-Level IO definitions
\  ------------------------------------------------------------
\
\     Addr  Bit READ            WRITE
\
\     0001  0   Port A in
\     0002  1   Port A out      Port A out
\     0004  2   Port A dir      Port A dir
\     0008  3
\
\     0010  4   Port B in
\     0020  5   Port B out      Port B out
\     0040  6   Port B dir      Port B dir
\     0080  7
\
\     0100  8   Port C in
\     0200  9   Port C out      Port C out
\     0400  10  Port C dir      Port C dir
\     0800  11
\
\     1000  12  UART RX         UART TX
\     2000  13  misc.in
\     4000  14  ticks           set ticks
\     8000  15  cycles
\
\
\ Contents of misc.out and misc.in:
\
\  Bitmask Bit   misc.in
\
\    0001    0   UART Ready to Transmit
\    0002    1   UART Character received
\    0004    2
\    0008    3
\    0010    4
\    0020    5
\    0040    6
\    0080    7
\    0100    8
\    0200    9
\    0400   10
\    0800   11
\    1000   12
\    2000   13
\    4000   14
\    8000   15
\

: ticks  ( -- u ) $4000 io@ ;
: cycles ( -- u ) $8000 io@ ;
