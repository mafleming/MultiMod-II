# Directory Implementation
The following provides implementation detail for an image directory. As per the previous section, a directory occupies a single 16KB block of flash memory, and is divided into 1024 directory entries of 16 bytes each. A directory is thus able to addess the entirety of flash memory, which itself is divided into 1024 images blocks of 16KB each.

Multiple directories and their associated image blocks can exist in flash. The initial implementation will have separate directories for Forth dictionary images and HP-71B ROM/IRAM images. The bulk of flash storage will be allocated to HP-71B images, which can be up to 14 MB. The base implementation will have only these two directories.

## Directory Structure
A directory is a collection of 16KB image blocks in flash. The first 16KB block is the directory itself, followed by a scratch 16KB block used by the packing process. Following these two 16KB blocks are a fixed number of 16KB image blocks associated with the directory. This size value is encoded in the first directory entry of the directory itself. Each directory collection can be packed separately.

## Directory Entry
A directory entry specifies an image name, type, size, and location. Starting with the first entry byte, the entry structure is

- (0) **Entry Status.**
<br>
This byte indicates the entry status, whether it is **Empty** ($FF), **Valid** ($F0), or **Reclaimable** ($00).
<br>
- (1) **Image Type.**
<br>
An image type is encoded in the upper four bits of this byte and its size is encoded in the lower four bits. The image size is the number of 16KB image blocks given by the lower four bits plus one.
<br>
The image type is **IRAM** ($0x), **ROM** ($1x), **HARD ROM** ($2x), **ROM+HARD ROM** ($3x), and **TAKEOVER ROM** ($4x). Note that the size of a HARD ROM and TAKEOVER ROM are fixed, so that the size bits are ignored. The size bits of a ROM+HARD ROM encode only the size of the ROM itself.
<br>
As an example, the Forth/Assembly ROM combined with its companion Hard ROM would be encoded as $20 (ROM+HARD ROM, size one 16KB image block).
<br>
The first entry of a directory is of type **SIZE** ($8x). The lower four bits are ignored, and the Image Location field indicates the number of image blocks that are inclusive to the directory.
- (2~3) **Image Location.**
<br>
The location of images could be encoded as an absolute position within flash memory, or as a offset from the beginning of the directory.
<br>
- (4~15) **Name.**
<br>
An image name can be up to 12 ASCII characters, padded with $FF bytes when less than 12 characters.


