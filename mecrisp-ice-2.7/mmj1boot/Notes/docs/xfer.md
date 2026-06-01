# Hex File Transfer Support
The MultiMod transferred ROM images to the IC controller's internal flash memory as a hexidecimal ASCII stream, 32 bytes per line or 64 ASCII characters. This transfer was one-way, to the MultiMod and from the attached host, vis a TTL level serial port. A short Python progrm was used to convert a binary .BIN ROM file to its ASCII equivalent for transfer.

The MultiMod II transfers files between host and device in both directions. The old hex representation method is retained for backward compatibility with existing ROM data files in ASCII format. These Forth words are descibed below.

- **bin2hex ( nibble -- char ) - ** Convert Nibble To Hex Character.  
Convert a 4-bit nibble value to a hex character in the set '0123456789ABCDEF'.  

- ** hexdump ( ram# -- ) - ** Dump 16KB SPRAM Block In Hex.  
Read data from a specified 16KB SPRAM block and output them to the console as a stream of hexidecimal ASCII characters, 64 characters per line. Valid range for `ram#` is 0 to 7.  

- ** hexdump32 ( ram# -- ) - ** Dump Two 16KB SPRAM Blocks In Hex.  
Read data from a specified 16KB SPRAM block and output them to the console as a stream of hexidecimal ASCII characters, 64 characters per line. Valid range of `ram#` is 0 to 6.  

- ** hexdump64 ( ram# -- ) - ** Dump Four 16KB SPRAM Blocks In Hex.  
Read data from a specified 16KB SPRAM block and output them to the console as a stream of hexidecimal ASCII characters, 64 characters per line. Valid values for `ram#` is 0 to 4.  

- **hex2bin ( -- byte | -1 ) -** Read Two Hex Characters, Return Value.  
Read two hexidecimal characters from the console and convert them to a byte value. If two carriage returns are encountered in a row, then return a value of -1 to signal the end of the stream of ASCII hex data.  

- **hexload ( ram# -- nwords ) -** Read Hex Stream, Save To SPRAM Block(s).  
Accept a stream of ASCII hex characters from the console, converting them to binary and storing them in a specified 16KB block of SPRAM. Return the number of 16-bit words written to SPRAM. The value should be a multiple of 8 KB words per SPRAM 16KB block.  
