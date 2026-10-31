TB=testbench
waves1: $(TB)/Makefile_tb1
	(cd $(TB) ; make -f Makefile_tb1 waves)

.PHONY: clean
clean:
	rm -rf $(TB)/.stamp.*;
	rm -rf $(TB)/obj_dir
	rm -rf $(TB)waveform.vcd
