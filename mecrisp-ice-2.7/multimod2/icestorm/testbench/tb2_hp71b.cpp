#include <stdlib.h>
#include <iostream>
#include <verilated.h>
#include <verilated_vcd_c.h>
#include "Vhp71b.h"


#define MAX_SIM_TIME 400
vluint64_t sim_time = 0;
vluint64_t posedge_cnt = 0;

/*********************************************************************
     Test bench to validate behaviour after reset.
 ********************************************************************/

#define NOP  0
#define ID 1
#define PCREAD 2
#define DPREAD 3
#define PCWRITE 4
#define DPWRITE 5
#define LOADPC 6
#define LOADDP 7
#define CONFIG 8
#define UNCONFIG 9
#define POLL 10
#define RSV1 11
#define BUSCC 12
#define RSV2 13
#define SHUTDOWN 14
#define RESET 15

#define BANKSET(t,a,v) case t: \
  dut->bank_addr = a;	       \
  dut->bank_wr = 1;	       \
  dut->bank_datain = v;	       \
  break;		       \
  case t+1:		       \
  dut->bank_wr = 0;	       \
  break;

#define DAISYON(t) case t: \
  dut->daisyin_i = 1;	   \
  break;

#define DAISYOFF(t) case t: \
  dut->daisyin_i = 0;	    \
  break;

#define COMMAND(t,c) case t: \
  dut->commandn_i = 0;	     \
  break;		     \
  case t+1:		     \
  dut->stroben_i = 0;	     \
  dut->bus_in = c;	     \
  break;		     \
  case t+5:		     \
  dut->stroben_i = 1;	     \
  break;		     \
  case t+7:		     \
  dut->commandn_i = 1;	     \
  break;

#define READBUS(t) case t: \
  dut->stroben_i = 0;	   \
  break;		   \
  case t+5:		   \
  dut->stroben_i = 1;	   \
  break;

#define WRITEBUS(t,v) case t: \
  dut->stroben_i = 0;	      \
  dut->bus_in = v;	      \
  break;		      \
  case t+5:		      \
  dut->stroben_i = 1;	      \
  break;


void dut_reset (Vhp71b *dut, vluint64_t &sim_time){
    dut->clk_i = 0;
    dut->rstn_i = 0;
    dut->bank_addr = 0;
    dut->bank_wr = 0;
    dut->bank_datain = 0;

    dut->stroben_i = 1;
    dut->commandn_i = 1;
    dut->daisyin_i = 0;
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

	  // Release reset
	case 2:
	  dut->rstn_i = 1;

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

	  // For HP-71B bus traffic, use the j1a clock for timing
	  // even though the clock and 71B bus are not synchronized.

	  // NOP command
	    COMMAND(50,NOP)

	    // ID command, follow with 5 read cycles
	    COMMAND(60,ID)
	    READBUS(70)
	    READBUS(80)
	    READBUS(90)
	    READBUS(100)
	    READBUS(110)

	    // LOADPC command, follow with 5 write cycles
	    COMMAND(120,LOADPC)
	    WRITEBUS(130,1)
	    WRITEBUS(140,2)
	    WRITEBUS(150,3)
	    WRITEBUS(160,4)
	    WRITEBUS(170,5)
	    /*
	case 120:
	  dut->commandn_i = 0;
	  break;
	case 121:
	  dut-> stroben_i = 0;
	  dut-> bus_in    = 6;
	  break;
	case 125:
	  dut-> stroben_i = 1;
	  break;
	case 127:
	  dut->commandn_i = 1;
	  dut-> stroben_i = 1;
	  break;
	case 130:
	  dut-> stroben_i = 0;
	  dut-> bus_in    = 1;
	  break;
	case 135:
	  dut-> stroben_i = 1;
	  break;
	case 140:
	  dut-> stroben_i = 0;
	  dut-> bus_in    = 2;
	  break;
	case 145:
	  dut-> stroben_i = 1;
	  break;
	case 150:
	  dut-> bus_in    = 3;
	  dut-> stroben_i = 0;
	  break;
	case 155:
	  dut-> stroben_i = 1;
	  break;
	case 160:
	  dut-> stroben_i = 0;
	  dut-> bus_in    = 4;
	  break;
	case 165:
	  dut-> stroben_i = 1;
	  break;
	case 170:
	  dut-> stroben_i = 0;
	  dut-> bus_in    = 5;
	  break;
	case 175:
	  dut-> stroben_i = 1;
	  break;
	    */

        }
      }
      m_trace->dump(sim_time);
      sim_time++;
    }

    m_trace->close();
    delete dut;
    exit(EXIT_SUCCESS);
}
