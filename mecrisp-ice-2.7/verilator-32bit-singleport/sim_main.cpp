
#include <stdio.h>
#include "Vj1a.h"
#include "Vj1a___024root.h"
#include "verilated_vcd_c.h"

VerilatedVcdC *m_trace;
vluint64_t sim_time = 0;
Vj1a* top;

void takt (void)
{
  top->clk = 1; top->eval(); /* m_trace->dump(sim_time); */ sim_time++;
  top->clk = 0; top->eval(); /* m_trace->dump(sim_time); */ sim_time++;
}

int main(int argc, char **argv)
{

    Verilated::commandArgs(argc, argv);
    top = new Vj1a;

//  Verilated::traceEverOn(true);
//  m_trace = new VerilatedVcdC;
//  top->trace(m_trace, 99);
//  m_trace->open("waveform.vcd");

//
//    if (argc != 2) {
//      fprintf(stderr, "usage: sim <hex-file>\n");
//      exit(1);
//    }
//
//    FILE *hex = fopen(argv[1], "r");
//    for (i = 0; i < 8192; i++) {
//      unsigned int v;
//      if (fscanf(hex, "%x\n", &v) != 1) {
//        fprintf(stderr, "invalid hex value at line %d\n", i + 1);
//        exit(1);
//      }
//      top->v__DOT___j1__DOT__mem[i] = v;
//    }


    uint64_t i = 0;

    top->uart0_valid = 1;   // pretend to always have a character waiting
    top->uart0_busy = 0;   // pretend to never be busy

    top->resetq = 0;
    top->eval();
    top->resetq = 1;

    int data = 0;

    for (i = 0; ; i++) {
      top->uart0_data = data;

      takt();

      if (top->uart0_wr) {
        putchar(top->uart_w);
      }

      if (top->uart0_rd) {
        data=getchar();
        if (data ==  27) break;
        if (data == EOF) break;
        if (data == 127) { data=8; } // Replace DEL with Backspace
      }

    }
//  m_trace->close();

    printf("Simulation ended after %d cycles\n", i);

    FILE *fcoredump;
    fcoredump = fopen("coredump.hex", "w");
    if(fcoredump == NULL) exit(-1);

    for (i = 0; i < 8192; i++) {
      fprintf(fcoredump, "%08X\n", top->rootp->v__DOT___j1__DOT__mem[i]);
    }

    fclose(fcoredump);

    delete top;
    exit(0);
}
