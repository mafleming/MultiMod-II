# MultiMod II Flash File System
Although a MultiMod II owner developing Forth applications might be able to keep track of in which block number a dictionary is stored, the task is far more difficult for the many HP-71B ROM/IRAM images stored in flash. Each image varies in size (16KB, 32KB, 64KB), access (Read-Only, Read-Write), and type (Forth, ROM, hard ROM, IRAM). There may even be different versions of ROMs, as with the MATH ROM.

Thus a file system is needed to support creating, deleting, or renaming a storage entity. In the case of both Forth dictionaries and HP-71B memories, the supported operations are as follows

* **New -** For the HP-71B a new IRAM can be created by giving a name and size.
* **Load -** A named image is loaded into memory.
* **Save -** A Forth dictionary image or a HP-71B IRAM image in memory can be saved to flash under its existing name (Save), or can be saved with a new name (Save As) so as not to overwrite the old image.

## Directory Structure
A directory consists of 1024 directory entries in a 16KB flash block. The structure of an individual entry is defined in the next section. Since new entries can only be written to previously erased sections of the flash block, new or modified entries are written after the last entry in the directory list.

Entries grow upward within the directory block, with deleted entries marked as reclaimable. When updating an existing HP771B ROM/IRAM image, the size of the image remains the same as when the image directory entry was first created. That is, the resizing of an image is not supported.

## Directory Entry
A directory entry is sixteen bytes in length, with twelve bytes devoted to the entry object name, two bytes to the image starting block number, and two bytes devoted to the attributes of the object. To support directory relocatability, the image starting block number is a offset from the beginning block of the directory itself.

Flash bytes are all ones when erased, so writing to flash consists of setting ones to zeros in a given byte. If bits are considered flags with default value set to one, then a bit in one of the attribute bytes can be forced to zero to indicate the entry is no longer valid.

## Directory Operations
As mutable images are updated and stored to flash, the most recent entry for an image is located at the upper end of active, unerased flash. Thus directory search starts at the most recently written directory entry, working downward towards the beginning of a directory block.

Directory entries and images both grow upwards within the flash storage allocated to them. Entries and/or images eventually reach the top of their allocated storage area, and so entries and images that are no longer valid must be eliminated in a process called *packing*.

## Directory/Storage Packing
The packing process begins by scanning upwards in the directory searching for reclaimable entries. For each such entry, the 16KB blocks making up the image that the directory entry points to are erased. This erased space will later be occupied by valid images as part of the packing process.

Directory packing involves copying valid directory entries over to an erased scratch block the same size as the directory block. As directory entries are copied, their corresponding images they point to are also moved downward in flash address space.

Downward movement of images involves keeping track of the lowest erased block, then copying the image one block at a time. Each image block copied downward is erased after the copy completes. This step is necessary not only to provide room for images above the current image, but also to handle the case when there are not enough empty blocks below the image to hold the complete image. Erasing blocks as they are copied provides the needed space for subsequent image blocks.
