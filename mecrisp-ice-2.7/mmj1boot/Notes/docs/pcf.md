
# The MultiMod II Pin Definitions
The pin assignments in the ```multimod-evt.pcf``` file map the signals declared in the module header of ```j1a.v``` to actual pins in the Lattice FPGA SG48 package.

## Pin Assignments
The ```pmod_(1,2,3,4)``` names match those declared in the j1a.v module declaration and will need to be renamed in both files to something like the ```Bio(0,1,2,3)``` names found on the MultiMod II schematic.

```
set_io rgb0 39
set_io rgb1 40
set_io rgb2 41

set_io pmod_1 46
set_io pmod_2 47
set_io pmod_3 45
set_io pmod_4 44

set_io data_0 46
set_io data_1 47
set_io data_2 45
set_io data_3 44

set_io to71   48
set_io from71 42
set_io ctrl71 38

set_io din71  34
set_io cdn71  36
set_io strn71 37

set_io spi_mosi 17
set_io spi_miso 14
set_io spi_clk  15
set_io spi_io2  20
set_io spi_io3  13
set_io spi_cs   16

set_io usb_dn    26
set_io usb_dp    27
set_io usb_dp_pu 31

set_io clki    25
set_io clki_en 35
```

## Trivia
In the original MultiMod II design the pins for ```clki``` and ```clki_en``` were swapped. This worked fine for the mecrisp-ice design and so four test boards were ordered. It quickly turned out after receiving the test boards that the two candidate bootloader designs could not be synthesized using this pin assignment, and so the pins were swapped, new boards were orderd, and the old test boards were scrapped.