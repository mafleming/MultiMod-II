# SPRAM Memory Management
Both the j1a Forth CPU and the HP-71B Saturn CPU need to access the 128 KB of SPRAM, possibly at the same time. The HP-71B bus runs at a leisurely 625 KHz, but it was potentially expected to run at 1 MHz at some future date. The j1a CPU runs at 12 MHz to 18 MHz, and thus must be given priority over the Saturn CPU in regard to simultaneous access requests.

The read and write bus cycles have already been described in the previous **HP-71B Bus** section.