\ core definitions

\ <<<board>>>
<<<<<<< HEAD
cr compiletoflash
( core start: ) here dup hex.

9 constant I2C.DELAY
include ../flib/stm32f1/i2c.fs
=======
compiletoflash
( core start: ) here hex.
>>>>>>> hwi2cfix

include ../flib/i2c/ssd1306.fs
include ../flib/mecrisp/graphics.fs
include ../flib/any/digits.fs
include ../flib/mecrisp/multi.fs
include ../flib/any/timed.fs

cornerstone <<<core>>>
hello
