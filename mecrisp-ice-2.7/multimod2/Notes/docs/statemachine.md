# HP-71B State Machine
The HP-71B bus state machine defaults to the idle NOP state on powerup or reset. The state machine will assume one of the other fifteen command states when a command cycle occurs. Some commands are one cycle long, others like ID or CONFIG take a fixed five cycles, while the read or write commands consist of a variable number of bus cycles. A state machine that implements a bus device behaviour is simple and straightforward.

| State (Cmd)   | Next State |      | State   | Next State |
| ------- | ---------- | ---  | ------- | ---------- |
| NOP     | *NOP*      |      | ID2     | ID3        |
| ID      | ID2        |      | ID3     | ID4        |
| PCREAD  | PCREAD     |      | ID4     | ID5        |
| DPREAD  | DPREAD     |      | ID5     | *NOP*      |
| PCWRITE | PCWRITE    |      | LOADPC2 | LOADPC3    |
| DPWRITE | DPWRITE    |      | LOADPC3 | LOADPC4    |
| LOADPC  | LOADPC2    |      | LOADPC4 | LOADPC5    |
| LOADDP  | LOADDP2    |      | LOADPC5 | *NOP*      |
| CONFIG  | CONFIG2    |      | LOADDP2 | LOADDP3    |
| UNCONFIG| *NOP*      |      | LOADDP3 | LOADDP4    |
| POLL    | *NOP*      |      | LOADDP4 | LOADDP5    |
| RSV1    | *NOP*      |      | LOADDP5 | *NOP*      |
| BUSCC   | *NOP*      |      | CONFIG2 | CONFIG3    |
| RSV2    | *NOP*      |      | CONFIG3 | CONFIG4    |
| SHUTDOWN| *NOP*      |      | CONFIG4 | CONFIG5    |
| RESET   | *NOP*      |      | CONFIG5 | *NOP*      |

**Note:** *Next State entries in italic are never transitioned*

The key to the state machine sequencing is the following Verilog code snippet

```
   wire [4:0]  cmddata;
   assign cmddata = {cdn71, data_in};

   reg  [4:0]   bus_data   = 0;

   always @(posedge strn71) begin
     // Data from Saturn CPU valid on rising edge of STRN
     // Data is a command if CDN=0
     // CDN: 0=Command, 1=Data on bus
     bus_data <= cmddata;
  end

  always @(negedge strn71) begin
     // A command or data cycle begins on the falling edge of STRN
     state   <= next_state;
  end
   
  always @(*) begin
     // Must use sampled bus values since CDN and Data Bus state
     // not held through the end of a bus cycle
     if (bus_data[4] == 0)  // CDN = 0, Command cycle
       next_state <= bus_data[3:0];
     else
       case (state)
       . . . (remaining mapping from state table)
```

HP-71B bus cycles occur on an as-needed basis, that is to say, the Strobe or "clock" signal is not free running. It remains in the inactive logic high state until the Saturn CPU needs to assert a bus command, and cycles continue until the command completes. Four commands, ID, CONFIG, LOADPC, and LOADDP have a fixed five cycle length. Four commands, PCREAD, DPREAD, PCWRITE, and DPWRITE have as many cycles as needed by the Saturn CPU.

No bus cycles continue after the completion of a command until the CPU needs to address a device, such as ROM or RAM. Therefore, in the state machine above those states that have a *NOP* as their next state column never actually transition to the NOP state. When the Strobe signal once again signals the beginning of a bus cycle with a falling edge transition, the Command/Data signal indicates this is a command cycle, caussing the state machine to load one of the command states in the lefthand State column.