\ core definitions

\ <<<board>>>
compiletoflash
( core start: ) here hex.

1 constant OLED.LARGE  \ display size: 0 = 128x32, 1 = 128x64 (default)

include ../flib/i2c/ssd1306.fs
include ../flib/mecrisp/graphics.fs
include ../flib/any/digits.fs
include ../flib/mecrisp/multi.fs
include ../flib/any/timed.fs

cornerstone <<<core>>>
hello
