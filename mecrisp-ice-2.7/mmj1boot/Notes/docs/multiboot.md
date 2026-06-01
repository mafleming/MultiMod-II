# Lattice FPGA Multiboot Support
The Lattice iCE40 series FPGAs support both warm and cold boot capability in which the FPGA can dynamically reconfigure itself to meet differing external requirements. In the MultiMod II design, this feature is used to configure the FPGA as a bootloader that can update the production configuration bitstream when attached to a USB host, and otherwise configure itself with the production configuration to support its function within the HP-71B.

This Forth module supports a single function used to load a configuration bitstream using the warm boot approach.

- **warmboot ( image_num -- ) -** Boot Into Selected Bitstream Image.
<br>
The *image_num* value is between 0 and 3, where 0 is the default bitstream image loaded duing powerup or reset, and 1 is the default HP-71B production image. When bit 2 is set, the warm boot is initiated using the selected bitstream from bits 0 and 1.