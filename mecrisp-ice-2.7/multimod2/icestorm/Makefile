TB=testbench

waves1: $(TB)/tb1_hp71b.cpp hp71b.v
	(cd $(TB) ; make TB=tb1 waves)

waves2: $(TB)/tb2_hp71b.cpp hp71b.v
	(cd $(TB) ; make TB=tb2 waves)

waves3: $(TB)/tb2_hp71b.cpp hp71b.v
	(cd $(TB) ; make TB=tb3 waves)

.PHONY: clean
clean:
	rm -rf $(TB)/.stamp.*;
	rm -rf $(TB)/obj_dir
	rm -rf $(TB)/waveform.vcd
