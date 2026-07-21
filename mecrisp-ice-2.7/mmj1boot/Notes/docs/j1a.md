# The J1A Forth CPU Verilog File

## iCE40UP5K FPGA Pin Assignments
The various MultiMod II signal connections are given in the table below. Signals are grouped by function rather than IO block/pin number.

**HP-71B Bus Signals**
```
Signal         FPGA Name           SG48 Pin
------------   -----------------   ------------
Data IO 0      IOB_0a              46
Data IO 1      IOB_2a              47
Data IO 2      IOB_5b              45
Data IO 3      IOB_3b_G6           44
StrobeN        IOT_45a_G1          37
Cmd/DatN       IOT_48b             36
DaisyIn        IOT_44b             34
```
**HP-71B Bus Level Shifter Control Signals**
```
Signal         FPGA Name           SG48 Pin
------------   -----------------   ------------
Data Out Enb   IOB_4a              48
Data In Enb    IOT_51a             42
Cntrl In Enb   IOB_50b             38
```
**External Device Control Signals**
```
Signal         FPGA Name           SG48 Pin
------------   -----------------   ------------
USB Pos        IOT_38b             27
USB Neg        IOT39a              26
USB Active     IOB_6a               2
USB Pullup     IOT_42b             31
Osc Enable     IOT_46b_G0          35
Osc Output     IOT_36b             25
```
**External Flash Storage Signals**
```
Signal         FPGA Name           SG48 Pin       Flash         Pin
------------   -----------------   ------------   -----------   -----
SPI_SO         IOB_32a             14             DI (IO0)       5
SPI_SI         IOB_33b             17             DO (IO1)       2
SPI_SCK        IOB_34a             15             CLK            6
SPI_SS         IOB_35b             16             /CS            1
GPIO           IOB_24a             13             IO3 (/HOLD)    7
GPIO           IOB_25b_G3          20             IO2 (/WP)      3
```

## CPU External Signal Modifications
The J1A module definition was modified to include an output pin for the external clock oscillator enable signal. The `clk_en` signal should be set to logic high by default.

The pins associated with the USB data signals and pullup are `usb_dn`, `usb_dp`, and `usb_pu`. Additionally there is a `usb_activ` signal that indicates power is being supplied by the USB C connector and therefore serves as a host detection signal.

The header is 
```
module top (
    input  clki,   // 48 MHz clock input
    output clk_en, // Enable external clock

    inout  data_1,   // Four user pins
    inout  data_2,
    inout  data_3,
    inout  data_4,

    output rgb0,   // LED outputs
    output rgb1,
    output rgb2,

    output spi_cs,    // SPI Flash
    output spi_clk,
    inout  spi_miso,
    inout  spi_mosi,
    inout  spi_io2,
    inout  spi_io3,

    inout  usb_dp,    // USB pins
    inout  usb_dn,
    output usb_dp_pu,
    input  usb_activ

);
```
Note that the four user pins `data_(1,2,3,4)` are bidirectional with separate input and output registers in the I/O address space, along with a direction register that indicates whether they are inputs or outputs.

The three RGB pins could be removed for power saving reasons, though doing so would require further modification to the Verilog source to remove references to the signals. The pins are unconnected to any external device.

Note that the SPI signals `spi_miso`, `spi_mosi`, `spi_io2`, and `spi_io3` are defined as bidirectional. These four signals now use the same **SB_IO** definitions as the four data signals. Normally `spi_miso` is set as an input and the other three are outputs. The `spi_io2` (WP/) and `spi_io3` (HOLD/) default to logic 1 while the SPI flash communication is in Standard transfer mode.

One set of external signals currently missing from the module header are those that interface to control inputs from the 71B bus. These signals are named and assigned to FPGA pins in the ```pcf``` file, and eventually need to be incorporated for a J1A Forth implementation operating in production mode within the 71B card reader cavity.

## CPU Internal Modifications

Changes to the j1a Verilog design were made include

- Support for bidirectional SPI signals to enable Dual and Quad data transfers,
- Warm Boot support to allow both bootloader and HP-71B embedded configurations,
- Changes to the I/O address space to support the above enhancements.

The I/O address modifications are

```
Address   Read Access   Write Access
-------   -----------   ------------
$0100     SPI signals   SPI signals
$0101                   SPIO direction
$0200     USB status    Warm Boot Ctl
```

```
 IO Address $0100, SPI Read/Write
 +------+------+------+------+------+------+
 | CLK  | CS   | IO3  | IO2  | MOSI | MISO |
 +-----5+-----4+-----3+-----2+-----1+-----0+

 IO Address $0101, SPI Direction 1:output, 0:input
 +------+------+------+------+
 | IO3  | IO2  | MOSI | MISO |
 +-----3+-----2+-----1+-----0+
```

The SPI signals are arranged in such a fashion to make it easy to support Dual and Quad mode transfers. The SPI Direction register is by default set to the Standard transfer mode configuration in which **MISO** is an input from the flash device and the remaining three signals are outputs from the FPGA to the flash device.

```
 IO Address $0200
 USB Active/State Read       Boot Write
 +------+------+------+      +------+------+------+
 | ACTV | P_TX | N_TX |      | BOOT | S1   | S0   |
 +-----2+-----1+-----0+      +-----2+-----1+-----0+
```

The Warm Boot control register `BOOTCTL` specifies which of four bitstream images are to be loaded using the lower two bits of the register ('00' for the bootloader, '01' for the HP-71B embedded configuration). Bit 02, when set to logic 1, will initiate the boot sequence.

The USB State includes the status of the USB data pins `USB-P` and `USB-N` along with the `usb_activ` signal that indicates power is present on the USB C connector.

