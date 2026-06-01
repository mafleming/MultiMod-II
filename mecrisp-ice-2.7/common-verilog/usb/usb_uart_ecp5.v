
`default_nettype none

`include "../common-verilog/usb/edge_detect.v"
`include "../common-verilog/usb/serial.v"
`include "../common-verilog/usb/usb_fs_in_arb.v"
`include "../common-verilog/usb/usb_fs_in_pe.v"
`include "../common-verilog/usb/usb_fs_out_arb.v"
`include "../common-verilog/usb/usb_fs_out_pe.v"
`include "../common-verilog/usb/usb_fs_pe.v"
`include "../common-verilog/usb/usb_fs_rx.v"
`include "../common-verilog/usb/usb_fs_tx_mux.v"
`include "../common-verilog/usb/usb_fs_tx.v"
`include "../common-verilog/usb/usb_reset_det.v"
`include "../common-verilog/usb/usb_serial_ctrl_ep.v"
`include "../common-verilog/usb/usb_uart_bridge_ep.v"
`include "../common-verilog/usb/usb_uart_core.v"

module usb_uart (
  input clk_48mhz,
  input resetq,
  output host_presence,

  // USB pins
  inout  pin_usb_p,
  inout  pin_usb_n,

  // UART interface
  input  uart_wr,
  input  uart_rd,
  input  [7:0] uart_tx_data,
  output [7:0] uart_rx_data,
  output uart_busy,
  output uart_valid
);

    wire usb_p_tx;
    wire usb_n_tx;
    wire usb_p_rx;
    wire usb_n_rx;
    wire usb_tx_en;

    wire usb_reset_detect;

    usb_reset_det _reset_det(
      .clk(clk_48mhz),
      .reset(usb_reset_detect),
      .usb_p_rx(usb_p_rx),
      .usb_n_rx(usb_n_rx),
    );

    wire reset = usb_reset_detect | ~resetq;

    usb_uart_core _uart (
        .clk_48mhz(clk_48mhz),
        .reset    (reset),
        .host_presence  (host_presence),

        // USB interface

        .usb_p_tx(usb_p_tx),
        .usb_n_tx(usb_n_tx),
        .usb_p_rx(usb_p_rx),
        .usb_n_rx(usb_n_rx),
        .usb_tx_en(usb_tx_en),

        // UART interface

        .uart_wr     (uart_wr),
        .uart_rd     (uart_rd),
        .uart_tx_data(uart_tx_data),
        .uart_rx_data(uart_rx_data),
        .uart_busy   (uart_busy),
        .uart_valid  (uart_valid)
    );

    wire usb_p_in;
    wire usb_n_in;

    assign usb_p_rx = usb_tx_en ? 1'b1 : usb_p_in;
    assign usb_n_rx = usb_tx_en ? 1'b0 : usb_n_in;

    // T = TRISTATE (not transmit)
    BB io_p( .I( usb_p_tx ), .T( !usb_tx_en ), .O( usb_p_in ), .B( pin_usb_p ) );
    BB io_n( .I( usb_n_tx ), .T( !usb_tx_en ), .O( usb_n_in ), .B( pin_usb_n ) );

endmodule
