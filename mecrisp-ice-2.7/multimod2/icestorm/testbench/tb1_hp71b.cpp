#include <stdlib.h>
#include <iostream>
#include <verilated.h>
#include <verilated_vcd_c.h>
#include "Vhp71b.h"


#define MAX_SIM_TIME 100
vluint64_t sim_time = 0;
vluint64_t posedge_cnt = 0;

/*********************************************************************
     Test bench to validate the bank register access function for the
     HP71B state machine.
     The clk_i input is driven by the same clock as the j1a Forth CPU
     and is only active when the CPU is active.
     The test bench accesses each bank register in turn, writing a
     configuration value similar to the MultiMod default ROM content.
     This includes the Forth/Assembler 16K ROM, the 32K hard ROM
     associated with Forth, and two 32K ROMs, Math & JPC05. One bank
     is left unused.
 ********************************************************************/

#define BANKSET(t,a,v) case t: \
  dut->bank_addr = a;	       \
  dut->bank_wr = 1;	       \
  dut->bank_datain = v;	       \
  break;		       \
  case t+1:		       \
  dut->bank_wr = 0;	       \
  break;


void dut_reset (Vhp71b *dut, vluint64_t &sim_time){
    dut->clk_i = 0;
    dut->rstn_i = 0;
    dut->bank_addr = 0;
    dut->bank_wr = 0;
    dut->bank_datain = 0;
}

int main(int argc, char** argv, char** env) {
    Verilated::commandArgs(argc, argv);
    Vhp71b *dut = new Vhp71b;

    Verilated::traceEverOn(true);
    VerilatedVcdC *m_trace = new VerilatedVcdC;
    dut->trace(m_trace, 5);
    m_trace->open("waveform.vcd");

    dut_reset(dut, sim_time);
    while (sim_time < MAX_SIM_TIME) {

      dut->clk_i ^= 1;
      dut->eval();

      if (dut->clk_i == 1){ // Active on clock high
	posedge_cnt++;
	switch (posedge_cnt){

	case 2:
	  dut->rstn_i = 1;
	  break;

	  // 16KB Forth/Assembler ROM
	  BANKSET(5,0,0b1101)  // Inuse, ChainEnd, ROM (01)

	    // 32KB Forth/Assembler Hard ROM
	    BANKSET(10,1,0b1010)  // Inuse, Hard ROM (10)
	    BANKSET(15,2,0b1110)  // Inuse, ChainEnd, Hard ROM (10)

	    // 16KB Unused Bank
	    BANKSET(20,3,0)  // None

	    // 32KB Math ROM
	    BANKSET(25,4,0b1001)  // Inuse, Hard ROM (01)
	    BANKSET(30,5,0b1101)  // Inuse, ChainEnd, Hard ROM (01)

	    // 32KB JPC05 ROM
	    BANKSET(35,6,0b1001)  // Inuse, Hard ROM (01)
	    BANKSET(40,7,0b1101)  // Inuse, ChainEnd, Hard ROM (01)

	}
      }
      m_trace->dump(sim_time);
      sim_time++;
    }

    m_trace->close();
    delete dut;
    exit(EXIT_SUCCESS);
}
