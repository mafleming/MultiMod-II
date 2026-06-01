`default_nettype none
`define WIDTH 16

module stack2(
  input wire clk,

  input wire we,                // Soll ein Element geschrieben werden ? Das wird dann sofort das neue NOS.
  input wire [1:0] delta,

  output wire [`WIDTH-1:0] rd,  // Das hier ist das NOS-Element, es muss immer da sein.
  input  wire [`WIDTH-1:0] wd   // Hiermit wird ein neues Element in den Stack geschrieben.
);

  parameter DEPTH = 16;
  localparam BITS = (`WIDTH * DEPTH) - 1;

  reg [15:0] inhalt [31:0];

  wire [1:0] dspI = delta;

  reg  [4:0] zeiger;
  wire [4:0] zeigerN = zeiger + {dspI[1], dspI[1], dspI[1], dspI};


  assign rd = inhalt[zeiger];

  always @(posedge clk)
  begin
    zeiger <= zeigerN;
    if (we) inhalt[zeigerN] <= wd;
  end

endmodule
