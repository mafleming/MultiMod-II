\ #######   Warm Boot   ###########################################

\ Support For FPGA Warm Boot


: warmboot ( num -- )
    \ num is 0 .. 7
    \ bits [1:0] select bitstream image
    \ bit [2] = 1 triggers warm boot
    $0200 io!     \ Write to BOOTCTL register
;

