
`default_nettype none

module usb_uart_core (
  input  clk_48mhz,
  input  reset,
  output host_presence,

  // USB lines.  Split into input vs. output and oe control signal to maintain
  // highest level of compatibility with synthesis tools.

  output usb_p_tx,
  output usb_n_tx,

  input  usb_p_rx,
  input  usb_n_rx,

  output usb_tx_en,

  input  uart_wr,
  input  uart_rd,
  input  [7:0] uart_tx_data,
  output [7:0] uart_rx_data,
  output uart_busy,
  output uart_valid
);

  ////////////////////////////////////////////////////////////////////////////////
  ////////
  //////// usb engine
  ////////
  ////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////

  wire [6:0] dev_addr;
  wire [7:0] out_ep_data;

  wire ctrl_out_ep_req;
  wire ctrl_out_ep_grant;
  wire ctrl_out_ep_data_avail;
  wire ctrl_out_ep_setup;
  wire ctrl_out_ep_data_get;
  wire ctrl_out_ep_stall;
  wire ctrl_out_ep_acked;

  wire ctrl_in_ep_req;
  wire ctrl_in_ep_grant;
  wire ctrl_in_ep_data_free;
  wire ctrl_in_ep_data_put;
  wire [7:0] ctrl_in_ep_data;
  wire ctrl_in_ep_data_done;
  wire ctrl_in_ep_stall;
  wire ctrl_in_ep_acked;


  wire serial_out_ep_req;
  wire serial_out_ep_grant;
  wire serial_out_ep_data_avail;
  wire serial_out_ep_setup;
  wire serial_out_ep_data_get;
  wire serial_out_ep_stall;
  wire serial_out_ep_acked;

  wire serial_in_ep_req;
  wire serial_in_ep_grant;
  wire serial_in_ep_data_free;
  wire serial_in_ep_data_put;
  wire [7:0] serial_in_ep_data;
  wire serial_in_ep_data_done;
  wire serial_in_ep_stall;
  wire serial_in_ep_acked;

  wire sof_valid;
  wire [10:0] frame_index;

  usb_serial_ctrl_ep ctrl_ep_inst (
    .clk(clk_48mhz),
    .reset(reset),
    .dev_addr(dev_addr),

    // out endpoint interface
    .out_ep_req(ctrl_out_ep_req),
    .out_ep_grant(ctrl_out_ep_grant),
    .out_ep_data_avail(ctrl_out_ep_data_avail),
    .out_ep_setup(ctrl_out_ep_setup),
    .out_ep_data_get(ctrl_out_ep_data_get),
    .out_ep_data(out_ep_data),
    .out_ep_stall(ctrl_out_ep_stall),
    .out_ep_acked(ctrl_out_ep_acked),


    // in endpoint interface
    .in_ep_req(ctrl_in_ep_req),
    .in_ep_grant(ctrl_in_ep_grant),
    .in_ep_data_free(ctrl_in_ep_data_free),
    .in_ep_data_put(ctrl_in_ep_data_put),
    .in_ep_data(ctrl_in_ep_data),
    .in_ep_data_done(ctrl_in_ep_data_done),
    .in_ep_stall(ctrl_in_ep_stall),
    .in_ep_acked(ctrl_in_ep_acked)
  );

  usb_uart_bridge_ep usb_uart_bridge_ep_inst (
    .clk(clk_48mhz),
    .reset(reset),

    // out endpoint interface
    .out_ep_req(serial_out_ep_req),
    .out_ep_grant(serial_out_ep_grant),
    .out_ep_data_avail(serial_out_ep_data_avail),
    .out_ep_setup(serial_out_ep_setup),
    .out_ep_data_get(serial_out_ep_data_get),
    .out_ep_data(out_ep_data),
    .out_ep_stall(serial_out_ep_stall),
    .out_ep_acked(serial_out_ep_acked),

    // in endpoint interface
    .in_ep_req(serial_in_ep_req),
    .in_ep_grant(serial_in_ep_grant),
    .in_ep_data_free(serial_in_ep_data_free),
    .in_ep_data_put(serial_in_ep_data_put),
    .in_ep_data(serial_in_ep_data),
    .in_ep_data_done(serial_in_ep_data_done),
    .in_ep_stall(serial_in_ep_stall),
    .in_ep_acked(serial_in_ep_acked),

    // uart interface
    .uart_wr(uart_wr),
    .uart_rd(uart_rd),
    .uart_tx_data(uart_tx_data),
    .uart_rx_data(uart_rx_data),
    .uart_busy(uart_busy),
    .uart_valid(uart_valid)
  );

  usb_fs_pe #(
    .NUM_OUT_EPS(5'd2),
    .NUM_IN_EPS(5'd2)
  ) usb_fs_pe_inst (
    .clk(clk_48mhz),
    .reset(reset),

    .usb_p_tx(usb_p_tx),
    .usb_n_tx(usb_n_tx),
    .usb_p_rx(usb_p_rx),
    .usb_n_rx(usb_n_rx),
    .usb_tx_en(usb_tx_en),

    .dev_addr(dev_addr),

    // out endpoint interfaces
    .out_ep_req({serial_out_ep_req, ctrl_out_ep_req}),
    .out_ep_grant({serial_out_ep_grant, ctrl_out_ep_grant}),
    .out_ep_data_avail({serial_out_ep_data_avail, ctrl_out_ep_data_avail}),
    .out_ep_setup({serial_out_ep_setup, ctrl_out_ep_setup}),
    .out_ep_data_get({serial_out_ep_data_get, ctrl_out_ep_data_get}),
    .out_ep_data(out_ep_data),
    .out_ep_stall({serial_out_ep_stall, ctrl_out_ep_stall}),
    .out_ep_acked({serial_out_ep_acked, ctrl_out_ep_acked}),

    // in endpoint interfaces
    .in_ep_req({serial_in_ep_req, ctrl_in_ep_req}),
    .in_ep_grant({serial_in_ep_grant, ctrl_in_ep_grant}),
    .in_ep_data_free({serial_in_ep_data_free, ctrl_in_ep_data_free}),
    .in_ep_data_put({serial_in_ep_data_put, ctrl_in_ep_data_put}),
    .in_ep_data({serial_in_ep_data[7:0], ctrl_in_ep_data[7:0]}),
    .in_ep_data_done({serial_in_ep_data_done, ctrl_in_ep_data_done}),
    .in_ep_stall({serial_in_ep_stall, ctrl_in_ep_stall}),
    .in_ep_acked({serial_in_ep_acked, ctrl_in_ep_acked}),

    // sof interface
    .sof_valid(sof_valid),
    .frame_index(frame_index)
  );


  ////////////////////////////////////////////////////////////////////////////////
  // host presence detection
  ////////////////////////////////////////////////////////////////////////////////

  // Start-of-Frame packets are transmitted every millisecond by the host.
  // When not reveicing SOF packets for 100 ms = 4 800 000, host isn't present.
  // Original value for timeout was 1 second, 48 000 000

  reg        host_presence_reg = 0;
  reg [23:0] host_presence_timer = 0;

  assign host_presence = host_presence_reg;

  always @(posedge clk_48mhz) begin

    if (sof_valid) begin
      host_presence_reg <= 1;
      host_presence_timer <= 0;
    end else begin
      if (host_presence_timer > 4800000) begin // 100 ms Timeout
        host_presence_reg <= 0;
      end else begin
        host_presence_timer <= host_presence_timer + 1;
      end
    end
  end

endmodule
