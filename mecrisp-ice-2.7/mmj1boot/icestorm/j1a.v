
`default_nettype none

`include "../common-verilog/j1-universal-16kb-quickstore.v"

`include "../common-verilog/usb_cdc/usb_cdc.v"
`include "../common-verilog/usb_cdc/bulk_endp.v"
`include "../common-verilog/usb_cdc/ctrl_endp.v"
`include "../common-verilog/usb_cdc/phy_rx.v"
`include "../common-verilog/usb_cdc/phy_tx.v"
`include "../common-verilog/usb_cdc/sie.v"

module top (
    input  clki,   // 48 MHz clock input
    output clk_en, // Enable external clock

    inout  pmod_1,   // Four user pins
    inout  pmod_2,
    inout  pmod_3,
    inout  pmod_4,

    output rgb0,   // LED outputs
    output rgb1,
    output rgb2,

    output spi_mosi,    // SPI Flash
    input  spi_miso,
    output spi_clk,
    output spi_io2,
    output spi_io3,
    output spi_cs,

    inout  usb_dp,    // USB pins
    inout  usb_dn,
    output usb_dp_pu
);

  // ######   Clock   #########################################

    reg [1:0] divider;

    always @(posedge clki) divider <= divider + 1;

    wire clk_usb = clki;       // 48 MHz
    wire clk     = divider[1]; // 12 MHz

  // ######   Reset logic   ###################################

    wire button = 1'b1;

    reg [7:0] reset_cnt = 0;
    wire resetq = &reset_cnt;

    always @(posedge clk) begin
        clk_en <= 1'b1;        // Always enabled
        if (button) reset_cnt <= reset_cnt + !resetq;
        else        reset_cnt <= 0;
    end

  // ######   Bus   ###########################################

    wire io_rd, io_wr;
    wire [15:0] io_addr;
    wire [15:0] io_dout;
    wire [15:0] io_din;

    reg interrupt = 0;

  // ######   Processor   #####################################

    j1 #( .MEMWORDS(7680) ) _j1( // 15 kb Memory

        .clk(clk),
        .resetq(resetq),

        .io_rd(io_rd),
        .io_wr(io_wr),
        .io_dout(io_dout),
        .io_din(io_din),
        .io_addr(io_addr),

        .interrupt_request(interrupt)
    );

  // ######   SPI   ###########################################
    reg [2:0] spios;
    assign {spi_clk, spi_mosi, spi_cs} = spios;

/* -----\/----- EXCLUDED -----\/-----
  SB_IO #(.PIN_TYPE(6'b1010_01)) spio0 (.PACKAGE_PIN(spi_miso), .D_OUT_0(spi_out[0]),
   .D_IN_0(spi_in[0]), .OUTPUT_ENABLE(1'b0));
  SB_IO #(.PIN_TYPE(6'b1010_01)) spio1 (.PACKAGE_PIN(spi_cs  ), .D_OUT_0(spi_out[1]),
   .D_IN_0(spi_in[1]), .OUTPUT_ENABLE(1'b1));
  SB_IO #(.PIN_TYPE(6'b1010_01)) spio2 (.PACKAGE_PIN(spi_clk ), .D_OUT_0(spi_out[2]),
   .D_IN_0(spi_in[2]), .OUTPUT_ENABLE(1'b1));
  SB_IO #(.PIN_TYPE(6'b1010_01)) spio3 (.PACKAGE_PIN(spi_mosi), .D_OUT_0(spi_out[3]),
   .D_IN_0(spi_in[3]), .OUTPUT_ENABLE(1'b1));
 -----/\----- EXCLUDED -----/\----- */

/* -----\/----- EXCLUDED -----\/-----
  SB_IO #(.PIN_TYPE(6'b1010_01)) spio4 (.PACKAGE_PIN(spi_io2), .D_OUT_0(1'b1),
   .D_IN_0(spi_in[4]), .OUTPUT_ENABLE(1'b1));
  SB_IO #(.PIN_TYPE(6'b1010_01)) spio5 (.PACKAGE_PIN(spi_io3), .D_OUT_0(1'b1),
   .D_IN_0(spi_in[5]), .OUTPUT_ENABLE(1'b1));
 -----/\----- EXCLUDED -----/\----- */
  
   

   
  // ######   Ticks   #########################################

  reg [15:0] ticks;

  wire [16:0] ticks_plus_1 = ticks + 1;

  always @(posedge clk)
    if (io_wr & (io_addr[15:00] == 16'h0040))
      ticks <= io_dout;
    else
      ticks <= ticks_plus_1;

  always @(posedge clk) // Generate interrupt on ticks overflow
    interrupt <= ticks_plus_1[16];

  // ######   PMOD   ##########################################

  reg  [3:0] pmod_dir;   // 1:output, 0:input
  reg  [3:0] pmod_out;
  wire [3:0] pmod_in;

  SB_IO #(.PIN_TYPE(6'b1010_01)) io0 (.PACKAGE_PIN(pmod_1), .D_OUT_0(pmod_out[0]), .D_IN_0(pmod_in[0]), .OUTPUT_ENABLE(pmod_dir[0]));
  SB_IO #(.PIN_TYPE(6'b1010_01)) io1 (.PACKAGE_PIN(pmod_2), .D_OUT_0(pmod_out[1]), .D_IN_0(pmod_in[1]), .OUTPUT_ENABLE(pmod_dir[1]));
  SB_IO #(.PIN_TYPE(6'b1010_01)) io2 (.PACKAGE_PIN(pmod_3), .D_OUT_0(pmod_out[2]), .D_IN_0(pmod_in[2]), .OUTPUT_ENABLE(pmod_dir[2]));
  SB_IO #(.PIN_TYPE(6'b1010_01)) io3 (.PACKAGE_PIN(pmod_4), .D_OUT_0(pmod_out[3]), .D_IN_0(pmod_in[3]), .OUTPUT_ENABLE(pmod_dir[3]));

  // ######   SRAM  CONTROLLER   ##############################

  

    reg sram_en1, sram_en2, sram_en3, sram_en4;
  

    always @(posedge clk)
    begin
    
        case (sram_addr[15:14])
            2'b00: begin
                sram_en1 <= 1'b1;
                sram_en2 <= 1'b0;
                sram_en3 <= 1'b0;
                sram_en4 <= 1'b0;
                end
            2'b01: begin
                sram_en1 <= 1'b0;
                sram_en2 <= 1'b1;
                sram_en3 <= 1'b0;
                sram_en4 <= 1'b0;
                end
            2'b10: begin
                sram_en1 <= 1'b0;
                sram_en2 <= 1'b0;
                sram_en3 <= 1'b1;
                sram_en4 <= 1'b0;
                end
            2'b11: begin
                sram_en1 <= 1'b0;
                sram_en2 <= 1'b0;
                sram_en3 <= 1'b0;
                sram_en4 <= 1'b1;
                end
            default: begin
                sram_en1 <= 1'b0;
                sram_en2 <= 1'b0;
                sram_en3 <= 1'b0;
                sram_en4 <= 1'b0;
                end
        endcase
    end


  // ######   SRAM   ##########################################

    // Registers controlling the four SPRAM blocks
    reg [15:0] sram_addr;

    // Registers used by the J1A Forth CPU
    reg [15:0] j1a_addr;
    reg [15:0] j1a_din;
    reg [15:0] j1a_dout;
    reg [ 3:0] j1a_mask;
  
    // Registers used by the HP-71B memory device controller
    reg [19:0] h71_addr;
    reg [ 3:0] h71_din;
    reg [ 3:0] h71_dout;
    reg [ 3:0] h71_mask;

    wire sram_wr = io_wr_sram_data | hp_wr_sram_data;

    wire [15:0] sram_out = sram_out_bank3 | sram_out_bank2 | sram_out_bank1 | sram_out_bank0;

    wire [15:0] sram_out_bank0, sram_out_bank1, sram_out_bank2, sram_out_bank3;
  
    wire [15:0] sram_in = hp_wr_sram_data == 1'b0 ? io_dout :
                      {io_dout[3:0], io_dout[3:0], io_dout[3:0], io_dout[3:0]};
  
    wire [3:0] sram_wrmask = hp_wr_sram_data == 1'b0 ? j1a_mask : h71_mask;

    SB_SPRAM256KA rambank0 (
        .DATAIN(sram_in),
        .ADDRESS(sram_addr[13:0]),
        .MASKWREN(sram_wrmask),
        .WREN(sram_wr),
        .CHIPSELECT(sram_en1),
//        .CHIPSELECT(sram_addr[15:14] == 2'b00),
        .CLOCK(clk),
        .STANDBY(1'b0),
        .SLEEP(~sram_en1),
//        .SLEEP(~(sram_addr[15:14] == 2'b00)),
//        .SLEEP(1'b0),
        .POWEROFF(1'b1),
        .DATAOUT(sram_out_bank0)
);

    SB_SPRAM256KA rambank1 (
        .DATAIN(sram_in),
        .ADDRESS(sram_addr[13:0]),
        .MASKWREN(sram_wrmask),
        .WREN(sram_wr),
        .CHIPSELECT(sram_en2),
//        .CHIPSELECT(sram_addr[15:14] == 2'b01),
        .CLOCK(clk),
        .STANDBY(1'b0),
        .SLEEP(~sram_en2),
//        .SLEEP(~(sram_addr[15:14] == 2'b01)),
//        .SLEEP(1'b0),
        .POWEROFF(1'b1),
        .DATAOUT(sram_out_bank1)
);

    SB_SPRAM256KA rambank2 (
        .DATAIN(sram_in),
        .ADDRESS(sram_addr[13:0]),
        .MASKWREN(sram_wrmask),
        .WREN(sram_wr),
        .CHIPSELECT(sram_en3),
//        .CHIPSELECT(sram_addr[15:14] == 2'b10),
        .CLOCK(clk),
        .STANDBY(1'b0),
        .SLEEP(~sram_en3),
//        .SLEEP(~(sram_addr[15:14] == 2'b10)),
//        .SLEEP(1'b0),
        .POWEROFF(1'b1),
        .DATAOUT(sram_out_bank2)
);

    SB_SPRAM256KA rambank3 (
        .DATAIN(sram_in),
        .ADDRESS(sram_addr[13:0]),
        .MASKWREN(sram_wrmask),
        .WREN(sram_wr),
        .CHIPSELECT(sram_en4),
//        .CHIPSELECT(sram_addr[15:14] == 2'b11),
        .CLOCK(clk),
        .STANDBY(1'b0),
        .SLEEP(~sram_en4),
//        .SLEEP(~(sram_addr[15:14] == 2'b11)),
//        .SLEEP(1'b0),
        .POWEROFF(1'b1),
        .DATAOUT(sram_out_bank3)
);

  // ######   USB-CDC terminal   ##############################

  assign usb_dp_pu = resetq;

  wire usb_p_tx;
  wire usb_n_tx;
  wire usb_p_rx;
  wire usb_n_rx;
  wire usb_tx_en;

   SB_IO #(
       .PIN_TYPE(6'b 1010_01), // PIN_OUTPUT_TRISTATE - PIN_INPUT
       .PULLUP(1'b 0)
   ) iobuf_usbp (
       .PACKAGE_PIN(usb_dp),
       .OUTPUT_ENABLE(usb_tx_en),
       .D_OUT_0(usb_p_tx),
       .D_IN_0(usb_p_rx)
   );

   SB_IO #(
       .PIN_TYPE(6'b 1010_01), // PIN_OUTPUT_TRISTATE - PIN_INPUT
       .PULLUP(1'b 0)
   ) iobuf_usbn (
       .PACKAGE_PIN(usb_dn),
       .OUTPUT_ENABLE(usb_tx_en),
       .D_OUT_0(usb_n_tx),
       .D_IN_0(usb_n_rx)
   );

  usb_cdc #(.VENDORID(16'h0483), .PRODUCTID(16'h5740), .BIT_SAMPLES(4), .USE_APP_CLK(1), .APP_CLK_RATIO(4)) _terminal
  (
    // Part running on 48 MHz:

    .clk_i(clk_usb),
    .tx_en_o(usb_tx_en),
    .tx_dp_o(usb_p_tx),
    .tx_dn_o(usb_n_tx),
    .rx_dp_i(usb_p_rx),
    .rx_dn_i(usb_n_rx),

    // Part running on 12 MHz:

    .app_clk_i(clk),
    .rstn_i(resetq),

    .out_data_o(terminal_data),
    .out_valid_o(terminal_valid),
    .out_ready_i(terminal_rd),

    .in_data_i(io_dout[7:0]),
    .in_ready_o(terminal_ready),
    .in_valid_i(terminal_wr)
  );

  wire terminal_valid, terminal_ready;
  wire [7:0] terminal_data;
  wire terminal_wr = io_wr & io_addr[12];
  wire terminal_rd = io_rd & io_addr[12];

  // ######   RING OSCILLATOR   ###############################

  wire [1:0] buffers_in, buffers_out;
  assign buffers_in = {buffers_out[0:0], ~buffers_out[1]};
  SB_LUT4 #(
          .LUT_INIT(16'd2)
  ) buffers [1:0] (
          .O(buffers_out),
          .I0(buffers_in),
          .I1(1'b0),
          .I2(1'b0),
          .I3(1'b0)
  );

  wire random = ~buffers_out[1];

  // ######   Blink   #########################################

  // Instantiate iCE40 LED driver hard logic.
  //
  // Note that it's possible to drive the LEDs directly,
  // however that is not current-limited and results in
  // overvolting the red LED.
  //
  // See also:
  // https://www.latticesemi.com/-/media/LatticeSemi/Documents/ApplicationNotes/IK/ICE40LEDDriverUsageGuide.ashx?document_id=50668

  reg [2:0] LEDS;

  SB_RGBA_DRV #(
      .CURRENT_MODE("0b1"),       // half current
      .RGB0_CURRENT("0b000011"),  // 4 mA
      .RGB1_CURRENT("0b000011"),  // 4 mA
      .RGB2_CURRENT("0b000011")   // 4 mA
  ) RGBA_DRIVER (
      .CURREN(1'b1),
      .RGBLEDEN(1'b1),
      .RGB1PWM(LEDS[0]),     // Red
      .RGB0PWM(LEDS[1]),     // Green
      .RGB2PWM(LEDS[2]),     // Blue
      .RGB0(rgb0),
      .RGB1(rgb1),
      .RGB2(rgb2)
  );

   
  // ######   Warm Boot Control ###############################
  // Add control register to j1a address space

   reg [2:0] BOOTCTL = 0;

  SB_WARMBOOT B_WARMBOOT(
      .BOOT(BOOTCTL[2]),
      .S0(BOOTCTL[0]),
      .S1(BOOTCTL[1])
  );
   
  // ######   HP71B Registers   ###############################

    // Try replacing with a RAM entity

    // Bit      0           1
    //  0       RAM         ROM
    //  1       Not EoC     End of Chain (EoC)
    //  2       N/A         O/S takeover, $00000
    //  3       N/A         Hard ROM, $E0000
/* -----\/----- EXCLUDED -----\/-----
    reg [3:0] id0;      // RAM Block 0
    reg [3:0] id1;
    reg [3:0] id2;
    reg [3:0] id3;
    reg [3:0] id4;
    reg [3:0] id5;
    reg [3:0] id6;
    reg [3:0] id7;      // RAM Block 7
 -----/\----- EXCLUDED -----/\----- */

    // Contains the upper five bits of the RAM/ROM block address
/* -----\/----- EXCLUDED -----\/-----
    reg [4:0] cfg0;      // RAM Block 0
    reg [4:0] cfg1;
    reg [4:0] cfg2;
    reg [4:0] cfg3;
    reg [4:0] cfg4;
    reg [4:0] cfg5;
    reg [4:0] cfg6;
    reg [4:0] cfg7;      // RAM Block 7
 -----/\----- EXCLUDED -----/\----- */


    // Control register to enable or write protect each 16K block
    // [7:0] - Block 7:0 visible, [15:08] - Block 7:0 write protect
    // Visible blocks are contiguous from the bottom
/* -----\/----- EXCLUDED -----\/-----
    reg [15:00] ctrl;
 -----/\----- EXCLUDED -----/\----- */
    
    // The actual memory device registers and card reader I/O registers
/* -----\/----- EXCLUDED -----\/-----
    reg [19:00] pc, dp;
 -----/\----- EXCLUDED -----/\----- */
    
    // 71B IO registers for Forth serial console
    // Status register bits:
/* -----\/----- EXCLUDED -----\/-----
    reg [07:00] cr_in, cr_out, cr_stat;
 -----/\----- EXCLUDED -----/\----- */

  // ######   IO Ports   ######################################

  /*        Bit READ            WRITE

    + ...0                      Write as usual
    + ...1                      _C_lear bits
    + ...2                      _S_et bits
    + ...3                      _T_oggle bits

      1000  12  UART RX         UART TX
      2000  13  UART Flags
  */
  
    wire io_rd_sram_data = io_rd & (io_addr[15:00] == 16'h0020);
    wire io_rd_sram_addr = io_rd & (io_addr[15:00] == 16'h0021);
    wire io_wr_sram_data = io_wr & (io_addr[15:00] == 16'h0020);
    wire io_wr_sram_addr = io_wr & (io_addr[15:00] == 16'h0021);
  
    wire hp_rd_sram_data  = io_rd & (io_addr[15:00] == 16'h0050);
    wire hp_rd_sram_addrl = io_rd & (io_addr[15:00] == 16'h0051);
    wire hp_rd_sram_addrh = io_rd & (io_addr[15:00] == 16'h0052);
    wire hp_wr_sram_data  = io_wr & (io_addr[15:00] == 16'h0050);
    wire hp_wr_sram_addrl = io_wr & (io_addr[15:00] == 16'h0051);
    wire hp_wr_sram_addrh = io_wr & (io_addr[15:00] == 16'h0052);

  assign io_din =

    (io_addr[15:00] == 16'h0008 ? {13'd0, LEDS}                       : 16'd0) |

    (io_addr[15:00] == 16'h0010 ? {12'd0, pmod_in}                    : 16'd0) |
    (io_addr[15:00] == 16'h0010 ? {12'd0, pmod_out}                   : 16'd0) |
    (io_addr[15:00] == 16'h0018 ? {12'd0, pmod_dir}                   : 16'd0) |

    (io_addr[15:00] == 16'h0020 ? sram_out                            : 16'd0) |
    (io_addr[15:00] == 16'h0021 ? sram_addr                           : 16'd0) |

    (io_addr[15:00] == 16'h0028 ? {13'd0, spios}                      : 16'd0) |
    (io_addr[15:00] == 16'h0030 ? {15'd0, spi_miso}                   : 16'd0) |

/* -----\/----- EXCLUDED -----\/-----
    (io_addr[15:00] == 16'h0050 ? {12'd0, h71_dout}                   : 16'd0) |
    (io_addr[15:00] == 16'h0051 ? h71_addr[15:00]                     : 16'd0) |
    (io_addr[15:00] == 16'h0052 ? {12'd0, h71_addr[19:16]}            : 16'd0) |

    (io_addr[15:00] == 16'h0060 ? {12'd0, id0}                        : 16'd0) |
    (io_addr[15:00] == 16'h0061 ? {12'd0, id1}                        : 16'd0) |
    (io_addr[15:00] == 16'h0062 ? {12'd0, id2}                        : 16'd0) |
    (io_addr[15:00] == 16'h0063 ? {12'd0, id3}                        : 16'd0) |
    (io_addr[15:00] == 16'h0064 ? {12'd0, id4}                        : 16'd0) |
    (io_addr[15:00] == 16'h0065 ? {12'd0, id5}                        : 16'd0) |
    (io_addr[15:00] == 16'h0066 ? {12'd0, id6}                        : 16'd0) |
    (io_addr[15:00] == 16'h0067 ? {12'd0, id7}                        : 16'd0) |
 -----/\----- EXCLUDED -----/\----- */

/* -----\/----- EXCLUDED -----\/-----
    (io_addr[15:00] == 16'h0070 ? {11'd0, cfg0}                       : 16'd0) |
    (io_addr[15:00] == 16'h0071 ? {11'd0, cfg1}                         : 16'd0) |
    (io_addr[15:00] == 16'h0072 ? {11'd0, cfg2}                         : 16'd0) |
    (io_addr[15:00] == 16'h0073 ? {11'd0, cfg3}                         : 16'd0) |
    (io_addr[15:00] == 16'h0074 ? {11'd0, cfg4}                         : 16'd0) |
    (io_addr[15:00] == 16'h0075 ? {11'd0, cfg5}                         : 16'd0) |
    (io_addr[15:00] == 16'h0076 ? {11'd0, cfg6}                         : 16'd0) |
    (io_addr[15:00] == 16'h0077 ? {11'd0, cfg7}                         : 16'd0) |

    (io_addr[15:00] == 16'h0080 ? ctrl                                : 16'd0) |

    (io_addr[15:00] == 16'h0090 ? {8'd0, cr_in}                       : 16'd0) |
    (io_addr[15:00] == 16'h0091 ? {8'd0, cr_stat}                     : 16'd0) |
 -----/\----- EXCLUDED -----/\----- */

    (io_addr[15:00] == 16'h00A0 ? BOOTCTL                             : 16'd0) |

    (io_addr[12] ? { 8'd0, terminal_data}                             : 16'd0) |
    (io_addr[13] ? {13'd0, random, terminal_valid, terminal_ready}    : 16'd0) |
    (io_addr[15:00] == 16'h0040 ? ticks                               : 16'd0) ;


    
  always @(posedge clk) begin

    if (io_wr & (io_addr[15:00] == 16'h0008))      LEDS  <=           io_dout;
    if (io_wr & (io_addr[15:00] == 16'h0009))      LEDS  <=  LEDS  & ~io_dout; // Clear
    if (io_wr & (io_addr[15:00] == 16'h000A))      LEDS  <=  LEDS  |  io_dout; // Set
    if (io_wr & (io_addr[15:00] == 16'h000B))      LEDS  <=  LEDS  ^  io_dout; // Invert

    if (io_wr & (io_addr[15:00] == 16'h0010))      pmod_out  <=               io_dout;
    if (io_wr & (io_addr[15:00] == 16'h0011))      pmod_out  <=  pmod_out  & ~io_dout; // Clear
    if (io_wr & (io_addr[15:00] == 16'h0012))      pmod_out  <=  pmod_out  |  io_dout; // Set
    if (io_wr & (io_addr[15:00] == 16'h0013))      pmod_out  <=  pmod_out  ^  io_dout; // Invert

    if (io_wr & (io_addr[15:00] == 16'h0018))      pmod_dir  <=               io_dout;
    if (io_wr & (io_addr[15:00] == 16'h0019))      pmod_dir  <=  pmod_dir  & ~io_dout; // Clear
    if (io_wr & (io_addr[15:00] == 16'h001A))      pmod_dir  <=  pmod_dir  |  io_dout; // Set
    if (io_wr & (io_addr[15:00] == 16'h001B))      pmod_dir  <=  pmod_dir  ^  io_dout; // Invert

    if (io_wr & (io_addr[15:00] == 16'h0028))      {spios}   <=               io_dout;
    if (io_wr & (io_addr[15:00] == 16'h0029))      {spios}   <=  {spios}   & ~io_dout; // Clear
    if (io_wr & (io_addr[15:00] == 16'h002A))      {spios}   <=  {spios}   |  io_dout; // Set
    if (io_wr & (io_addr[15:00] == 16'h002B))      {spios}   <=  {spios}   ^  io_dout; // Invert

    if (io_wr & (io_addr[15:00] == 16'h0020))      j1a_din   <= io_dout;
    if (io_wr & (io_addr[15:00] == 16'h0021))      sram_addr <= io_dout;
    if (io_wr & (io_addr[15:00] == 16'h0021))      j1a_addr  <= io_dout;

/* -----\/----- EXCLUDED -----\/-----
    if (io_wr & (io_addr[15:00] == 16'h0051))      sram_addr[13:00] <= io_dout[15:02];
    if (io_wr & (io_addr[15:00] == 16'h0051))      h71_addr [15:00] <= io_dout[15:00];
    if (io_wr & (io_addr[15:00] == 16'h0052))      sram_addr[15:14] <= io_dout[01:00];
    if (io_wr & (io_addr[15:00] == 16'h0052))      h71_addr [19:16] <= io_dout[03:00];

    if (io_wr & (io_addr[15:00] == 16'h0060))      id0 <= io_dout[3:0];
    if (io_wr & (io_addr[15:00] == 16'h0061))      id1 <= io_dout[3:0];
    if (io_wr & (io_addr[15:00] == 16'h0062))      id2 <= io_dout[3:0];
    if (io_wr & (io_addr[15:00] == 16'h0063))      id3 <= io_dout[3:0];
    if (io_wr & (io_addr[15:00] == 16'h0064))      id4 <= io_dout[3:0];
    if (io_wr & (io_addr[15:00] == 16'h0065))      id5 <= io_dout[3:0];
    if (io_wr & (io_addr[15:00] == 16'h0066))      id6 <= io_dout[3:0];
    if (io_wr & (io_addr[15:00] == 16'h0067))      id7 <= io_dout[3:0];

    if (io_wr & (io_addr[15:00] == 16'h0070))      cfg0 <= io_dout[4:0];
    if (io_wr & (io_addr[15:00] == 16'h0071))      cfg1 <= io_dout[4:0];
    if (io_wr & (io_addr[15:00] == 16'h0072))      cfg2 <= io_dout[4:0];
    if (io_wr & (io_addr[15:00] == 16'h0073))      cfg3 <= io_dout[4:0];
    if (io_wr & (io_addr[15:00] == 16'h0074))      cfg4 <= io_dout[4:0];
    if (io_wr & (io_addr[15:00] == 16'h0075))      cfg5 <= io_dout[4:0];
    if (io_wr & (io_addr[15:00] == 16'h0076))      cfg6 <= io_dout[4:0];
    if (io_wr & (io_addr[15:00] == 16'h0077))      cfg7 <= io_dout[4:0];

    if (io_wr & (io_addr[15:00] == 16'h0080))      ctrl    <= io_dout;

    if (io_wr & (io_addr[15:00] == 16'h0090))      cr_out  <= io_dout[7:0];
    if (io_wr & (io_addr[15:00] == 16'h0091))      cr_stat <= io_dout[7:0];
 -----/\----- EXCLUDED -----/\----- */
    if (io_wr & (io_addr[15:00] == 16'h00A0))      BOOTCTL  <= io_dout[2:0];
    
    case (h71_addr[1:0])
      2'b00: begin
                h71_mask <= 4'b0001;
                h71_dout <= sram_out[03:00];
                end
      2'b01: begin
                h71_mask <= 4'b0010;
                h71_dout <= sram_out[07:04];
                end
      2'b10: begin
                h71_mask <= 4'b0100;
                h71_dout <= sram_out[11:08];
                end
      2'b11: begin
                h71_mask <= 4'b1000;
                h71_dout <= sram_out[15:12];
                end
      default: begin
                h71_mask <= 4'b1111;
                h71_dout <= sram_out[03:00];
                end
    endcase

    j1a_dout  <= io_dout;
    j1a_mask  <= 4'b1111;
    
  end

endmodule
