
`default_nettype none

module hp71b
  (
   input 	 clk_i,
   input 	 rstn_i,
   
   input  [ 2:0] bank_addr,
   input 	 bank_wr,
   input  [ 3:0] bank_datain,

   input 	 daisyin_i,
   input 	 stroben_i,
   input 	 commandn_i,

   output 	 cntrl_enb_o, // Control signals
   output 	 busin_enb_o, // Data from bus
   output 	 busout_enb_o, // Data to bus
   output [ 3:0] data_dir_o, // IOB direction

   input  [ 3:0] bus_in, // Data from 71B bus
   output [ 3:0] bus_out, // Data to 71B bus

   output [19:0] spram_addr,
   output [ 3:0] spram_out,
   input  [ 3:0] spram_in,
   output        spram_en,
   output 	 spram_wr
   );

  // ######   HP-71B Bus Control   ############################
  // M. Fleming
  // No need to add control signals to j1a address space
  // Buffers are controlled by HP-71B bus state machine
   reg  [ 3:0] data_out;
   reg  [19:0] ramaddr;
   //reg  [ 3:0] ramout;
   // See if a bank is selected by the DP or PC register
   wire        sel0;
   wire        sel1;
   wire        sel2;
   wire        sel3;
   wire        sel4;
   wire        sel5;
   wire        sel6;
   wire        sel7;
   // A valid SPRAM access is in a read or write state
   wire        spram_req;
   // General conditions
   wire        idout;
   wire        pcdpout;


   assign cntrl_enb_o   = 1;
   assign idout         = (state == ID)  | (state == ID2) | (state == ID3) |
		          (state == ID4) | (state == ID5);
   assign pcdpout       = (state == PCREAD) | (state == DPREAD);

   assign busout_enb_o  = (idout & daisyin_i & !bankdout[7]) |
			  (pcdpout & spram_en);
   assign busin_enb_o   = !busout_enb_o;
   assign data_dir_o    = busout_enb_o ? 4'hF : 4'h0;
   

   // A bank is selected if: it's InUse, has a valid address, and
   // the DP or PC register points to it
   assign sel0 = bank0[3] & valid_addr[0] &
		 (addr0 == pcreg[19:15] || addr0 == dpreg[19:15]);
   assign sel1 = bank1[3] & valid_addr[0] &
		 (addr1 == pcreg[19:15] || addr1 == dpreg[19:15]);
   assign sel2 = bank2[3] & valid_addr[0] &
		 (addr2 == pcreg[19:15] || addr2 == dpreg[19:15]);
   assign sel3 = bank3[3] & valid_addr[0] &
		 (addr3 == pcreg[19:15] || addr3 == dpreg[19:15]);
   assign sel4 = bank4[3] & valid_addr[0] &
		 (addr4 == pcreg[19:15] || addr4 == dpreg[19:15]);
   assign sel5 = bank5[3] & valid_addr[0] &
		 (addr5 == pcreg[19:15] || addr5 == dpreg[19:15]);
   assign sel6 = bank6[3] & valid_addr[0] &
		 (addr6 == pcreg[19:15] || addr6 == dpreg[19:15]);
   assign sel7 = bank7[3] & valid_addr[0] &
		 (addr7 == pcreg[19:15] || addr7 == dpreg[19:15]);


   assign bus_out    = ((state == PCREAD) | (state == DPREAD)) ? spram_in :
		       data_out;
   assign spram_addr = ramaddr;
   assign spram_out  = bus_in;
   assign spram_req  = (state == PCREAD) | (state == DPREAD) |
		      (state == PCWRITE) | (state == DPWRITE);

   assign spram_en   = spram_req & (sel0 | sel1 | sel2 | sel3 |
				  sel4 | sel5 | sel6 | sel7);
   assign spram_wr   = busout_enb_o;

     


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
   reg  [3:0]  bank0 = 0;
   reg  [3:0]  bank1 = 0;
   reg  [3:0]  bank2 = 0;
   reg  [3:0]  bank3 = 0;
   reg  [3:0]  bank4 = 0;
   reg  [3:0]  bank5 = 0;
   reg  [3:0]  bank6 = 0;
   reg  [3:0]  bank7 = 0;
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
   wire [7:0]  bankdout;
   

  // ######   HP-71B State Machine   ##########################
  // M. Fleming
  // The state machine is driven by the bus Strobe signal STRN
   wire [4:0]  cmddata;
   assign cmddata = {commandn_i, bus_in};

   reg  [4:0]   bus_data   = 0;
   reg  [4:0]   state      = 0;
   reg  [4:0]   next_state = 0;

   reg  [19:0]  dpreg = 0;
   reg  [19:0]  pcreg = 0;



  localparam NOP      = 5'h0;
  localparam ID       = 5'h1;
  localparam PCREAD   = 5'h2;
  localparam DPREAD   = 5'h3;
  localparam PCWRITE  = 5'h4;
  localparam DPWRITE  = 5'h5;
  localparam LOADPC   = 5'h6;
  localparam LOADDP   = 5'h7;
  localparam CONFIG   = 5'h8;
  localparam UNCONFIG = 5'h9;
  localparam POLL     = 5'hA;
  localparam RSV1     = 5'hB;
  localparam BUSCC    = 5'hC;
  localparam RSV2     = 5'hD;
  localparam SHUTDOWN = 5'hE;
  localparam RESET    = 5'hF;

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

   always @(posedge clk_i or negedge rstn_i) begin
      if (!rstn_i) begin
	 // Reset to initial settings
	 bank0 <= 0;
	 bank1 <= 0;
	 bank2 <= 0;
	 bank3 <= 0;
	 bank4 <= 0;
	 bank5 <= 0;
	 bank6 <= 0;
	 bank7 <= 0;
      end
      else begin
	 if (bank_wr) begin
	    case (bank_addr)
	      0: bank0 <= bank_datain;
	      1: bank1 <= bank_datain;
	      2: bank2 <= bank_datain;
	      3: bank3 <= bank_datain;
	      4: bank4 <= bank_datain;
	      5: bank5 <= bank_datain;
	      6: bank6 <= bank_datain;
	      7: bank7 <= bank_datain;
	    endcase // case (bank_addr)
	 end // if (bank_wr)
      end
   end
   
  always @(posedge stroben_i) begin
     // Reset valid address bits on rising edge of Daisy-In
     if ( daisyin_i && !din_last ) begin
	valid_addr <= 0;
     end
     din_last <= daisyin_i;

     // Data from Saturn CPU valid on rising edge of STRN
     // Data is a command if CDN=0
     // CDN: 0=Command, 1=Data on bus
     // Read cycle expects data to be valid on rising edge of STRN
     // Write cycle data is valid on rising edge of STRN
     bus_data <= cmddata;

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
	  pcreg[3:0] <= bus_in;
       end
       LOADPC2: begin
	  pcreg[7:4] <= bus_in;
       end
       LOADPC3: begin
	  pcreg[11:8] <= bus_in;
       end
       LOADPC4: begin
	  pcreg[15:12] <= bus_in;
       end
       LOADPC5: begin
	  pcreg[19:16] <= bus_in;
       end
       // DP Address is 5 nibbles, low to high
       LOADDP: begin
	  dpreg[3:0] <= bus_in;
       end
       LOADDP2: begin
	  dpreg[7:4] <= bus_in;
       end
       LOADDP3: begin
	  dpreg[11:8] <= bus_in;
       end
       LOADDP4: begin
	  dpreg[15:12] <= bus_in;
       end
       LOADDP5: begin
	  dpreg[19:16] <= bus_in;
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
	  if (daisyin_i & !bankdout[0])
	    addr0[0] <= bus_in[3];
	  if (bankdout[0] & !bankdout[1])
	    addr1[0] <= bus_in[3];
	  if (bankdout[1] & !bankdout[2])
	    addr2[0] <= bus_in[3];
	  if (bankdout[2] & !bankdout[3])
	    addr3[0] <= bus_in[3];
	  if (bankdout[3] & !bankdout[4])
	    addr4[0] <= bus_in[3];
	  if (bankdout[4] & !bankdout[5])
	    addr5[0] <= bus_in[3];
	  if (bankdout[5] & !bankdout[6])
	    addr6[0] <= bus_in[3];
	  if (bankdout[6] & !bankdout[7])
	    addr7[0] <= bus_in[3];
       end
       CONFIG5: begin
	  if (daisyin_i & !bankdout[0]) begin
	     addr0[4:1]    <= bus_in;
	     valid_addr[0] <= 1;
	  end
	  if (bankdout[0] & !bankdout[1]) begin
	     addr1[4:1]    <= bus_in;
	     valid_addr[1] <= 1;
	  end
	  if (bankdout[1] & !bankdout[2]) begin
	     addr2[4:1]    <= bus_in;
	     valid_addr[2] <= 1;
	  end
	  if (bankdout[2] & !bankdout[3]) begin
	     addr3[4:1]    <= bus_in;
	     valid_addr[3] <= 1;
	  end
	  if (bankdout[3] & !bankdout[4]) begin
	     addr4[4:1]    <= bus_in;
	     valid_addr[4] <= 1;
	  end
	  if (bankdout[4] & !bankdout[5]) begin
	     addr5[4:1]    <= bus_in;
	     valid_addr[5] <= 1;
	  end
	  if (bankdout[5] & !bankdout[6]) begin
	     addr6[4:1]    <= bus_in;
	     valid_addr[6] <= 1;
	  end
	  if (bankdout[6] & !bankdout[7]) begin
	     addr7[4:1]    <= bus_in;
	     valid_addr[7] <= 1;
	  end
       end // case: CONFIG5
       SHUTDOWN: begin
       end
       RESET: begin
       end
       default: begin
       end
     endcase // case (state)
     
  end // always @ (posedge stroben_i)
   

  always @(negedge stroben_i) begin
     // A command or data cycle begins on the falling edge of STRN
     state   <= next_state;

     // The direction of the four data I/O lines should follow the
     // logic translation buffer directions
     
     // Data out to the 71B bus should be asserted at the beginning
     // of a bus cycle
     case (state)
       PCREAD: begin
	  // SPRAM read transaction
	  ramaddr <= pcreg;
       end
       DPREAD: begin
	  // SPRAM read transaction
	  ramaddr <= dpreg;
       end
       PCWRITE: begin
	  // SPRAM read transaction
	  ramaddr <= pcreg;
       end
       DPWRITE: begin
	  // SPRAM read transaction
	  ramaddr <= dpreg;
       end
       default: begin
       end
     endcase // case (state)
  end // always @ (negedge stroben_i)

   
  always @(*) begin
     // Daisy-In chain management while daisyin_i == 1
     // Daisy-Out = DaisyIn & InUse & Configured |
     //             DaisyIn & InUse & (HARD | TAKEOVER) |
     //             DaisyIn & !InUse
     bankdout[0] = (daisyin_i & bank0[3] & valid_addr[0]) |
		   (daisyin_i & bank0[3] & bank0[1]) | // HARD or TAKEOVER
		   (!bank0[3] & daisyin_i);

     bankdout[1] = (bankdout[0] & bank1[3] & valid_addr[1]) |
		   (bankdout[0] & bank1[3] & bank1[1]) | // HARD or TAKEOVER
		   (!bank1[3] & bankdout[0]);

     bankdout[2] = (bankdout[1] & bank2[3] & valid_addr[2]) |
		   (bankdout[1] & bank2[3] & bank2[1]) | // HARD or TAKEOVER
		   (!bank2[3] & bankdout[1]);

     bankdout[3] = (bankdout[2] & bank3[3] & valid_addr[3]) |
		   (bankdout[2] & bank3[3] & bank3[1]) | // HARD or TAKEOVER
		   (!bank3[3] & bankdout[2]);

     bankdout[4] = (bankdout[3] & bank4[3] & valid_addr[4]) |
		   (bankdout[3] & bank4[3] & bank4[1]) | // HARD or TAKEOVER
		   (!bank4[3] & bankdout[3]);

     bankdout[5] = (bankdout[4] & bank5[3] & valid_addr[5]) |
		   (bankdout[4] & bank5[3] & bank5[1]) | // HARD or TAKEOVER
		   (!bank5[3] & bankdout[4]);

     bankdout[6] = (bankdout[5] & bank6[3] & valid_addr[6]) |
		   (bankdout[5] & bank6[3] & bank6[1]) | // HARD or TAKEOVER
		   (!bank6[3] & bankdout[5]);

     bankdout[7] = (bankdout[6] & bank7[3] & valid_addr[7]) |
		   (bankdout[6] & bank7[3] & bank7[1]) | // HARD or TAKEOVER
		   (!bank7[3] & bankdout[6]);
     
     // Must use sampled bus values since CDN and Data Bus state
     // not held through the end of a bus cycle
     if (bus_data[4] == 0)   // commandn_i == 0
       next_state = {1'b0, bus_data[3:0]};
     else
       case (state)
	 ID: begin
	    // Only respond if DaisyIn and not DaisyOut
	    if (daisyin_i & !bankdout[7])
	      next_state = ID2;
	    else
	      next_state = NOP;
	 end
	 ID2: begin
	    next_state = ID3;
	 end
	 ID3: begin
	    next_state = ID4;
	 end
	 ID4: begin
	    next_state = ID5;
	 end
	 ID5: begin
	    next_state = NOP;
	 end
	 PCREAD: begin
	    next_state = PCREAD;
	 end
	 DPREAD: begin
	    next_state = DPREAD;
	 end
	 PCWRITE: begin
	    next_state = PCWRITE;
	 end
	 DPWRITE: begin
	    next_state = DPWRITE;
	 end
	 LOADPC: begin
	    next_state = LOADPC2;
	 end
	 LOADPC2: begin
	    next_state = LOADPC3;
	 end
	 LOADPC3: begin
	    next_state = LOADPC4;
	 end
	 LOADPC4: begin
	    next_state = LOADPC5;
	 end
	 LOADPC5: begin
	    next_state = PCREAD;
	 end
	 LOADDP: begin
	    next_state = LOADDP2;
	 end
	 LOADDP2: begin
	    next_state = LOADDP3;
	 end
	 LOADDP3: begin
	    next_state = LOADDP4;
	 end
	 LOADDP4: begin
	    next_state = LOADDP5;
	 end
	 LOADDP5: begin
	    next_state = DPREAD;
	 end
	 CONFIG: begin
	    if (daisyin_i == 1)
	      next_state = CONFIG2;
	    else
	      next_state = NOP;
	 end
	 CONFIG2: begin
	    next_state = CONFIG3;
	 end
	 CONFIG3: begin
	    next_state = CONFIG4;
	 end
	 CONFIG4: begin
	    next_state = CONFIG5;
	 end
	 CONFIG5: begin
	    next_state = NOP;
	 end
	 SHUTDOWN: begin
	    next_state = NOP;
	 end
	 RESET: begin
	    next_state = NOP;
	 end
	 default: begin
	    next_state = NOP;
	 end
       endcase // case (state)

     /* verilator lint_off CASEINCOMPLETE */
     case (state)
       ID: begin
	  if (daisyin_i) begin
	     data_out = 4'hB;     // 16K memory chip size
	  end
       end
       ID2: begin
	  if (daisyin_i) begin
	     data_out = 4'h0;     // Reserved for future use
	  end
       end
       ID3: begin
	  if (daisyin_i) begin
	     // 0 for RAM, 1 for ROM,
	     // 2~E asst mem types, F memory mapped I/O
	     if ( daisyin_i & bank0[3] & !bank0[1] )
	       data_out = {3'b000, bank0[0]};
	     if ( daisyin_i & bank1[3] & !bank1[1] )
	       data_out = {3'b000, bank1[0]};
	     if ( daisyin_i & bank2[3] & !bank2[1] )
	       data_out = {3'b000, bank2[0]};
	     if ( daisyin_i & bank3[3] & !bank3[1] )
	       data_out = {3'b000, bank3[0]};
	     if ( daisyin_i & bank4[3] & !bank4[1] )
	       data_out = {3'b000, bank4[0]};
	     if ( daisyin_i & bank5[3] & !bank5[1] )
	       data_out = {3'b000, bank5[0]};
	     if ( daisyin_i & bank6[3] & !bank6[1] )
	       data_out = {3'b000, bank6[0]};
	     if (daisyin_i & bank7[3] & !bank7[1] )
	       data_out = {3'b000, bank7[0]};
	  end
       end
       ID4: begin
	  if (daisyin_i) begin
	     data_out = 4'h0;     // Memory, value unassigned
	  end
       end
       ID5: begin
	  // Set bits 3 & 2 to 1 if last chip flag set
	  // MultiMod only sets bit 3?
	  if ( daisyin_i & bank0[3] & !bank0[1] )
	    data_out = {bank0[2], bank0[2], 2'b00};
	  if ( daisyin_i & bank1[3] & !bank1[1] )
	    data_out = {bank1[2], bank1[2], 2'b00};
	  if ( daisyin_i & bank2[3] & !bank2[1] )
	    data_out = {bank2[2], bank2[2], 2'b00};
	  if ( daisyin_i & bank3[3] & !bank3[1] )
	    data_out = {bank3[2], bank3[2], 2'b00};
	  if ( daisyin_i & bank4[3] & !bank4[1] )
	    data_out = {bank4[2], bank4[2], 2'b00};
	  if ( daisyin_i & bank5[3] & !bank5[1] )
	    data_out = {bank5[2], bank5[2], 2'b00};
	  if ( daisyin_i & bank6[3] & !bank6[1] )
	    data_out = {bank6[2], bank6[2], 2'b00};
	  if ( daisyin_i & bank7[3] & !bank7[1] )
	    data_out = {bank7[2], bank7[2], 2'b00};
       end // case: ID5
     endcase // case (state)
     /* verilator lint_on CASEINCOMPLETE */
     
  end

endmodule
