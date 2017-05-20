compiletoram
include ./i2c.fs
include ./si70xx.fs

i2c-init i2c.

: t $48 i2c-addr 3 >i2c $34 >i2c  $12 >i2c $aa >i2c $cc >i2c 0 i2c-xfer ;
: y $48 i2c-addr 3 >i2c 4 i2c-xfer drop i2c> i2c> i2c> i2c> h.2 h.2 h.2 h.2 ;
