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
 ********************************************************************/

void dut_reset (Vhp71b *dut, vluint64_t &sim_time){
    dut->clk_i = 0;
    dut->rstn_i = 1;
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
	//	set_rnd_out_valid(dut, sim_time);
	switch (posedge_cnt){
	case 5:
	  // 16KB Forth/Assembler ROM
	  dut->bank_addr = 0;
	  dut->bank_wr = 1;
	  dut->bank_datain = 0b1101;  // Inuse, ChainEnd, ROM (01)
	  break;

	case 6:
	  dut->bank_addr = 0;
	  dut->bank_wr = 0;
	  dut->bank_datain = 0b1101;  // Inuse, ChainEnd, ROM (01)
	  break;

	case 10:
	  // 32KB Forth/Assembler Hard ROM
	  dut->bank_addr = 1;
	  dut->bank_wr = 1;
	  dut->bank_datain = 0b1010;  // Inuse, Hard ROM (10)
	  break;

	case 11:
	  // 32KB Forth/Assembler Hard ROM
	  dut->bank_addr = 1;
	  dut->bank_wr = 0;
	  dut->bank_datain = 0b1010;  // Inuse, Hard ROM (10)
	  break;

	case 15:
	  // 32KB Forth/Assembler Hard ROM
	  dut->bank_addr = 2;
	  dut->bank_wr = 1;
	  dut->bank_datain = 0b1110;  // Inuse, ChainEnd, Hard ROM (10)
	  break;

	case 16:
	  // 32KB Forth/Assembler Hard ROM
	  dut->bank_addr = 2;
	  dut->bank_wr = 0;
	  dut->bank_datain = 0b1110;  // Inuse, ChainEnd, Hard ROM (10)
	  break;

	case 20:
	  // 16KB Unused Bank
	  dut->bank_addr = 3;
	  dut->bank_wr = 1;
	  dut->bank_datain = 0;  // None
	  break;

	case 21:
	  dut->bank_addr = 3;
	  dut->bank_wr = 0;
	  dut->bank_datain = 0;  // None
	  break;

	case 25:
	  // 32KB Math ROM
	  dut->bank_addr = 4;
	  dut->bank_wr = 1;
	  dut->bank_datain = 0b1001;  // Inuse, ROM (01)
	  break;

	case 26:
	  // 32KB Math ROM
	  dut->bank_addr = 4;
	  dut->bank_wr = 0;
	  dut->bank_datain = 0b1001;  // Inuse, Hard ROM (01)
	  break;

	case 30:
	  // 32KB Math ROM
	  dut->bank_addr = 5;
	  dut->bank_wr = 1;
	  dut->bank_datain = 0b1101;  // Inuse, ChainEnd, ROM (01)
	  break;

	case 31:
	  // 32KB Math ROM
	  dut->bank_addr = 5;
	  dut->bank_wr = 0;
	  dut->bank_datain = 0b1101;  // Inuse, ChainEnd, ROM (01)
	  break;

	case 35:
	  // 32KB JPC05 ROM
	  dut->bank_addr = 6;
	  dut->bank_wr = 1;
	  dut->bank_datain = 0b1001;  // Inuse, ROM (01)
	  break;

	case 36:
	  // 32KB JPC05 ROM
	  dut->bank_addr = 6;
	  dut->bank_wr = 0;
	  dut->bank_datain = 0b1001;  // Inuse, Hard ROM (01)
	  break;

	case 40:
	  // 32KB JPC05 ROM
	  dut->bank_addr = 7;
	  dut->bank_wr = 1;
	  dut->bank_datain = 0b1101;  // Inuse, ChainEnd, ROM (01)
	  break;

	case 41:
	  // 32KB JPC05 ROM
	  dut->bank_addr = 7;
	  dut->bank_wr = 0;
	  dut->bank_datain = 0b1101;  // Inuse, ChainEnd, ROM (01)
	  break;

	}
      }
      m_trace->dump(sim_time);
      sim_time++;
    }

    m_trace->close();
    delete dut;
    exit(EXIT_SUCCESS);
}
