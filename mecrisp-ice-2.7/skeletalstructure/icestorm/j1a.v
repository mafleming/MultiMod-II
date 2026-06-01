
`default_nettype none

`define cfg_divider  104  // 12 MHz / 115200 = 104.17

`include "../common-verilog/uart.v"
`include "../common-verilog/j1-universal-16kb-quickstore.v"

module top(input  oscillator,

           output TXD,        // UART TX
           input  RXD,        // UART RX

           input reset_button
);

  // ######   Clock   #########################################

  wire clk = oscillator;

  // ######   Reset logic   ###################################

  reg [3:0] reset_cnt = 0;
  wire resetq = &reset_cnt;

  always @(posedge clk) begin
    if (reset_button) reset_cnt <= reset_cnt + !resetq;
    else              reset_cnt <= 0;
  end

  // ######   Bus    ##########################################

  wire io_rd, io_wr;
  wire [15:0] io_addr;
  wire [15:0] io_dout;
  wire [15:0] io_din;

  reg interrupt = 0;

  // ######   Processor   #####################################

  j1 _j1(
    .clk(clk),
    .resetq(resetq),

    .io_rd(io_rd),
    .io_wr(io_wr),
    .io_dout(io_dout),
    .io_din(io_din),
    .io_addr(io_addr),

    .interrupt_request(interrupt)
  );

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

  // ######   Terminal   ######################################

  wire uart0_valid, uart0_busy;
  wire [7:0] uart0_data;
  wire uart0_wr = io_wr & io_addr[12];
  wire uart0_rd = io_rd & io_addr[12];

  buart _uart0 (
     .clk(clk),
     .resetq(resetq),
     .rx(RXD),
     .tx(TXD),
     .rd(uart0_rd),
     .wr(uart0_wr),
     .valid(uart0_valid),
     .busy(uart0_busy),
     .tx_data(io_dout[7:0]),
     .rx_data(uart0_data));

  // ######   IO Ports   ######################################

  /*        bit READ            WRITE

      0001  0
      0002  1
      0004  2
      0008  3

      0010  4
      0020  5
      0040  6
      0080  7

      0100  8
      0200  9
      0400  10
      0800  11

      1000  12  UART RX         UART TX
      2000  13  UART Flags
      4000  14  Ticks           Set Ticks
      8000  15  Cycles
  */

  assign io_din =

    (io_addr[12] ? { 8'd0, uart0_data}                                              : 16'd0) |
    (io_addr[13] ? {14'd0, uart0_valid, !uart0_busy}                                : 16'd0) |
    (io_addr[14] ?         ticks                                                    : 16'd0) |
    (io_addr[15] ?         cycles                                                   : 16'd0) ;

endmodule // top
