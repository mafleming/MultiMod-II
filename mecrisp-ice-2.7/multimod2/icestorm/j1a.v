
`default_nettype none

`include "../common-verilog/j1-universal-16kb-quickstore.v"

`include "../common-verilog/usb_cdc/usb_cdc.v"
`include "../common-verilog/usb_cdc/bulk_endp.v"
`include "../common-verilog/usb_cdc/ctrl_endp.v"
`include "../common-verilog/usb_cdc/phy_rx.v"
`include "../common-verilog/usb_cdc/phy_tx.v"
`include "../common-verilog/usb_cdc/sie.v"

module top (
    input  clki,     // 48 MHz clock input
    output clki_en,  // Enable external clock

    inout  data_0, // Four user pins
    inout  data_1,
    inout  data_2,
    inout  data_3,

    output rgb0, // LED outputs
    output rgb1,
    output rgb2,

    output spi_cs,    // SPI Flash
    output spi_clk,
    inout  spi_miso,
    inout  spi_mosi,
    inout  spi_io2,
    inout  spi_io3,

    inout  usb_dp, // USB pins
    inout  usb_dn,
    output usb_dp_pu,
    input  usb_activ,

    output to71,   // Tristate buffer control
    output from71,
    output ctrl71,

    input  din71,  // HP71B bus control signals
    input  cdn71,
    input  strn71
);

  // ######   Clock   #########################################

  reg [1:0] divider;

  always @(posedge clki) divider <= divider + 1;

  wire clk_usb = clki;       // 48 MHz
  wire clk     = divider[1]; // 12 MHz

  assign clki_en = 1'b1;


  // ######   Reset logic   ###################################

  wire button = 1'b1;

  reg [7:0] reset_cnt = 0;
  wire resetq = &reset_cnt;

  always @(posedge clk) begin
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
  // M. Fleming
  // Make the SPI IO0~IO3 lines directional

  reg  [3:0] spio_dir = 4'b1110;   // 1:output, 0:input
  reg  [3:0] spio_out;
  wire [3:0] spio_in;

  SB_IO #(.PIN_TYPE(6'b1010_01)) spio0 (.PACKAGE_PIN(spi_miso), .D_OUT_0(spio_out[0]), .D_IN_0(spio_in[0]), .OUTPUT_ENABLE(spio_dir[0]));
  SB_IO #(.PIN_TYPE(6'b1010_01)) spio1 (.PACKAGE_PIN(spi_mosi), .D_OUT_0(spio_out[1]), .D_IN_0(spio_in[1]), .OUTPUT_ENABLE(spio_dir[1]));
  SB_IO #(.PIN_TYPE(6'b1010_01)) spio2 (.PACKAGE_PIN(spi_io2), .D_OUT_0(spio_out[2]), .D_IN_0(spio_in[2]), .OUTPUT_ENABLE(spio_dir[2]));
  SB_IO #(.PIN_TYPE(6'b1010_01)) spio3 (.PACKAGE_PIN(spi_io3), .D_OUT_0(spio_out[3]), .D_IN_0(spio_in[3]), .OUTPUT_ENABLE(spio_dir[3]));

  // ######   Warm Boot Control   #############################
  // M. Fleming
  // Add control register to j1a address space

   reg [2:0] BOOTCTL = 0;

  SB_WARMBOOT B_WARMBOOT(
      .BOOT(BOOTCTL[2]),
      .S0(BOOTCTL[0]),
      .S1(BOOTCTL[1])
  );


  // ######   HP-71B Bus Control   ############################
  // M. Fleming
  // No need to add control signals to j1a address space
  // Buffers are controlled by HP-71B bus state machine
   assign ctrl71 = 1;
   assign from71 = (state == ID)  | (state == ID2) | (state == ID3) |
		   (state == ID4) | (state == ID5) |
		   (state == PCREAD) | (state == DPREAD);
   assign to71   = !from71;
     


  // ######   HP-71B Bank Control   ###########################
  // M. Fleming
  // Bank control registers selected by the lower three I/O
  // address bits and enabled by the I/O address bit 10

   // type values must match those in Forth file directory.fs
   localparam IRAM     = 2'b00;
   localparam ROM      = 2'b01;
   localparam HARD     = 2'b10;
   localparam TAKEOVER = 2'b11;
   

  // Bank bits are: InUse, ChainEnd, ROMtype[1:0]
   reg [3:0]  bank0 = 0;
   reg [3:0]  bank1 = 0;
   reg [3:0]  bank2 = 0;
   reg [3:0]  bank3 = 0;
   reg [3:0]  bank4 = 0;
   reg [3:0]  bank5 = 0;
   reg [3:0]  bank6 = 0;
   reg [3:0]  bank7 = 0;
   // Bank address registers are upper 5 bits of assigned address
   reg  [4:0]  addr0  = 0;
   reg  [4:0]  addr1  = 0;
   reg  [4:0]  addr2  = 0;
   reg  [4:0]  addr3  = 0;
   reg  [4:0]  addr4  = 0;
   reg  [4:0]  addr5  = 0;
   reg  [4:0]  addr6  = 0;
   reg  [4:0]  addr7  = 0;
   // Keep track of whether a bank has been assigned a valid address
   reg  [7:0]  valid_addr = 0;

  // While Daisy-In (Din) is high, the state machine issues
  // responses to the ID command and accepts CONFIG command
  // addresses
   reg 	       din_last = 0;
   reg  [7:0]  bankdout = 0;
   

  // ######   HP-71B State Machine   ##########################
  // M. Fleming
  // The state machine is driven by the bus Strobe signal STRN
   wire [4:0]  cmddata;
   assign cmddata = {cdn71, data_in};

   reg  [4:0]   bus_data   = 0;
   reg  [4:0]   state      = 0;
   reg  [4:0]   next_state = 0;

   reg  [19:0]  dpreg = 0;
   reg  [19:0]  pcreg = 0;
   wire [19:0] 	pcreg_plus_1 = pcreg + 1;
   wire [19:0] 	dpreg_plus_1 = dpreg + 1;



  localparam NOP      = 4'h0;
  localparam ID       = 4'h1;
  localparam PCREAD   = 4'h2;
  localparam DPREAD   = 4'h3;
  localparam PCWRITE  = 4'h4;
  localparam DPWRITE  = 4'h5;
  localparam LOADPC   = 4'h6;
  localparam LOADDP   = 4'h7;
  localparam CONFIG   = 4'h8;
  localparam UNCONFIG = 4'h9;
  localparam POLL     = 4'hA;
  localparam RSV1     = 4'hB;
  localparam BUSCC    = 4'hC;
  localparam RSV2     = 4'hD;
  localparam SHUTDOWN = 4'hE;
  localparam RESET    = 4'hF;

  localparam ID2      = 5'h10;
  localparam ID3      = 5'h11;
  localparam ID4      = 5'h12;
  localparam ID5      = 5'h13;
  localparam LOADPC2  = 5'h14;
  localparam LOADPC3  = 5'h15;
  localparam LOADPC4  = 5'h16;
  localparam LOADPC5  = 5'h17;
  localparam LOADDP2  = 5'h18;
  localparam LOADDP3  = 5'h19;
  localparam LOADDP4  = 5'h1A;
  localparam LOADDP5  = 5'h1B;
  localparam CONFIG2  = 5'h1C;
  localparam CONFIG3  = 5'h1D;
  localparam CONFIG4  = 5'h1E;
  localparam CONFIG5  = 5'h1F;

  always @(posedge strn71) begin
     // Reset valid address bits on rising edge of Daisy-In
     if ( din71 && !din_last ) begin
	valid_addr <= 0;
	bankdout   <= 0;
     end
     if ( !din71 && din_last ) begin
	bankdout   <= 0;
     end
     din_last <= din71;

     // Daisy-In chain management while din71 == 1
     // Daisy-Out = InUse & Configured | InUse & (HARD | TAKEOVER)
     if (din71 == 1) begin
	bankdout[0] <= bank0[3] & valid_addr[0] |
		       bank0[3] & bank0[1]; // HARD or TAKEOVER

	bankdout[1] <= bankdout[0] & bank1[3] & valid_addr[1] |
		       bank1[3] & bank1[1]; // HARD or TAKEOVER

	bankdout[2] <= bankdout[1] & bank1[3] & valid_addr[2] |
		       bank2[3] & bank2[1]; // HARD or TAKEOVER

	bankdout[3] <= bankdout[2] & bank1[3] & valid_addr[3] |
		       bank3[3] & bank3[1]; // HARD or TAKEOVER

	bankdout[4] <= bankdout[3] & bank1[3] & valid_addr[4] |
		       bank4[3] & bank4[1]; // HARD or TAKEOVER

	bankdout[5] <= bankdout[4] & bank1[3] & valid_addr[5] |
		       bank5[3] & bank5[1]; // HARD or TAKEOVER

	bankdout[6] <= bankdout[5] & bank1[3] & valid_addr[6] |
		       bank6[3] & bank6[1]; // HARD or TAKEOVER

	bankdout[7] <= bankdout[6] & bank1[3] & valid_addr[7] |
		       bank7[3] & bank7[1]; // HARD or TAKEOVER
     end // if (din71 == 1)
     else
       bankdout <= 0;


     // Data from Saturn CPU valid on rising edge of STRN
     // Data is a command if CDN=0
     // CDN: 0=Command, 1=Data on bus
     // Read cycle expects data to be valid on rising edge of STRN
     // Write cycle data is valid on rising edge of STRN
     bus_data = cmddata;

     // Action performed within each command state
     case (state)
       PCREAD: begin
	  pcreg  <= pcreg + 1;
       end
       DPREAD: begin
	  dpreg  <= dpreg + 1;
       end
       PCWRITE: begin
	  pcreg <= pcreg + 1;
       end
       DPWRITE: begin
	  dpreg <= dpreg + 1;
       end
       // PC Address is 5 nibbles, low to high
       LOADPC: begin
	  pcreg[3:0] <= data_in;
       end
       LOADPC2: begin
	  pcreg[7:4] <= data_in;
       end
       LOADPC3: begin
	  pcreg[11:8] <= data_in;
       end
       LOADPC4: begin
	  pcreg[15:12] <= data_in;
       end
       LOADPC5: begin
	  pcreg[19:16] <= data_in;
       end
       // DP Address is 5 nibbles, low to high
       LOADDP: begin
	  dpreg[3:0] <= data_in;
       end
       LOADDP2: begin
	  dpreg[7:4] <= data_in;
       end
       LOADDP3: begin
	  dpreg[11:8] <= data_in;
       end
       LOADDP4: begin
	  dpreg[15:12] <= data_in;
       end
       LOADDP5: begin
	  dpreg[19:16] <= data_in;
       end
       CONFIG: begin
	  // Banks with HARD or TAKEOVER ROM content need fixed
	  // addresses. This scheme requires a hard ROM to appear
	  // in consecutive banks. As encoded, bank address regs
	  // will be initialized even when Daisy-In isn't active.
	  if (bank0[3] & !bank0[2] & bank0[1] & !bank0[0])
	    begin
	       addr0 <= 5'b11100;
	       addr1 <= 5'b11110;
	    end
	  if (bank1[3] & !bank1[2] & bank1[1] & !bank1[0])
	    begin
	       addr1 <= 5'b11100;
	       addr2 <= 5'b11110;
	    end
	  if (bank2[3] & !bank2[2] & bank2[1] & !bank2[0])
	    begin
	       addr2 <= 5'b11100;
	       addr3 <= 5'b11110;
	    end
	  if (bank3[3] & !bank3[2] & bank3[1] & !bank3[0])
	    begin
	       addr3 <= 5'b11100;
	       addr4 <= 5'b11110;
	    end
	  if (bank4[3] & !bank4[2] & bank4[1] & !bank4[0])
	    begin
	       addr4 <= 5'b11100;
	       addr5 <= 5'b11110;
	    end
	  if (bank5[3] & !bank5[2] & bank5[1] & !bank5[0])
	    begin
	       addr5 <= 5'b11100;
	       addr5 <= 5'b11110;
	    end
	  if (bank6[3] & !bank6[2] & bank6[1] & !bank6[0])
	    begin
	       addr6 <= 5'b11100;
	       addr7 <= 5'b11110;
	    end

	  // This encoding assumes a 64KB TAKEOVER ROM appears
	  // in consecutive banks.
	  if (bank0[3] & !bank0[2] & bank0[1] & bank0[0])
	    begin
	       addr0 <= 5'b00000;
	       addr1 <= 5'b00001;
	       addr2 <= 5'b00010;
	       addr3 <= 5'b00011;
	    end
	  if (bank1[3] & !bank1[2] & bank1[1] & bank1[0])
	    begin
	       addr1 <= 5'b00000;
	       addr2 <= 5'b00001;
	       addr3 <= 5'b00010;
	       addr4 <= 5'b00011;
	    end
	  if (bank2[3] & !bank2[2] & bank2[1] & bank2[0])
	    begin
	       addr2 <= 5'b00000;
	       addr3 <= 5'b00001;
	       addr4 <= 5'b00010;
	       addr5 <= 5'b00011;
	    end
	  if (bank3[3] & !bank3[2] & bank3[1] & bank3[0])
	    begin
	       addr3 <= 5'b00000;
	       addr4 <= 5'b00001;
	       addr5 <= 5'b00010;
	       addr6 <= 5'b00011;
	    end
	  if (bank4[3] & !bank4[2] & bank4[1] & bank4[0])
	    begin
	       addr4 <= 5'b00000;
	       addr5 <= 5'b00001;
	       addr6 <= 5'b00010;
	       addr7 <= 5'b00011;
	    end
       end // case: CONFIG
       CONFIG4: begin
	  if (din71 & !bankdout[0])
	    addr0[0] <= data_in[3];
	  if (bankdout[0] & !bankdout[1])
	    addr1[0] <= data_in[3];
	  if (bankdout[1] & !bankdout[2])
	    addr2[0] <= data_in[3];
	  if (bankdout[2] & !bankdout[3])
	    addr3[0] <= data_in[3];
	  if (bankdout[3] & !bankdout[4])
	    addr4[0] <= data_in[3];
	  if (bankdout[4] & !bankdout[5])
	    addr5[0] <= data_in[3];
	  if (bankdout[5] & !bankdout[6])
	    addr6[0] <= data_in[3];
	  if (bankdout[6] & !bankdout[7])
	    addr7[0] <= data_in[3];
       end
       CONFIG5: begin
	  if (din71 & !bankdout[0]) begin
	     addr0[3:1] <= data_in;
	     valid_addr[0] <= 1;
	  end
	  if (bankdout[0] & !bankdout[1]) begin
	     addr1[3:1] <= data_in;
	     valid_addr[1] <= 1;
	  end
	  if (bankdout[1] & !bankdout[2]) begin
	     addr2[3:1] <= data_in;
	     valid_addr[2] <= 1;
	  end
	  if (bankdout[2] & !bankdout[3]) begin
	     addr3[3:1] <= data_in;
	     valid_addr[3] <= 1;
	  end
	  if (bankdout[3] & !bankdout[4]) begin
	     addr4[3:1] <= data_in;
	     valid_addr[4] <= 1;
	  end
	  if (bankdout[4] & !bankdout[5]) begin
	     addr5[3:1] <= data_in;
	     valid_addr[5] <= 1;
	  end
	  if (bankdout[5] & !bankdout[6]) begin
	     addr6[3:1] <= data_in;
	     valid_addr[6] <= 1;
	  end
	  if (bankdout[6] & !bankdout[7]) begin
	     addr7[3:1] <= data_in;
	     valid_addr[7] <= 1;
	  end
       end // case: CONFIG5
       SHUTDOWN: begin
       end
       RESET: begin
       end
     endcase // case (state)
     
  end // always @ (posedge strn71)
   

  always @(negedge strn71) begin
     // A command or data cycle begins on the falling edge of STRN
     state   <= next_state;

     // The direction of the four data I/O lines should follow the
     // logic translation buffer directions
     
     // Data out to the 71B bus should be asserted at the beginning
     // of a bus cycle
     case (state)
       ID: begin
	  if (din71) begin
	     data_out <= 4'hB;     // 16K memory chip size
	     data_dir <= 4'hF;     // Output
	  end
       end
       ID2: begin
	  if (din71) begin
	     data_out <= 4'h0;     // Reserved for future use
	     data_dir <= 4'hF;     // Output
	  end
       end
       ID3: begin
	  if (din71)
	    data_dir <= 4'hF;     // Output

	  // 0 for RAM, 1 for ROM,
	  // 2~E asst mem types, F memory mapped I/O
	  if ( din71 & bank0[3] & !bank0[1] )
	    data_out <= bank0[0];
	  if ( din71 & bank1[3] & !bank1[1] )
	    data_out <= bank1[0];
	  if ( din71 & bank2[3] & !bank2[1] )
	    data_out <= bank2[0];
	  if ( din71 & bank3[3] & !bank3[1] )
	    data_out <= bank3[0];
	  if ( din71 & bank4[3] & !bank4[1] )
	    data_out <= bank4[0];
	  if ( din71 & bank5[3] & !bank5[1] )
	    data_out <= bank5[0];
	  if ( din71 & bank6[3] & !bank6[1] )
	    data_out <= bank6[0];
	  if (din71 & bank7[3] & !bank7[1] )
	    data_out <= bank7[0];
       end
       ID4: begin
	  if (din71) begin
	     data_out <= 4'h0;     // Memory, value unassigned
	     data_dir <= 4'hF;     // Output
	  end
       end
       ID5: begin
	  if (din71)
	    data_dir <= 4'hF;     // Output

	  // Set bits 3 & 2 to 1 if last chip flag set
	  // MultiMod only sets bit 3?
	  if ( din71 & bank0[3] & !bank0[1] )
	    data_out <= {bank0[2], bank0[2], 2'b00};
	  if ( din71 & bank1[3] & !bank1[1] )
	    data_out <= {bank1[2], bank1[2], 2'b00};
	  if ( din71 & bank2[3] & !bank2[1] )
	    data_out <= {bank2[2], bank2[2], 2'b00};
	  if ( din71 & bank3[3] & !bank3[1] )
	    data_out <= {bank3[2], bank3[2], 2'b00};
	  if ( din71 & bank4[3] & !bank4[1] )
	    data_out <= {bank4[2], bank4[2], 2'b00};
	  if ( din71 & bank5[3] & !bank5[1] )
	    data_out <= {bank5[2], bank5[2], 2'b00};
	  if ( din71 & bank6[3] & !bank6[1] )
	    data_out <= {bank6[2], bank6[2], 2'b00};
	  if ( din71 & bank7[3] & !bank7[1] )
	    data_out <= {bank7[2], bank7[2], 2'b00};
       end
       PCREAD: begin
	  data_dir <= 4'hF;     // Output
	  // SPRAM read transaction
	  addr71 <= pcreg;
       end
       DPREAD: begin
	  data_dir <= 4'hF;
	  // SPRAM read transaction
	  addr71 <= dpreg;
       end
       PCWRITE: begin
	  data_dir <= 4'h0;     // Input
	  // SPRAM read transaction
	  addr71 <= pcreg;
       end
       DPWRITE: begin
	  data_dir <= 4'h0;     // Input
	  // SPRAM read transaction
	  addr71 <= dpreg;
       end
       default: begin
	  data_dir <= 4'h0;     // Input
       end
     endcase // case (state)
  end // always @ (negedge strn71)

   
  always @(*) begin
     // Must use sampled bus values since CDN and Data Bus state
     // not held through the end of a bus cycle
     if (bus_data[4] == 0)
       next_state <= bus_data[3:0];
     else
       case (state)
	 ID: begin
	    next_state <= ID2;
	 end
	 ID2: begin
	    next_state <= ID3;
	 end
	 ID3: begin
	    next_state <= ID4;
	 end
	 ID4: begin
	    next_state <= ID5;
	 end
	 ID5: begin
	    next_state <= NOP;
	 end
	 PCREAD: begin
	    next_state <= PCREAD;
	 end
	 DPREAD: begin
	    next_state <= DPREAD;
	 end
	 PCWRITE: begin
	    next_state <= PCWRITE;
	 end
	 DPWRITE: begin
	    next_state <= DPWRITE;
	 end
	 LOADPC: begin
	    next_state <= LOADPC2;
	 end
	 LOADPC2: begin
	    next_state <= LOADPC3;
	 end
	 LOADPC3: begin
	    next_state <= LOADPC4;
	 end
	 LOADPC4: begin
	    next_state <= LOADPC5;
	 end
	 LOADPC5: begin
	    next_state <= PCREAD;
	 end
	 LOADDP: begin
	    next_state <= LOADDP2;
	 end
	 LOADDP2: begin
	    next_state <= LOADDP3;
	 end
	 LOADDP3: begin
	    next_state <= LOADDP4;
	 end
	 LOADDP4: begin
	    next_state <= LOADDP5;
	 end
	 LOADDP5: begin
	    next_state <= DPREAD;
	 end
	 CONFIG: begin
	    next_state <= CONFIG2;
	 end
	 CONFIG2: begin
	    next_state <= CONFIG3;
	 end
	 CONFIG3: begin
	    next_state <= CONFIG4;
	 end
	 CONFIG4: begin
	    next_state <= CONFIG5;
	 end
	 CONFIG5: begin
	    next_state <= NOP;
	 end
	 SHUTDOWN: begin
	    next_state <= NOP;
	 end
	 RESET: begin
	    next_state <= NOP;
	 end
	 default: begin
	    next_state <= NOP;
	 end
       endcase
  end


   
  // ######   Ticks   #########################################

  reg [15:0] ticks;

  wire [16:0] ticks_plus_1 = ticks + 1;

  always @(posedge clk)
    if (io_wr & io_addr[14])
      ticks <= io_dout;
    else
      ticks <= ticks_plus_1;

  always @(posedge clk) // Generate interrupt on ticks overflow
    interrupt <= ticks_plus_1[16];

  // ######   Cycles   ########################################

  reg [15:0] cycles;

  always @(posedge clk) cycles <= cycles + 1;

  // ######   DATA   ##########################################
  // M. Fleming
  // These are currently controlled by the j1a CPU but will
  // eventually be part of the HP-71B bus state machine

  reg  [3:0] data_dir = 0;   // 1:output, 0:input
  reg  [3:0] data_out = 0;
  wire [3:0] data_in;

  SB_IO #(.PIN_TYPE(6'b1010_01)) io0 (.PACKAGE_PIN(data_0), .D_OUT_0(data_out[0]), .D_IN_0(data_in[0]), .OUTPUT_ENABLE(data_dir[0]));
  SB_IO #(.PIN_TYPE(6'b1010_01)) io1 (.PACKAGE_PIN(data_1), .D_OUT_0(data_out[1]), .D_IN_0(data_in[1]), .OUTPUT_ENABLE(data_dir[1]));
  SB_IO #(.PIN_TYPE(6'b1010_01)) io2 (.PACKAGE_PIN(data_2), .D_OUT_0(data_out[2]), .D_IN_0(data_in[2]), .OUTPUT_ENABLE(data_dir[2]));
  SB_IO #(.PIN_TYPE(6'b1010_01)) io3 (.PACKAGE_PIN(data_3), .D_OUT_0(data_out[3]), .D_IN_0(data_in[3]), .OUTPUT_ENABLE(data_dir[3]));


  // ######   SPRAM Pseudo Dual-Port   ########################
  // M. Fleming
  // SPRAM must respond to read or write requests from the j1a
  // CPU or from the HP-71B Saturn CPU via the bus
   reg  [19:0]  addr71;
   reg  [ 3:0] 	data_out71;
   wire [ 3:0] 	data_in71;



  // ######   SRAM   ##########################################

  reg  [15:0] sram_addr;

  wire sram_wr = io_wr & io_addr[7];

  wire [15:0] sram_in_bank0, sram_in_bank1, sram_in_bank2, sram_in_bank3;

    SB_SPRAM256KA rambank0 (
        .DATAIN(io_dout),
        .ADDRESS(sram_addr[13:0]),
        .MASKWREN(4'b1111),
        .WREN(sram_wr),
        .CHIPSELECT(1'b1),
        .CLOCK(clk),
        .STANDBY(1'b0),
        .SLEEP(~(sram_addr[15:14] == 2'b00)),
        .POWEROFF(1'b1),
        .DATAOUT(sram_in_bank0)
);

    SB_SPRAM256KA rambank1 (
        .DATAIN(io_dout),
        .ADDRESS(sram_addr[13:0]),
        .MASKWREN(4'b1111),
        .WREN(sram_wr),
        .CHIPSELECT(1'b1),
        .CLOCK(clk),
        .STANDBY(1'b0),
        .SLEEP(~(sram_addr[15:14] == 2'b01)),
        .POWEROFF(1'b1),
        .DATAOUT(sram_in_bank1)
);

    SB_SPRAM256KA rambank2 (
        .DATAIN(io_dout),
        .ADDRESS(sram_addr[13:0]),
        .MASKWREN(4'b1111),
        .WREN(sram_wr),
        .CHIPSELECT(1'b1),
        .CLOCK(clk),
        .STANDBY(1'b0),
        .SLEEP(~(sram_addr[15:14] == 2'b10)),
        .POWEROFF(1'b1),
        .DATAOUT(sram_in_bank2)
);

    SB_SPRAM256KA rambank3 (
        .DATAIN(io_dout),
        .ADDRESS(sram_addr[13:0]),
        .MASKWREN(4'b1111),
        .WREN(sram_wr),
        .CHIPSELECT(1'b1),
        .CLOCK(clk),
        .STANDBY(1'b0),
        .SLEEP(~(sram_addr[15:14] == 2'b11)),
        .POWEROFF(1'b1),
        .DATAOUT(sram_in_bank3)
);

  wire [15:0] sram_in = sram_in_bank3 | sram_in_bank2 | sram_in_bank1 | sram_in_bank0;

  // ######   USB-CDC terminal   ##############################

  assign usb_dp_pu = resetq;     // Pull-up on USB-P
   
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


  // ######   IO Ports   ######################################

  /*        Bit READ            WRITE

    + ...0                      Write as usual
    + ...1                      _C_lear bits
    + ...2                      _S_et bits
    + ...3                      _T_oggle bits

      0008  3   LEDS            LEDS (cst)

      0080  7   SRAM read       SRAM write

      0100  8   SPI read        SPI write
      0101                      SPIO direction
      0200  9   USB status      Warm Boot write
      0400  10  Bank control    Bank control
      0800  11  SRAM addr       SRAM addr

      1000  12  UART RX         UART TX
      2000  13  UART Flags
      4000  14  Ticks           Set Ticks
      8000  15  Cycles
  */

  assign io_din =

    (io_addr[ 3] ? {13'd0, LEDS}                                                    : 16'd0) |

    (io_addr[ 7] ?         sram_in                                                  : 16'd0) |

    (io_addr[ 8] ? {12'd0, spio_in}                                                 : 16'd0) |
    (io_addr[ 9] ? {13'd0, usb_activ, usb_p_tx, usb_n_tx}                           : 16'd0) |
/*  Only needed during development 
    ((io_addr[10] & (io_addr[2:0] == 0)) ? {12'd0, bank0}                           : 16'd0) |
    ((io_addr[10] & (io_addr[2:0] == 1)) ? {12'd0, bank1}                           : 16'd0) |
    ((io_addr[10] & (io_addr[2:0] == 2)) ? {12'd0, bank2}                           : 16'd0) |
    ((io_addr[10] & (io_addr[2:0] == 3)) ? {12'd0, bank3}                           : 16'd0) |
    ((io_addr[10] & (io_addr[2:0] == 4)) ? {12'd0, bank4}                           : 16'd0) |
    ((io_addr[10] & (io_addr[2:0] == 5)) ? {12'd0, bank5}                           : 16'd0) |
    ((io_addr[10] & (io_addr[2:0] == 6)) ? {12'd0, bank6}                           : 16'd0) |
    ((io_addr[10] & (io_addr[2:0] == 7)) ? {12'd0, bank7}                           : 16'd0) |
*/
    (io_addr[11] ?         sram_addr                                                : 16'd0) |

    (io_addr[12] ? { 8'd0, terminal_data}                                           : 16'd0) |
    (io_addr[13] ? {13'd0, random, terminal_valid, terminal_ready}                  : 16'd0) |
    (io_addr[14] ?         ticks                                                    : 16'd0) |
    (io_addr[15] ?         cycles                                                   : 16'd0) ;

  always @(posedge clk) begin

    if (io_wr & io_addr[3] & (io_addr[1:0] == 0))  LEDS  <=           io_dout;
    if (io_wr & io_addr[3] & (io_addr[1:0] == 1))  LEDS  <=  LEDS  & ~io_dout; // Clear
    if (io_wr & io_addr[3] & (io_addr[1:0] == 2))  LEDS  <=  LEDS  |  io_dout; // Set
    if (io_wr & io_addr[3] & (io_addr[1:0] == 3))  LEDS  <=  LEDS  ^  io_dout; // Invert

    if (io_wr & io_addr[8] & (io_addr[1:0] == 0))  {spi_clk, spi_cs, spio_out}  <= io_dout;
    if (io_wr & io_addr[8] & (io_addr[1:0] == 1))  spio_dir                     <= io_dout;

    if (io_wr & io_addr[9]) BOOTCTL                <= io_dout;
    if (io_wr & io_addr[10] & (io_addr[2:0] == 0)) bank0   <= io_dout;
    if (io_wr & io_addr[10] & (io_addr[2:0] == 1)) bank1   <= io_dout;
    if (io_wr & io_addr[10] & (io_addr[2:0] == 2)) bank2   <= io_dout;
    if (io_wr & io_addr[10] & (io_addr[2:0] == 3)) bank3   <= io_dout;
    if (io_wr & io_addr[10] & (io_addr[2:0] == 4)) bank4   <= io_dout;
    if (io_wr & io_addr[10] & (io_addr[2:0] == 5)) bank5   <= io_dout;
    if (io_wr & io_addr[10] & (io_addr[2:0] == 6)) bank6   <= io_dout;
    if (io_wr & io_addr[10] & (io_addr[2:0] == 7)) bank7   <= io_dout;

    if (io_wr & io_addr[11]) sram_addr <= io_dout;
  end

endmodule
