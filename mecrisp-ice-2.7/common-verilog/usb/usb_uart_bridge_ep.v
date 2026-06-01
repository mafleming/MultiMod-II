
// USB-UART-Bridge is based on https://github.com/lawrie/tiny_usb_examples by Lawrie Griffiths
// Modified for use in Mecrisp-Ice by Matthias Koch

`default_nettype none

module usb_uart_bridge_ep (
  input clk,
  input reset,

  // OUT endpoint interface: Host to device

  output out_ep_req,
  input out_ep_grant,
  input out_ep_data_avail,
  input out_ep_setup,
  output out_ep_data_get,
  input [7:0] out_ep_data,
  output out_ep_stall,
  input out_ep_acked,

  // IN endpoint interface: Device to host

  output reg in_ep_req,
  input in_ep_grant,
  input in_ep_data_free,
  output reg in_ep_data_put,
  output [7:0] in_ep_data,
  output reg in_ep_data_done = 0,
  output in_ep_stall,
  input in_ep_acked,

  // UART Interface: Host to device

  output reg uart_valid = 0,
  input uart_rd,
  output reg [7:0] uart_rx_data,

  // UART Interface: Device to host

  output reg uart_busy = 0,
  input uart_wr,
  input [7:0] uart_tx_data
);


  // --------------------------------------------------------------------------
  //   OUT Endpoint: Host to device
  // --------------------------------------------------------------------------

  assign out_ep_stall = 1'b0;

  reg get_out_data = 0;

  assign out_ep_req = out_ep_data_avail;
  assign out_ep_data_get = get_out_data && out_ep_grant;

  wire out_data_ready = out_ep_grant && out_ep_data_avail;


  // State machine
  reg [1:0] state_out = 0;

  always @(posedge clk)
  begin

    get_out_data <= 0;
    uart_valid <= uart_valid && ~uart_rd;

    case (state_out)
      0: begin
        if (out_data_ready && ~uart_valid) begin
          state_out <= 1;
          get_out_data <= 1;
        end
      end
      1: begin
        state_out <= 2;
      end
      2: begin
        uart_rx_data <= out_ep_data;
        state_out <= 3;
        uart_valid <= 1;
      end
      3: begin
        state_out <= 0;
      end
    endcase
  end


  // --------------------------------------------------------------------------
  //   IN Endpoint: Device to Host
  // --------------------------------------------------------------------------

  reg [7:0] buffer_to_send = 0;

  assign in_ep_stall = 1'b0;
  assign in_ep_data = buffer_to_send;

  // State machine
  reg [1:0] state_in = 0;

  always @(posedge clk)
  begin

    if (uart_wr) // New data to transmit
    begin
      buffer_to_send <= uart_tx_data;
      uart_busy <= 1;
    end

    in_ep_data_put <= 0;
    in_ep_data_done <= 0;

    case (state_in)
      0: begin
        if (uart_busy) state_in <= 1;
      end
      1: begin
        if (in_ep_data_free) begin
          in_ep_req <= 1;
          state_in <= 2;
        end
      end
      2: begin
        if (in_ep_data_free && in_ep_grant) begin
          in_ep_data_put <= 1;
          state_in <= 3;
        end
      end
      3: begin
        in_ep_data_done <= 1;
        in_ep_req <= 0;
        uart_busy <= 0;
        state_in <= 0;
      end
    endcase
  end

endmodule
