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
The J1A module definition was modified to include an output pin for the external clock oscillator enable signal. The ```clk_en``` signal should be set to logic high by default.

The pins associated with the USB data signals and pullup are ```usb_dn```, ```usb_dp```, and ```usb_pu```. These signals are mapped to FPGA pins not enumerated in the default J1a CPU.

Likewise, the ```to71```, ```from71```, and ```ctrl71``` outputs that control the tristate level shifters are added to the default J1a CPU interface. These also should be set to logic low so that the buffers are tri-stated. These may not be necessary since the enable inputs are pulled low and unconfigured IO pins are likely left floating.

> ***Check this in the FPGA data sheet***

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

    output spi_cs,      // SPI Flash
    output spi_clk,
    inout  spi_mosi,
    inout  spi_miso,
    inout  spi_io2,
    inout  spi_io3,

    inout  usb_dp,    // USB pins
    inout  usb_dn,
    output usb_dp_pu,
    input  usb_activ,

    output to71,  // Tristate buffer control
    output from71,
    output ctrl71,

    input  din71,  // HP71B bus control signals
    input  cdn71,
    input  strn71
);
```

The three RGB pins could be removed for power saving reasons, though doing so would require further modification to the Verilog source to remove references to the signals. The pins are unconnected to any external device.
